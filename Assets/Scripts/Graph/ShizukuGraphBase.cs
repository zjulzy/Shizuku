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
        _guid2NodeMap.Clear();
        foreach (var node in _nodes)
        {
            _guid2NodeMap[node.GUID] = node;
            node.Init(this);
        }
        
        _guid2EdgeMap.Clear();
        foreach (var edge in _edges)
        {
            _guid2EdgeMap[edge.GUID] = edge;
            edge.ConnectPorts(this);
        }
    }
    
    public void Update()
    {
        if (!string.IsNullOrEmpty(RootNodeGUID))
        {
            if (_guid2NodeMap.TryGetValue(RootNodeGUID, out var rootNode))
            {
                rootNode.Execute();
            }
        }
    }
}
