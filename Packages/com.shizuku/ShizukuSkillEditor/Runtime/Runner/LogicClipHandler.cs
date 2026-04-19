using UnityEngine;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 逻辑事件 Clip 处理器。Clip 进入时触发事件。
    /// </summary>
    public class LogicClipHandler : ClipHandler<LogicClipData>
    {
        protected override void OnEnterTyped(LogicClipData clip, SkillContext ctx)
        {
            Debug.Log($"[SkillEditor] 逻辑事件触发: {clip.EventName}");
        }

        protected override void OnExitTyped(LogicClipData clip, SkillContext ctx) { }
    }
}

