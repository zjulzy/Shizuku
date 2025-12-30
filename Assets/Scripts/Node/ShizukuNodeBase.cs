using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditorInternal;
using UnityEngine;


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