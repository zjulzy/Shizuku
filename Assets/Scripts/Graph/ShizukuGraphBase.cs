using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShizukuGraph", menuName = "Shizuku/Graph", order = 1)]
public class ShizukuGraphBase : ScriptableObject
{
    [SerializeField]
    public string GUID;
    
    [SerializeReference]
    private List<ShizukuNodeBase> _nodes = new List<ShizukuNodeBase>();
    public List<ShizukuNodeBase> Nodes => _nodes;
    
    [SerializeReference]
    private List<ParameterEdge> _edges = new List<ParameterEdge>();
    public List<ParameterEdge> Edges => _edges;
    
    [SerializeField]
    private List<GroupData> _groups = new List<GroupData>();
    public List<GroupData> Groups => _groups;
    
    [SerializeField] 
    public string RootNodeGUID = null;
    
    // 变量系统
    [SerializeField]
    private List<GraphVariable> _variables = new List<GraphVariable>();
    public List<GraphVariable> Variables => _variables;
    
    [NonSerialized]
    private Dictionary<string , ShizukuNodeBase> _guid2NodeMap = new Dictionary<string, ShizukuNodeBase>();
    public Dictionary<string , ShizukuNodeBase> Guid2NodeMap => _guid2NodeMap;
    
    [NonSerialized]
    private Dictionary<string , ParameterEdge> _guid2EdgeMap = new Dictionary<string, ParameterEdge>();
    public Dictionary<string , ParameterEdge> Guid2EdgeMap => _guid2EdgeMap;
    
    // 运行时变量存储
    [NonSerialized] private RuntimeVariableStore _variableStore;
    public RuntimeVariableStore VariableStore => _variableStore;
    
    // 调试恢复点：断点/单步中断后，记录从哪个节点恢复执行
    [NonSerialized] public string PendingResumeNodeGuid;

    #region Debug 相关

    /// <summary>
    /// 给节点设置/取消断点
    /// </summary>
    public void ToggleBreakpoint(string nodeGuid)
    {
        if (_guid2NodeMap.TryGetValue(nodeGuid, out var node) && node is ShizukuRunnableNode runnable)
        {
            runnable.HasBreakPoint = !runnable.HasBreakPoint;
        }
    }
    
    /// <summary>
    /// 拍摄当前状态的快照（断点命中时由节点调用）
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

    #endregion
    
    public void AddNode(ShizukuNodeBase node)
    {
        _nodes.Add(node);
    }
    
    public void AddParameterEdge(ShizukuNodeBase sourceNode, string outputPortName, ShizukuNodeBase targetNode, string inputPortName)
    {
        ParameterEdge edge = new ParameterEdge(
            sourceNode.GUID,
            outputPortName,
            targetNode.GUID,
            inputPortName
        );
        _edges.Add(edge);
    }

    public virtual void Init()
    {
        // 初始化节点
        _guid2NodeMap.Clear();
        foreach (var node in _nodes)
        {
            _guid2NodeMap[node.GUID] = node;
            node.Init(this);
        }
        
        // 初始化边
        _guid2EdgeMap.Clear();
        foreach (var edge in _edges)
        {
            _guid2EdgeMap[edge.GUID] = edge;
            edge.ConnectPorts(this);
        }
        
        // 初始化变量
        InitVariables();
    }
    
    public void Update()
    {
        if (string.IsNullOrEmpty(RootNodeGUID))
            return;
        
        if (!ShizukuDebugger.Enabled)
        {
            // ---- 正常模式：递归一帧跑完 ----
            if (_guid2NodeMap.TryGetValue(RootNodeGUID, out var rootNode) && rootNode is ShizukuRootNode root)
            {
                root.StartExcute();
            }
            return;
        }
        
        // ---- Debug 模式 ----
        
        ShizukuDebugger.BeginFrame();
        
        // 暂停中 → 什么都不做，等编辑器按钮驱动 StepExecute / ContinueExecute
        if (ShizukuDebugger.IsPaused)
            return;
        
        // 有恢复点 → 说明上次被断点/单步中断了，等编辑器按钮驱动，不在 Update 里自动恢复
        if (!string.IsNullOrEmpty(PendingResumeNodeGuid))
            return;
        
        // 无恢复点 → 新的一帧，从 Root 开始执行（可能碰到断点）
        if (_guid2NodeMap.TryGetValue(RootNodeGUID, out var debugRootNode) && debugRootNode is ShizukuRootNode debugRoot)
        {
            debugRoot.StartExcute();
        }
    }
    
    /// <summary>
    /// 由编辑器"单步"按钮调用，同帧内执行一个节点后暂停
    /// </summary>
    public void StepExecute()
    {
        if (string.IsNullOrEmpty(PendingResumeNodeGuid))
            return;
        
        if (!_guid2NodeMap.TryGetValue(PendingResumeNodeGuid, out var node) || node is not ShizukuRunnableNode runnable)
            return;
        
        // 从快照还原变量状态，防止断点到恢复之间被外部逻辑污染
        RestoreVariablesFromSnapshot();
        
        // 设置单步标志，同时告知调试器跳过恢复起点节点的断点检查
        // 注意：必须在 RestoreVariablesFromSnapshot 之后调用，因为 Step 会清除 CurrentSnapshot
        ShizukuDebugger.Step(PendingResumeNodeGuid);
        
        // 清除恢复点（Execute 内部如果再次中断会重新设置）
        PendingResumeNodeGuid = null;
        
        runnable.Execute();
    }
    
