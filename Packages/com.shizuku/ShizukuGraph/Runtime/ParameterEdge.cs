using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace Shizuku.Graph
{
    using Shizuku.Core;
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
        // public void ConnectPorts(ShizukuGraphBase graph) => ConnectPorts((INodeContext)graph);

        // 依据记录的信息连接参数端口（O(1) 字典查找版本）
        public void ConnectPorts(INodeContext context)
        {
            if (!context.Guid2NodeMap.TryGetValue(OutputNodeGuid, out var outputNode)) return;
            if (!context.Guid2NodeMap.TryGetValue(InputNodeGuid, out var inputNode)) return;

            inputNode.DependentNodes.Add(outputNode);
            var outputPort = outputNode.SelfOutputPorts.Find(p => p.Name == OutputPortName);
            var inputPort = inputNode.SelfInputPorts.Find(p => p.Name == InputPortName);
            if (outputPort != null && inputPort != null)
            {
                if (outputPort.GetType() == inputPort.GetType())
                {
                    inputPort.SameTypeConnectedPort = outputPort;
                }
                else
                {
                    Debug.LogError($"Type mismatch when connecting ports: {outputNode.Title}.{OutputPortName} ({outputPort.GetType()}) -> {inputNode.Title}.{InputPortName} ({inputPort.GetType()})");
                }
            }
        }
    }
}
