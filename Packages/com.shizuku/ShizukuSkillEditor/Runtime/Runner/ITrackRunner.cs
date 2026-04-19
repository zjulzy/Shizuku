namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 轨道运行器接口。由 SkillPlayer 在运行时为每条 Track 创建。
    /// </summary>
    public interface ITrackRunner
    {
        void Init(SkillTrack track);
        void OnSkillStart(SkillContext ctx);
        void OnTick(float currentTime, float deltaTime);
        void OnSkillEnd();
        void OnSkillInterrupt();
    }
}

