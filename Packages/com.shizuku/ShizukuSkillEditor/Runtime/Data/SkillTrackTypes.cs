using System;

namespace Shizuku.SkillEditor
{
    [Serializable]
    [TrackRunner(typeof(AnimationTrackRunner))]
    public class AnimationTrack : SkillTrack { }

    [Serializable]
    [TrackRunner(typeof(SimpleTrackRunner))]
    public class LogicTrack : SkillTrack { }

    [Serializable]
    [TrackRunner(typeof(SimpleTrackRunner))]
    public class EffectTrack : SkillTrack { }
}

