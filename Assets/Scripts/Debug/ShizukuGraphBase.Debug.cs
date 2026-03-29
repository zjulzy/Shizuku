#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// ShizukuGraphBase 的调试功能部分
/// 负责：快照拍摄、快照还原
/// </summary>
public partial class ShizukuGraphBase
{
    /// <summary>
    /// Debug 模式下的每帧更新（由 Update 调用）
    /// </summary>
    private void DebugUpdate()
    {
        ShizukuDebugger.BeginFrame();
        
        // 暂停中 → 什么都不做，等编辑器按钮驱动 ShizukuDebugger.ResumeExecute
        if (ShizukuDebugger.IsPaused)
            return;
        
        // 新的一帧，从 Root 开始执行（可能碰到断点）
        if (_guid2NodeMap.TryGetValue(RootNodeGUID, out var rootNode) && rootNode is ShizukuRootNode root)
        {
            root.StartExcute();
        }
    }
    
    /// <summary>
    /// 拍摄当前状态的快照（断点命中时由 ShizukuDebugger 调用）
    /// </summary>
    public virtual DebugSnapshot CaptureSnapshot(string pausedAtNodeGuid)
    {
        var clonedGraph = Instantiate(this);
        clonedGraph.name = $"{name}_DebugSnapshot";
        clonedGraph._variableStore = _variableStore?.Clone();
        
        return new DebugSnapshot
        {
            FrameCount = Time.frameCount,
            PausedAtNodeGuid = pausedAtNodeGuid,
            GraphClone = clonedGraph,
        };
    }
    
    /// <summary>
    /// 从调试快照中还原运行时变量，确保恢复执行时的状态与断点时刻一致。
    /// 由 ShizukuDebugger 在恢复执行前调用。
    /// </summary>
    public virtual void RestoreVariablesFromSnapshot()
    {
        var clonedStore = ShizukuDebugger.CurrentSnapshot?.GraphClone?.VariableStore;
        if (clonedStore != null)
        {
            _variableStore = clonedStore.Clone();
        }
    }
}
#endif

