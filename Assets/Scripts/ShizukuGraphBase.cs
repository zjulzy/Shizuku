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

    public void Init()
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

[Serializable]
public class ShizukuNodeBase
{
    [SerializeField]
    public string GUID;
    
    [SerializeField]
    public float4 PositionAndSize;

    [SerializeReference]
    public IntParameterEdgePort Parameter = new (){IsOut = false, Name = "parameter"};
    
    [SerializeReference]
    public IntParameterEdgePort ParameterResult = new(){IsOut = true, Name = "result"};
    
    [SerializeField]
    public string NextNodeGuid;
    
    [NonSerialized]
    private ShizukuGraphBase _parentGraph;
    
    [NonSerialized]
    public readonly List<ShizukuNodeBase> DependentNodes = new List<ShizukuNodeBase>();
    
    [NonSerialized]
    public readonly List<ParameterEdgePort> SelfInputPorts = new List<ParameterEdgePort>();
    
    [NonSerialized]
    public readonly List<ParameterEdgePort> SelfOutputPorts = new List<ParameterEdgePort>();
    
    private int _executedFrame = -1;

    public void Init(ShizukuGraphBase parentGraph)
    {
        _parentGraph = parentGraph;
        // 通过反射获取自身的输入输出端口
        SelfInputPorts.Clear();
        SelfOutputPorts.Clear();
        var fields = this.GetType().GetFields();
        foreach (var field in fields)
        {
            if (typeof(ParameterEdgePort).IsAssignableFrom(field.FieldType))
            {
                var port = field.GetValue(this) as ParameterEdgePort;
                if (port != null)
                {
                    if (!port.IsOut)
                    {
                        SelfInputPorts.Add(port);
                    }
                    else
                    {
                        SelfOutputPorts.Add(port);
                    }
                }
            }

        }
       
    }

    public void Execute()
    {
        GetInputValues();
        ParameterResult.Value = Parameter.Value + 1; // 示例逻辑：参数加1
        Debug.Log($"帧号:{Time.frameCount} 执行节点 {GUID}  参数:{Parameter.Value}");
        _executedFrame = Time.frameCount;
        // 执行下一个节点
        if (!string.IsNullOrEmpty(NextNodeGuid))
        {
            if (_parentGraph.Guid2NodeMap.TryGetValue(NextNodeGuid, out var nextNode))
            {
                nextNode.Execute();
            }
        }
    }

    private void GetInputValues()
    {
        foreach (var node in DependentNodes)
        {
            node.GetOutputValues();
        }
        SelfInputPorts.ForEach(port =>
        {
            port.GetSourceValue();
        });
        
    }
    
    public void GetOutputValues()
    {
        
    }
}

[Serializable]
public class ParameterEdgePort{
    [SerializeField]
    public string Name;
    [SerializeField]
    public string InputEdgeGUID;

    [SerializeField] 
    public bool IsOut;
    
    [NonSerialized]
    public ParameterEdgePort SameTypeConnectedPort;
    
    [NonSerialized]
    public ParameterEdgePort DifferentTypeConnectedPort;
    
    public virtual void GetSourceValue()
    {
        
    }
    
}

// 输入输出值的接口
[Serializable]
public class ParameterEdgePort<T>: ParameterEdgePort
{
    public T Value = default;
    
    public Type GetValueType => typeof(T);
    
    public override void GetSourceValue()
    {
        if (SameTypeConnectedPort != null)
        {
            var port = SameTypeConnectedPort as ParameterEdgePort<T>;
            Value = port.Value;
        }
        else if (DifferentTypeConnectedPort != null)
        {
            // 类型转换逻辑
            var differentValue = DifferentTypeConnectedPort as ParameterEdgePort<object>;
            if (differentValue != null)
            {
                Value = (T)Convert.ChangeType(differentValue.Value, typeof(T));
            }
        }
    }
}

[Serializable]
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

// edge只有记录功能，没有实际的逻辑，用于只是用于记录port之间的连接关系
[Serializable]
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

    private ParameterEdge()
    {
    }
    
    public ParameterEdge(string outputNodeGuid, string outputPortName, string inputNodeGuid, string inputPortName)
    {
        GUID = System.Guid.NewGuid().ToString();
        OutputNodeGuid = outputNodeGuid;
        OutputPortName = outputPortName;
        InputNodeGuid = inputNodeGuid;
        InputPortName = inputPortName;
    }

    // 依据记录的信息连接参数端口
    public void ConnectPorts(ShizukuGraphBase graph)
    {
        var OutputNode = graph.Nodes.Find(n => n.GUID == OutputNodeGuid);
        var InputNode = graph.Nodes.Find(n => n.GUID == InputNodeGuid);
        if (OutputNode != null && InputNode != null)
        {
            InputNode.DependentNodes.Add(OutputNode);
            var outputPort = OutputNode.SelfOutputPorts.Find(p => p.Name == OutputPortName);
            var inputPort = InputNode.SelfInputPorts.Find(p => p.Name == InputPortName);
            if (outputPort != null && inputPort != null)
            {
                if (outputPort.GetType() == inputPort.GetType())
                {
                    inputPort.SameTypeConnectedPort = outputPort;
                    inputPort.DifferentTypeConnectedPort = null;
                }
                else
                {
                    inputPort.DifferentTypeConnectedPort = outputPort;
                    inputPort.SameTypeConnectedPort = null;
                }
            }
        }
    }
}
