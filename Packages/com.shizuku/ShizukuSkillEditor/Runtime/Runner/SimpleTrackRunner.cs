using System.Collections.Generic;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 通用轨道运行器。为每个 Clip 创建独立的 IClipHandler 实例，
    /// 根据时间自动触发 Enter/Update/Exit。
    /// </summary>
    public class SimpleTrackRunner : ITrackRunner
    {
        private SkillTrack _track;
        private SkillContext _ctx;
        private readonly Dictionary<SkillClip, IClipHandler> _clipHandlers = new();
        private readonly HashSet<SkillClip> _activeClips = new();

        public void Init(SkillTrack track)
        {
            _track = track;
            foreach (var clip in _track.Clips)
            {
                var handler = ClipHandlerRegistry.CreateHandler(clip.GetType());
                if (handler != null)
                    _clipHandlers[clip] = handler;
            }
        }

        public void OnSkillStart(SkillContext ctx) => _ctx = ctx;

        public void OnTick(float currentTime, float deltaTime)
        {
            foreach (var clip in _track.Clips)
            {
                if (!_clipHandlers.TryGetValue(clip, out var handler))
                    continue;

                bool shouldBeActive = currentTime >= clip.StartTime && currentTime < clip.EndTime;
                bool isActive = _activeClips.Contains(clip);

                if (shouldBeActive && !isActive)
                {
                    _activeClips.Add(clip);
                    handler.OnEnter(clip, _ctx);
                }
                if (shouldBeActive && isActive)
                {
                    handler.OnUpdate(clip, currentTime - clip.StartTime, deltaTime, _ctx);
                }
                if (!shouldBeActive && isActive)
                {
                    _activeClips.Remove(clip);
                    handler.OnExit(clip, _ctx);
                }
            }
        }

        public void OnSkillEnd()
        {
            foreach (var clip in _activeClips)
            {
                if (_clipHandlers.TryGetValue(clip, out var handler))
                    handler.OnExit(clip, _ctx);
            }
            _activeClips.Clear();
        }

        public void OnSkillInterrupt() => OnSkillEnd();
    }
}
