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
}

/// <summary>
/// Shizuku 图调试器，全局静态管理类
/// 管理 Debug 开关、暂停/单步状态、快照、执行记录
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
    /// 是否处于单步模式
    /// </summary>
    private static bool _stepping;
    
    /// <summary>
    /// 恢复执行时需要跳过断点/单步检查的节点 GUID（即当前暂停的节点）
    /// 避免 Step/Continue 后在同一个节点再次触发暂停
    /// </summary>
    private static string _resumingFromNodeGuid;
    
    /// <summary>
    /// 最近一次断点的快照
    /// </summary>
    public static DebugSnapshot CurrentSnapshot { get; private set; }
    
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
    /// 节点触发暂停（断点命中或单步完成），同时拍摄快照
    /// 快照已在调用前捕获，之后调用 Debug.Break() 让 Unity 在帧尾暂停编辑器
    /// </summary>
    public static void Pause(DebugSnapshot snapshot)
    {
        IsPaused = true;
        CurrentSnapshot = snapshot;
        // 快照已保存断点时刻的完整状态，帧尾暂停不影响快照数据正确性
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
    /// 继续执行，直到碰到下一个断点或链结束
    /// </summary>
    public static void Continue(string resumeNodeGuid)
    {
        IsPaused = false;
        _stepping = false;
        _resumingFromNodeGuid = resumeNodeGuid;
        CurrentSnapshot = null;
    }
    
    /// <summary>
    /// 单步执行：执行当前节点后，在下一个节点前暂停
    /// </summary>
    public static void Step(string resumeNodeGuid)
    {
        IsPaused = false;
        _stepping = true;
        _resumingFromNodeGuid = resumeNodeGuid;
        CurrentSnapshot = null;
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
        CurrentSnapshot = null;
        _executedNodesThisFrame.Clear();
        _executedNodesLastFrame.Clear();
    }
}

