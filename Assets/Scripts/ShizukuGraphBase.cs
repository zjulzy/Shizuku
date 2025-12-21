using System;
using System.Collections.Generic;
using Unity.Mathematics;
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
    
    [NonSerialized]
    private Dictionary<string , ShizukuNodeBase> _guid2NodeMap = new Dictionary<string, ShizukuNodeBase>();
    public Dictionary<string , ShizukuNodeBase> Guid2NodeMap => _guid2NodeMap;
    
    [NonSerialized]
    private Dictionary<string , ParameterEdge> _guid2EdgeMap = new Dictionary<string, ParameterEdge>();
    public Dictionary<string , ParameterEdge> Guid2EdgeMap => _guid2EdgeMap;
    
    
    
    public void AddNode(ShizukuNodeBase node)
    {
        _nodes.Add(node);
    }
    
    public void AddParameterEdge(ShizukuNodeBase sourceNode, string outputPortName, ShizukuNodeBase targetNode, string inputPortName)
    {
        ParameterEdge edge = new ParameterEdge()
        {
            OutputNodeGuid = sourceNode.GUID,
            OutputPortName = outputPortName,
            InputNodeGuid = targetNode.GUID,
            InputPortName = inputPortName
        };
        _edges.Add(edge);
    }

    public void Init()
    {
        _guid2NodeMap.Clear();
        foreach (var node in _nodes)
        {
            _guid2NodeMap[node.GUID] = node;
        }
        
        _guid2EdgeMap.Clear();
        foreach (var edge in _edges)
        {
            _guid2EdgeMap[edge.GUID] = edge;
        }
    }
}

[Serializable]
public class ShizukuNodeBase
{
    [SerializeField]
    public string GUID;
    
    [SerializeField]
    public float4 PositionAndSize;

    [SerializeReference]
    public ParameterEdgePort ParameterA = new ();
    [SerializeReference]
    public ParameterEdgePort ParameterB = new();
    [SerializeReference]
    public ParameterEdgePort ParameterResult = new();
}

public class ParameterEdgePort{
    public string Name;
    public bool IsOut;
}

// 输入输出值的接口
public class ParameterEdgePort<T>: ParameterEdgePort
{
    public T Parameter;
    
    
}

public class IntParameterEdgePort : ParameterEdgePort<int>
{
}

// 指定执行顺序的接口
public class ChainPort
{
}

public class Chain
{
}

public class ParameterEdge
{
    [SerializeField]
    public string GUID;
    [SerializeField]
    public string OutputNodeGuid;
    
    [SerializeField]
    public string OutputPortName;
    
    [SerializeField]
    public string InputNodeGuid;
    
    [SerializeField]
    public string InputPortName;
}
