#if UNITY_EDITOR
using System.Collections.Generic;

/// <summary>
/// 断点时刻的快照数据
/// </summary>
public class DebugSnapshot
{
    /// <summary>
    /// 快照时的帧号
    /// </summary>
    public int FrameCount;
    
    /// <summary>
    /// 暂停在哪个节点的 GUID
    /// </summary>
    public string PausedAtNodeGuid;
    
    /// <summary>
    /// 整个图的深拷贝（包含所有序列化数据 + 运行时变量）
    /// </summary>
    public ShizukuGraphBase GraphClone;
    
    /// <summary>
    /// Behavior 字段快照（仅蓝图类图有值）
    /// Key: 字段名, Value: 断点时刻的字段值
    /// </summary>
    public Dictionary<string, object> BehaviorFields;
}

/// <summary>
/// Shizuku 图调试器，全局静态管理类
/// 管理 Debug 开关、暂停/单步状态、快照、执行记录、恢复执行
/// </summary>
public static class ShizukuDebugger
{
    /// <summary>
    /// 全局 Debug 开关，由编辑器控制
    /// </summary>
    public static bool Enabled;
    
    /// <summary>
    /// 当前是否处于暂停状态（碰到断点或单步后）
    /// </summary>
    public static bool IsPaused { get; private set; }
    
    /// <summary>
    /// 当前暂停所在的运行时图实例（暂停时记录，恢复后清除）
    /// </summary>
    public static ShizukuGraphBase PausedGraph { get; private set; }
    
    /// <summary>
    /// 恢复执行的目标节点 GUID（暂停时记录，恢复后清除）
    /// </summary>
    public static string PendingResumeNodeGuid { get; private set; }
    
    /// <summary>
    /// 是否处于单步模式
    /// </summary>
    private static bool _stepping;
    
    /// <summary>
    /// 恢复执行时需要跳过断点/单步检查的节点 GUID（即当前暂停的节点）
    /// 避免 Step/Continue 后在同一个节点再次触发暂停
    /// </summary>
    private static string _resumingFromNodeGuid;
    
    /// <summary>
    /// 最近一次断点的快照（私有，外部通过只读访问器获取具体数据）
    /// </summary>
    private static DebugSnapshot _currentSnapshot;
    
    // ---- 快照访问 ----
    
    /// <summary>
    /// 获取当前断点快照（暂停时有值，恢复后为 null）
    /// </summary>
    public static DebugSnapshot CurrentSnapshot => _currentSnapshot;
    
    /// <summary>
    /// 全局断点集合（存储节点 GUID）。
    /// 断点与图实例无关，只看 GUID，这样编辑器设置的断点在运行时克隆体上也能生效。
    /// </summary>
    private static readonly HashSet<string> _breakpointNodeGuids = new HashSet<string>();
    
    /// <summary>
    /// 本帧执行过的节点 GUID 列表（供编辑器高亮使用）
    /// </summary>
    private static List<string> _executedNodesThisFrame = new List<string>();
    
    /// <summary>
    /// 上一帧执行过的节点（用于编辑器在下一帧读取）
    /// </summary>
    private static List<string> _executedNodesLastFrame = new List<string>();
    public static IReadOnlyList<string> ExecutedNodesLastFrame => _executedNodesLastFrame;
    
    // ---- 每帧调用 ----
    
    /// <summary>
    /// 每帧开始时调用，交换帧记录缓冲
    /// </summary>
    public static void BeginFrame()
    {
        (_executedNodesThisFrame, _executedNodesLastFrame) = (_executedNodesLastFrame, _executedNodesThisFrame);
        _executedNodesThisFrame.Clear();
    }
    
    // ---- 断点管理 ----
    
    /// <summary>
    /// 检查指定节点是否有断点
    /// </summary>
    public static bool HasBreakpoint(string nodeGuid) => _breakpointNodeGuids.Contains(nodeGuid);
    
    /// <summary>
    /// 切换指定节点的断点状态，返回切换后是否有断点
    /// </summary>
    public static bool ToggleBreakpoint(string nodeGuid)
    {
        if (!_breakpointNodeGuids.Remove(nodeGuid))
        {
            _breakpointNodeGuids.Add(nodeGuid);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 清除所有断点
    /// </summary>
    public static void ClearAllBreakpoints()
    {
        _breakpointNodeGuids.Clear();
    }
    
    // ---- 节点执行时调用 ----
    
    /// <summary>
    /// 记录节点执行
    /// </summary>
    public static void RecordNodeExecution(string nodeGuid)
    {
        _executedNodesThisFrame.Add(nodeGuid);
    }
    
    /// <summary>
    /// 检查当前节点是否是恢复执行的起点节点。
    /// 如果是，清除标记并返回 true，调用方应跳过该节点的断点/单步检查。
    /// </summary>
    public static bool IsResumingFrom(string nodeGuid)
    {
        if (_resumingFromNodeGuid != null && _resumingFromNodeGuid == nodeGuid)
        {
            _resumingFromNodeGuid = null;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 节点触发暂停（断点命中或单步完成）。
    /// 由 Debugger 拍摄快照、记录恢复信息，然后调用 Debug.Break() 让 Unity 在帧尾暂停。
    /// </summary>
    public static void Pause(ShizukuGraphBase graph, string pausedAtNodeGuid)
    {
        IsPaused = true;
        PausedGraph = graph;
        PendingResumeNodeGuid = pausedAtNodeGuid;
        _currentSnapshot = graph.CaptureSnapshot(pausedAtNodeGuid);
        UnityEngine.Debug.Break();
    }
    
    /// <summary>
    /// 检查执行完一个节点后是否需要暂停（单步模式）
    /// </summary>
    public static bool ShouldPauseAfterStep()
    {
        if (_stepping)
        {
            _stepping = false;
            return true;
        }
        return false;
    }
    
    // ---- 编辑器控制按钮调用 ----
    
    /// <summary>
    /// 从断点处恢复执行。
    /// step=false 表示继续运行直到下一个断点或链结束；
    /// step=true  表示只执行一个节点后暂停。
    /// </summary>
    public static void ResumeExecute(bool step)
    {
        if (PausedGraph == null || string.IsNullOrEmpty(PendingResumeNodeGuid))
            return;
        
        if (!PausedGraph.Guid2NodeMap.TryGetValue(PendingResumeNodeGuid, out var node)
            || node is not ShizukuRunnableNode runnable)
            return;
        
        // 先还原快照变量（必须在清除 _currentSnapshot 之前）
        PausedGraph.RestoreVariablesFromSnapshot();
        
        var resumeGuid = PendingResumeNodeGuid;
        
        // 清除暂停状态，设置跳过恢复点的标记
        IsPaused = false;
        _stepping = step;
        _resumingFromNodeGuid = resumeGuid;
        _currentSnapshot = null;
        PausedGraph = null;
        PendingResumeNodeGuid = null;
        
        var result = runnable.Execute();
        
        // 链自然结束（最后一个节点没有后续），清理残留的单步标志
        if (step && result == ExecuteResult.Continue)
        {
            _stepping = false;
        }
    }
    
    /// <summary>
    /// 停止调试，重置所有状态
    /// </summary>
    public static void Stop()
    {
        Enabled = false;
        IsPaused = false;
        _stepping = false;
        _resumingFromNodeGuid = null;
        _currentSnapshot = null;
        PausedGraph = null;
        PendingResumeNodeGuid = null;
        _executedNodesThisFrame.Clear();
        _executedNodesLastFrame.Clear();
    }
}
#endif
