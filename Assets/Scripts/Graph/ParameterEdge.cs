using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


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