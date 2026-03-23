using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 节点执行结果，用于调试时中断递归链
/// </summary>
public enum ExecuteResult
{
    /// <summary>链正常完成</summary>
    Continue,
    /// <summary>链被断点/单步中断</summary>
    Halted,
}

[Serializable]
public abstract class ShizukuRunnableNode : ShizukuNormalNode
{
    public sealed override bool SupportControlInput => true;
    public sealed override bool SupportControlOutput => true;
    
    
    #region 编辑器中调试相关

    public bool HasBreakPoint = false;

    #endregion

    public override void Init(ShizukuGraphBase parentGraph)
    {
        base.Init(parentGraph);
    }

    public ExecuteResult Execute()
    {
        // ---- Debug：执行前检查是否需要暂停 ----
        if (ShizukuDebugger.Enabled)
        {
            // 恢复执行的起点节点 → 跳过断点/单步检查，直接执行
            if (!ShizukuDebugger.IsResumingFrom(GUID))
            {
                if (HasBreakPoint || ShizukuDebugger.ShouldPauseAfterStep())
                {
                    // 拍快照（此刻节点尚未执行，快照反映断点前状态）
                    var snapshot = _parentGraph.CaptureSnapshot(GUID);
                    ShizukuDebugger.Pause(snapshot);
                    
                    // 记录恢复点
                    _parentGraph.PendingResumeNodeGuid = GUID;
                    
                    return ExecuteResult.Halted;
                }
            }
            
            ShizukuDebugger.RecordNodeExecution(GUID);
        }
        
        // ---- 正常执行 ----
        GetInputValues();
        OnExecute();

        if (OnSelectNextNode(out var guid))
        {
            if (_parentGraph.Guid2NodeMap.TryGetValue(guid, out var nextNode))
            {
                if (nextNode is ShizukuRunnableNode runnable)
                {
                    return runnable.Execute();
                }
                else
                {
                    Debug.LogError($"Next node is not a runnable node: {guid}");
                }
            }
            else
            {
                Debug.LogError($"Next node not found: {guid}");
            }
        }
        
        return ExecuteResult.Continue;
    }

    protected abstract void OnExecute();
    protected abstract bool OnSelectNextNode(out string nextNodeGUID);
}