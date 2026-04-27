using System.Collections.Generic;
using UnityEngine;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 动画轨道运行器。通过 SkillPlayer 的槽位 API 插拔动画，
    /// 自身不持有任何 Playable 资源。
    /// </summary>
    public class AnimationTrackRunner : ITrackRunner
    {
        private SkillTrack _track;
        private SkillPlayer _player;

        private readonly List<AnimationClipData> _clips = new();
        private readonly List<int> _slots = new();
        private readonly HashSet<int> _active = new();

        private float _firstStart;
        private float _lastEnd;
        private const float LayerBlendIn = 0.1f;
        private const float LayerBlendOut = 0.15f;

        public void Init(SkillTrack track)
        {
            _track = track;
            foreach (var c in _track.Clips)
                if (c is AnimationClipData acd)
                    _clips.Add(acd);
        }

        public void OnSkillStart(SkillContext ctx)
        {
            _player = ctx.Player;
            if (_player == null || _clips.Count == 0) return;

            _firstStart = float.MaxValue;
            _lastEnd = 0f;
            foreach (var acd in _clips)
            {
                _slots.Add(_player.AcquireSlot(acd.Clip));
                if (acd.StartTime < _firstStart) _firstStart = acd.StartTime;
                if (acd.EndTime > _lastEnd) _lastEnd = acd.EndTime;
            }
        }

        public void OnTick(float currentTime, float deltaTime)
        {
            if (_player == null || _slots.Count == 0) return;

            _player.SkillLayerWeight = CalcLayerWeight(currentTime);

            for (int i = 0; i < _clips.Count; i++)
            {
                var acd = _clips[i];
                int slot = _slots[i];
                if (slot < 0) continue;

                bool inRange = currentTime >= acd.StartTime && currentTime < acd.EndTime;

                if (inRange)
                {
                    float local = currentTime - acd.StartTime;
                    if (_active.Add(i))
                    {
                        _player.SetSlotTime(slot, 0);
                        _player.SetSlotPlaying(slot, true);
                    }
                    _player.SetSlotWeight(slot, EvalWeight(acd, local));
                    _player.SetSlotTime(slot, local);
                }
                else if (_active.Remove(i))
                {
                    _player.SetSlotWeight(slot, 0f);
                    _player.SetSlotPlaying(slot, false);
                }
            }

            _player.NormalizeSlotWeights();
        }

        public void OnSkillEnd() => ReleaseAll();
        public void OnSkillInterrupt() => ReleaseAll();

        private void ReleaseAll()
        {
            if (_player == null) return;
            _player.SkillLayerWeight = 0f;
            foreach (int slot in _slots) _player.ReleaseSlot(slot);
            _slots.Clear();
            _active.Clear();
        }

        private float CalcLayerWeight(float t)
        {
            float w = 1f;
            float elapsed = t - _firstStart;
            if (elapsed < 0f) return 0f;
            if (LayerBlendIn > 0f && elapsed < LayerBlendIn)
                w = elapsed / LayerBlendIn;
            float remaining = _lastEnd - t;
            if (LayerBlendOut > 0f && remaining >= 0f && remaining < LayerBlendOut)
                w = Mathf.Min(w, remaining / LayerBlendOut);
            return Mathf.Clamp01(w);
        }

        private static float EvalWeight(AnimationClipData acd, float local)
        {
            float w = 1f;
            if (acd.BlendIn > 0f && local < acd.BlendIn)
            {
                float t = local / acd.BlendIn;
                w *= (acd.BlendInCurve != null && acd.BlendInCurve.length > 0)
                    ? acd.BlendInCurve.Evaluate(t) : t;
            }
            if (acd.BlendOut > 0f && local > acd.Duration - acd.BlendOut)
            {
                float t = (local - (acd.Duration - acd.BlendOut)) / acd.BlendOut;
                w *= (acd.BlendOutCurve != null && acd.BlendOutCurve.length > 0)
                    ? acd.BlendOutCurve.Evaluate(t) : (1f - t);
            }
            return Mathf.Clamp01(w);
        }
    }
}