    /// <summary>
    /// 由编辑器"继续"按钮调用，同帧内从断点处继续执行直到链结束或下一个断点
    /// </summary>
    public void ContinueExecute()
    {
        if (string.IsNullOrEmpty(PendingResumeNodeGuid))
            return;
        
        if (!_guid2NodeMap.TryGetValue(PendingResumeNodeGuid, out var node) || node is not ShizukuRunnableNode runnable)
            return;
        
        // 从快照还原变量状态，防止断点到恢复之间被外部逻辑污染
        RestoreVariablesFromSnapshot();
        
        // 告知调试器跳过恢复起点节点的断点检查
        ShizukuDebugger.Continue(PendingResumeNodeGuid);
        
        // 清除恢复点
        PendingResumeNodeGuid = null;
        
        runnable.Execute();
    }
    
    /// <summary>
    /// 从调试快照中还原运行时变量，确保恢复执行时的状态与断点时刻一致。
    /// 断点命中 → Debug.Break() → 帧尾暂停，这之间外部脚本可能修改了变量，
    /// 所以恢复执行前必须从快照 clone 回来。
    /// </summary>
    protected virtual void RestoreVariablesFromSnapshot()
    {
        var snapshot = ShizukuDebugger.CurrentSnapshot;
        if (snapshot?.GraphClone?.VariableStore != null)
        {
            _variableStore = snapshot.GraphClone.VariableStore.Clone();
        }
    }
    
    #region 变量管理
    
    /// <summary>
    /// 初始化运行时变量存储
    /// </summary>
    private void InitVariables()
    {
        _variableStore = new RuntimeVariableStore();
        _variableStore.Init();
        _variableStore.LoadFromVariables(_variables);
    }
    
    // 零装箱的变量访问方法（委托给 RuntimeVariableStore）
    public bool TryGetVariableInt(string guid, out int value) => _variableStore.Ints.TryGetValue(guid, out value);
    public bool TryGetVariableFloat(string guid, out float value) => _variableStore.Floats.TryGetValue(guid, out value);
    public bool TryGetVariableBool(string guid, out bool value) => _variableStore.Bools.TryGetValue(guid, out value);
    public bool TryGetVariableString(string guid, out string value) => _variableStore.Strings.TryGetValue(guid, out value);
    public bool TryGetVariableVector2(string guid, out Vector2 value) => _variableStore.Vector2s.TryGetValue(guid, out value);
    public bool TryGetVariableVector3(string guid, out Vector3 value) => _variableStore.Vector3s.TryGetValue(guid, out value);
    public bool TryGetVariableGameObject(string guid, out GameObject value) => _variableStore.GameObjects.TryGetValue(guid, out value);
    public bool TryGetVariableTransform(string guid, out Transform value) => _variableStore.Transforms.TryGetValue(guid, out value);
    public bool TryGetVariableColor(string guid, out Color value) => _variableStore.Colors.TryGetValue(guid, out value);
    
    public void SetVariableInt(string guid, int value) => _variableStore.Ints[guid] = value;
    public void SetVariableFloat(string guid, float value) => _variableStore.Floats[guid] = value;
    public void SetVariableBool(string guid, bool value) => _variableStore.Bools[guid] = value;
    public void SetVariableString(string guid, string value) => _variableStore.Strings[guid] = value;
    public void SetVariableVector2(string guid, Vector2 value) => _variableStore.Vector2s[guid] = value;
    public void SetVariableVector3(string guid, Vector3 value) => _variableStore.Vector3s[guid] = value;
    public void SetVariableGameObject(string guid, GameObject value) => _variableStore.GameObjects[guid] = value;
    public void SetVariableTransform(string guid, Transform value) => _variableStore.Transforms[guid] = value;
    public void SetVariableColor(string guid, Color value) => _variableStore.Colors[guid] = value;
    
    // 编辑器辅助方法
    public GraphVariable GetVariableByGUID(string guid)
    {
        return _variables.Find(v => v.GUID == guid);
    }
    
    public GraphVariable GetVariableByName(string name)
    {
        return _variables.Find(v => v.Name == name);
    }
    
    public void AddVariable(GraphVariable variable)
    {
        _variables.Add(variable);
    }
    
    public void RemoveVariable(string guid)
    {
        _variables.RemoveAll(v => v.GUID == guid);
    }
    
    public bool RenameVariable(string guid, string newName)
    {
        var variable = GetVariableByGUID(guid);
        if (variable != null)
        {
            variable.Name = newName;
            return true;
        }
        return false;
    }
    
    #endregion
}
