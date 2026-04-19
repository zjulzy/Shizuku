using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 技能播放器。外部 MonoBehaviour 持有并在 Update 中调用 Tick。
    /// </summary>
    public class SkillPlayer
    {
        private List<ITrackRunner> _runners;
        private float _currentTime;
        private float _duration;
        private bool _isPlaying;

        // SkillTrack到TrackRunner的映射
        private static readonly Dictionary<Type, Type> _runnerTypeCache = new();

        public bool IsPlaying => _isPlaying;
        public float CurrentTime => _currentTime;
        public float Duration => _duration;

        public void Play(ShizukuSkillConfig config, SkillContext ctx)
        {
            _duration = config.Duration;
            _currentTime = 0f;
            _isPlaying = true;

            _runners = new List<ITrackRunner>();
            foreach (var track in config.Tracks)
            {
                if (!track.Enabled) continue;
                var runner = CreateRunnerForTrack(track);
                runner.Init(track);
                runner.OnSkillStart(ctx);
                _runners.Add(runner);
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_isPlaying) return;
            _currentTime += deltaTime;

            foreach (var runner in _runners)
                runner.OnTick(_currentTime, deltaTime);

            if (_currentTime >= _duration)
                Stop();
        }

        public void Stop()
        {
            if (!_isPlaying) return;
            foreach (var runner in _runners)
                runner.OnSkillEnd();
            _isPlaying = false;
        }

        public void Interrupt()
        {
            if (!_isPlaying) return;
            foreach (var runner in _runners)
                runner.OnSkillInterrupt();
            _isPlaying = false;
        }

        private static ITrackRunner CreateRunnerForTrack(SkillTrack track)
        {
            var trackType = track.GetType();
            if (!_runnerTypeCache.TryGetValue(trackType, out var runnerType))
            {
                var attr = trackType.GetCustomAttribute<TrackRunnerAttribute>();
                runnerType = attr?.RunnerType
                    ?? throw new Exception($"Track {trackType.Name} 没有标记 [TrackRunner] attribute");
                _runnerTypeCache[trackType] = runnerType;
            }
            return (ITrackRunner)Activator.CreateInstance(runnerType);
        }
    }
}

