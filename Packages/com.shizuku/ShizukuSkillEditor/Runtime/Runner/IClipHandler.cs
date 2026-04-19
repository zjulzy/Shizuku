namespace Shizuku.SkillEditor
{
    /// <summary>
    /// Clip 回调接口（非泛型）。
    /// </summary>
    public interface IClipHandler
    {
        void OnEnter(SkillClip clip, SkillContext ctx);
        void OnUpdate(SkillClip clip, float clipLocalTime, float deltaTime, SkillContext ctx);
        void OnExit(SkillClip clip, SkillContext ctx);
    }

    /// <summary>
    /// 泛型便捷基类，子类只需处理强类型 Clip。
    /// </summary>
    public abstract class ClipHandler<TClip> : IClipHandler where TClip : SkillClip
    {
        public void OnEnter(SkillClip clip, SkillContext ctx) => OnEnterTyped((TClip)clip, ctx);
        public void OnUpdate(SkillClip clip, float localTime, float dt, SkillContext ctx) => OnUpdateTyped((TClip)clip, localTime, dt, ctx);
        public void OnExit(SkillClip clip, SkillContext ctx) => OnExitTyped((TClip)clip, ctx);

        protected abstract void OnEnterTyped(TClip clip, SkillContext ctx);
        protected virtual void OnUpdateTyped(TClip clip, float localTime, float dt, SkillContext ctx) { }
        protected abstract void OnExitTyped(TClip clip, SkillContext ctx);
    }
}

