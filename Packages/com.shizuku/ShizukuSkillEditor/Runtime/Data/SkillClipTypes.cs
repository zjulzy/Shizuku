using System;
using UnityEngine;

namespace Shizuku.SkillEditor
{
    // ---- 动画 ----
    [Serializable]
    [ClipForTrack(typeof(AnimationTrack), "动画片段")]
    public class AnimationClipData : SkillClip
    {
        public AnimationClip Clip;
        public float BlendIn;
        public float BlendOut;
        public AnimationCurve BlendInCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve BlendOutCurve = AnimationCurve.Linear(0, 1, 1, 0);
    }

    // ---- 特效 ----
    [Serializable]
    [ClipForTrack(typeof(EffectTrack), "特效")]
    public class VfxClipData : SkillClip
    {
        public GameObject Prefab;
        public string AttachBone;
        public Vector3 Offset;
    }

    // ---- 音效 ----
    [Serializable]
    [ClipForTrack(typeof(EffectTrack), "音效")]
    public class SfxClipData : SkillClip
    {
        public AudioClip Clip;
        public float Volume = 1f;
    }

    // ---- 逻辑事件 ----
    [Serializable]
    [ClipForTrack(typeof(LogicTrack), "逻辑事件")]
    public class LogicClipData : SkillClip
    {
        public string EventName;
    }
}

