using System;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 标记在 SkillTrack 派生类上，声明该轨道使用哪种 ITrackRunner。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TrackRunnerAttribute : Attribute
    {
        public Type RunnerType { get; }
        public TrackRunnerAttribute(Type runnerType) => RunnerType = runnerType;
    }
}

