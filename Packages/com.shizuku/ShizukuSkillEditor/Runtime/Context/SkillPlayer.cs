using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 技能播放器（MonoBehaviour）。挂在角色上，负责：
    /// 1. 持久化 PlayableGraph（Awake 创建，OnDestroy 销毁）
    /// 2. 技能播放调度（Play / Stop / Interrupt）
    /// 3. 动画槽位管理（供 AnimationTrackRunner 调用）
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class SkillPlayer : MonoBehaviour
    {
        // ---- 配置 ----
        [SerializeField] private int _maxAnimSlots = 8;

        // ---- 技能播放状态 ----
        private List<ITrackRunner> _runners;
        private float _currentTime;
        private float _duration;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;
        public float CurrentTime => _currentTime;
        public float Duration => _duration;

        // ---- Playable Graph ----
        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationLayerMixerPlayable _layerMixer;
        private AnimationMixerPlayable _skillMixer;

        private bool[] _slotUsed;
        private AnimationClipPlayable[] _slotPlayables;

        public bool IsGraphReady => _graph.IsValid();

        // ---- TrackRunner 类型缓存 ----
        private static readonly Dictionary<Type, Type> _runnerTypeCache = new();

        // ============================================================
        // MonoBehaviour 生命周期
        // ============================================================

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            BuildGraph();
        }

        private void Update()
        {
            if (_isPlaying)
                Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
                _graph.Destroy();
        }

        // ============================================================
        // PlayableGraph 构建（仅 Awake 调用一次）
        // ============================================================

        private void BuildGraph()
        {
            _graph = PlayableGraph.Create("SkillPlayer");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            _layerMixer = AnimationLayerMixerPlayable.Create(_graph, 2);
            var output = AnimationPlayableOutput.Create(_graph, "Output", _animator);
            output.SetSourcePlayable(_layerMixer);

            // Layer 0: 原始 AnimatorController
            if (_animator.runtimeAnimatorController != null)
            {
                var ctrl = AnimatorControllerPlayable.Create(_graph, _animator.runtimeAnimatorController);
                _animator.runtimeAnimatorController = null; // 交由 graph 驱动
                _graph.Connect(ctrl, 0, _layerMixer, 0);
            }
            _layerMixer.SetInputWeight(0, 1f);

            // Layer 1: 技能动画槽位池
            _skillMixer = AnimationMixerPlayable.Create(_graph, _maxAnimSlots);
            _graph.Connect(_skillMixer, 0, _layerMixer, 1);
            _layerMixer.SetInputWeight(1, 0f);

            _slotUsed = new bool[_maxAnimSlots];
            _slotPlayables = new AnimationClipPlayable[_maxAnimSlots];

            _graph.Play();
        }

        // ============================================================
        // 技能播放 API
        // ============================================================

        public void Play(ShizukuSkillConfig config, SkillContext ctx = null)
        {
            if (_isPlaying) Interrupt();

            var context = ctx ?? new SkillContext();
            context.Caster = gameObject;
            context.CasterAnimator = _animator;
            context.Player = this;

            _duration = config.Duration;
            _currentTime = 0f;
            _isPlaying = true;

            _runners = new List<ITrackRunner>();
            foreach (var track in config.Tracks)
            {
                if (!track.Enabled) continue;
                var runner = CreateRunnerForTrack(track);
                runner.Init(track);
                runner.OnSkillStart(context);
                _runners.Add(runner);
            }
        }

        private void Tick(float deltaTime)
        {
            if (!_isPlaying) return;
            _currentTime += deltaTime;

            foreach (var runner in _runners)
                runner.OnTick(_currentTime, deltaTime);

            // 推进 graph
            if (_graph.IsValid())
                _graph.Evaluate(deltaTime);

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

        // ============================================================
        // 动画槽位 API（AnimationTrackRunner 调用）
        // ============================================================

        /// <summary>技能动画层整体权重 (0~1)。</summary>
        public float SkillLayerWeight
        {
            get => _layerMixer.IsValid() ? _layerMixer.GetInputWeight(1) : 0f;
            set { if (_layerMixer.IsValid()) _layerMixer.SetInputWeight(1, Mathf.Clamp01(value)); }
        }

        /// <summary>申请槽位，填入 clip。返回 slotIndex，-1 表示已满。</summary>
        public int AcquireSlot(AnimationClip clip)
        {
            if (clip == null) return -1;
            for (int i = 0; i < _maxAnimSlots; i++)
            {
                if (_slotUsed[i]) continue;
                _slotUsed[i] = true;

                var p = AnimationClipPlayable.Create(_graph, clip);
                p.Pause();
                _graph.Connect(p, 0, _skillMixer, i);
                _skillMixer.SetInputWeight(i, 0f);
                _slotPlayables[i] = p;
                return i;
            }
            Debug.LogWarning("[SkillPlayer] 动画槽位已满");
            return -1;
        }

        /// <summary>释放槽位。</summary>
        public void ReleaseSlot(int slot)
        {
            if (slot < 0 || slot >= _maxAnimSlots || !_slotUsed[slot]) return;
            _slotUsed[slot] = false;
            _skillMixer.SetInputWeight(slot, 0f);
            if (_slotPlayables[slot].IsValid())
            {
                _graph.Disconnect(_skillMixer, slot);
                _slotPlayables[slot].Destroy();
            }
        }

        public void SetSlotWeight(int slot, float w)
        {
            if (slot >= 0 && slot < _maxAnimSlots)
                _skillMixer.SetInputWeight(slot, w);
        }

        public void SetSlotTime(int slot, float t)
        {
            if (slot >= 0 && slot < _maxAnimSlots && _slotPlayables[slot].IsValid())
                _slotPlayables[slot].SetTime(t);
        }

        public void SetSlotPlaying(int slot, bool play)
        {
            if (slot < 0 || slot >= _maxAnimSlots || !_slotPlayables[slot].IsValid()) return;
            if (play) _slotPlayables[slot].Play();
            else _slotPlayables[slot].Pause();
        }

        /// <summary>归一化活跃槽位权重总和为 1。</summary>
        public void NormalizeSlotWeights()
        {
            float total = 0f;
            for (int i = 0; i < _maxAnimSlots; i++)
                total += _skillMixer.GetInputWeight(i);
            if (total > 0f && !Mathf.Approximately(total, 1f))
            {
                float inv = 1f / total;
                for (int i = 0; i < _maxAnimSlots; i++)
                    _skillMixer.SetInputWeight(i, _skillMixer.GetInputWeight(i) * inv);
            }
        }

        // ============================================================
        // TrackRunner 工厂
        // ============================================================

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
