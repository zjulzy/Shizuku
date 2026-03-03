using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditorInternal;
using UnityEngine;

[CreateAssetMenu(fileName = "ShizukuGraph", menuName = "Shizuku/Graph", order = 1)]
public class ShizukuGraphBase :ScriptableObject
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
    
    // 运行时变量存储（分类型字典，零装箱）
    [NonSerialized] private Dictionary<string, int> _runtimeInts;
    [NonSerialized] private Dictionary<string, float> _runtimeFloats;
    [NonSerialized] private Dictionary<string, bool> _runtimeBools;
    [NonSerialized] private Dictionary<string, string> _runtimeStrings;
    [NonSerialized] private Dictionary<string, Vector2> _runtimeVector2s;
    [NonSerialized] private Dictionary<string, Vector3> _runtimeVector3s;
    [NonSerialized] private Dictionary<string, GameObject> _runtimeGameObjects;
    [NonSerialized] private Dictionary<string, Transform> _runtimeTransforms;
    [NonSerialized] private Dictionary<string, Color> _runtimeColors;
    
    
    
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
        if (!string.IsNullOrEmpty(RootNodeGUID))
        {
            if (_guid2NodeMap.TryGetValue(RootNodeGUID, out var rootNode))
            {
                if (rootNode is ShizukuRootNode)
                {
                    (rootNode as ShizukuRootNode).StartExcute();
                }
            }
        }
    }
    
    #region 变量管理
    
    /// <summary>
    /// 初始化运行时变量存储（零装箱）
    /// </summary>
    private void InitVariables()
    {
        _runtimeInts = new Dictionary<string, int>();
        _runtimeFloats = new Dictionary<string, float>();
        _runtimeBools = new Dictionary<string, bool>();
        _runtimeStrings = new Dictionary<string, string>();
        _runtimeVector2s = new Dictionary<string, Vector2>();
        _runtimeVector3s = new Dictionary<string, Vector3>();
        _runtimeGameObjects = new Dictionary<string, GameObject>();
        _runtimeTransforms = new Dictionary<string, Transform>();
        _runtimeColors = new Dictionary<string, Color>();
        
        foreach (var variable in _variables)
        {
            switch (variable.Type)
            {
                case VariableType.Int:
                    _runtimeInts[variable.GUID] = variable.IntValue;
                    break;
                case VariableType.Float:
                    _runtimeFloats[variable.GUID] = variable.FloatValue;
                    break;
                case VariableType.Bool:
                    _runtimeBools[variable.GUID] = variable.BoolValue;
                    break;
                case VariableType.String:
                    _runtimeStrings[variable.GUID] = variable.StringValue;
                    break;
                case VariableType.Vector2:
                    _runtimeVector2s[variable.GUID] = variable.Vector2Value;
                    break;
                case VariableType.Vector3:
                    _runtimeVector3s[variable.GUID] = variable.Vector3Value;
                    break;
                case VariableType.GameObject:
                    _runtimeGameObjects[variable.GUID] = variable.GameObjectValue;
                    break;
                case VariableType.Transform:
                    _runtimeTransforms[variable.GUID] = variable.TransformValue;
                    break;
                case VariableType.Color:
                    _runtimeColors[variable.GUID] = variable.ColorValue;
                    break;
            }
        }
    }
    
    // 零装箱的泛型变量访问方法
    public bool TryGetVariableInt(string guid, out int value)
    {
        if (_runtimeInts != null && _runtimeInts.TryGetValue(guid, out value))
            return true;
        value = default;
        return false;
    }
    
    public bool TryGetVariableFloat(string guid, out float value)
    {
        if (_runtimeFloats != null && _runtimeFloats.TryGetValue(guid, out value))
            return true;
        value = default;
        return false;
    }
    
    public bool TryGetVariableBool(string guid, out bool value)
    {
        if (_runtimeBools != null && _runtimeBools.TryGetValue(guid, out value))
            return true;
        value = default;
        return false;
    }
    
    public bool TryGetVariableString(string guid, out string value)
    {
        if (_runtimeStrings != null && _runtimeStrings.TryGetValue(guid, out value))
            return true;
        value = default;
        return false;
    }
    
    public bool TryGetVariableVector2(string guid, out Vector2 value)
    {
        if (_runtimeVector2s != null && _runtimeVector2s.TryGetValue(guid, out value))
            return true;
        value = default;
        return false;
    }
    
    public bool TryGetVariableVector3(string guid, out Vector3 value)
    {
        if (_runtimeVector3s != null && _runtimeVector3s.TryGetValue(guid, out value))
            return true;
        value = default;
        return false;
    }
    
    public bool TryGetVariableGameObject(string guid, out GameObject value)
    {
        if (_runtimeGameObjects != null && _runtimeGameObjects.TryGetValue(guid, out value))
            return true;
        value = default;
        return false;
    }
    
    public bool TryGetVariableTransform(string guid, out Transform value)
    {
        if (_runtimeTransforms != null && _runtimeTransforms.TryGetValue(guid, out value))
            return true;
        value = default;
        return false;
    }
    
    public bool TryGetVariableColor(string guid, out Color value)
    {
        if (_runtimeColors != null && _runtimeColors.TryGetValue(guid, out value))
            return true;
        value = default;
        return false;
    }
    
    public void SetVariableInt(string guid, int value) { if (_runtimeInts != null) _runtimeInts[guid] = value; }
    public void SetVariableFloat(string guid, float value) { if (_runtimeFloats != null) _runtimeFloats[guid] = value; }
    public void SetVariableBool(string guid, bool value) { if (_runtimeBools != null) _runtimeBools[guid] = value; }
    public void SetVariableString(string guid, string value) { if (_runtimeStrings != null) _runtimeStrings[guid] = value; }
    public void SetVariableVector2(string guid, Vector2 value) { if (_runtimeVector2s != null) _runtimeVector2s[guid] = value; }
    public void SetVariableVector3(string guid, Vector3 value) { if (_runtimeVector3s != null) _runtimeVector3s[guid] = value; }
    public void SetVariableGameObject(string guid, GameObject value) { if (_runtimeGameObjects != null) _runtimeGameObjects[guid] = value; }
    public void SetVariableTransform(string guid, Transform value) { if (_runtimeTransforms != null) _runtimeTransforms[guid] = value; }
    public void SetVariableColor(string guid, Color value) { if (_runtimeColors != null) _runtimeColors[guid] = value; }
    
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
