namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 动画轨道运行器占位。Phase 4 Step 18 完整实现 PlayableGraph。
    /// </summary>
    public class AnimationTrackRunner : ITrackRunner
    {
        public void Init(SkillTrack track) { }
        public void OnSkillStart(SkillContext ctx) { }
        public void OnTick(float currentTime, float deltaTime) { }
        public void OnSkillEnd() { }
        public void OnSkillInterrupt() { }
    }
}

