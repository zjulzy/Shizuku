#if UNITY_EDITOR
/// <summary>
/// ShizukuRunnableNode 的调试功能部分
/// 负责：断点检查、单步检查、执行记录
/// </summary>
public abstract partial class ShizukuRunnableNode
{
    /// <summary>
    /// 执行前的调试检查。
    /// 返回 null 表示无需中断，继续正常执行；
    /// 返回 Halted 表示已被断点/单步暂停，应立即返回。
    /// </summary>
    private ExecuteResult DebugCheck()
    {
        // 恢复执行的起点节点 → 跳过断点/单步检查，直接执行
        if (!ShizukuDebugger.IsResumingFrom(GUID))
        {
            if (ShizukuDebugger.HasBreakpoint(GUID) || ShizukuDebugger.ShouldPauseAfterStep())
            {
                ShizukuDebugger.Pause(_parentGraph, GUID);
                return ExecuteResult.Halted;
            }
        }
        
        ShizukuDebugger.RecordNodeExecution(GUID);
        return ExecuteResult.Continue;
    }
}
#endif

