using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shizuku.Graph
{
    /// <summary>
    /// 函数参数定义
    /// </summary>
    [Serializable]
    public class MethodParameter
    {
        [SerializeField] public string Name;
        [SerializeField] public VariableType Type;
        [SerializeField] public string CustomTypeName;

        public MethodParameter()
        {
            Name = "param";
            Type = VariableType.Float;
        }

        public MethodParameter(string name, VariableType type, string customTypeName = null)
        {
            Name = name;
            Type = type;
            CustomTypeName = customTypeName;
        }
    }

    /// <summary>
    /// 图中的函数定义（自包含子图）
    /// 每个函数拥有独立的节点列表和边列表，
    /// 包含一个入口节点（MethodEntryNode）和可选的返回节点（MethodReturnNode），
    /// 构成一段可被复用调用的执行子链。
    /// </summary>
    [Serializable]
    public class ShizukuMethod
    {
        [SerializeField] public string GUID;
        [SerializeField] public string Name;

        /// <summary>
        /// 函数入口节点的 GUID
        /// </summary>
        [SerializeField] public string EntryNodeGUID;

        /// <summary>
        /// 函数返回节点的 GUID（可选，无返回值的函数可以没有）
        /// </summary>
        [SerializeField] public string ReturnNodeGUID;

        // ===== 子图数据（函数私有） =====

        /// <summary>
        /// 函数内部的节点列表（Entry、Return、以及所有中间逻辑节点）
        /// </summary>
        [SerializeReference]
        private List<ShizukuNodeBase> _nodes = new List<ShizukuNodeBase>();
        public List<ShizukuNodeBase> Nodes => _nodes;

        /// <summary>
        /// 函数内部的边列表
        /// </summary>
        [SerializeReference]
        private List<ParameterEdge> _edges = new List<ParameterEdge>();
        public List<ParameterEdge> Edges => _edges;

        /// <summary>
        /// 函数内部的分组数据（编辑器用）
        /// </summary>
        [SerializeField]
        private List<GroupData> _groups = new List<GroupData>();
        public List<GroupData> Groups => _groups;

        // ===== 参数定义 =====

        /// <summary>
        /// 输入参数列表
        /// </summary>
        [SerializeField] public List<MethodParameter> InputParameters = new List<MethodParameter>();

        /// <summary>
        /// 输出参数列表（返回值可以有多个）
        /// </summary>
        [SerializeField] public List<MethodParameter> OutputParameters = new List<MethodParameter>();

        // ===== 运行时缓存（不序列化） =====

        [NonSerialized]
        private Dictionary<string, ShizukuNodeBase> _guid2NodeMap = new Dictionary<string, ShizukuNodeBase>();
        public Dictionary<string, ShizukuNodeBase> Guid2NodeMap => _guid2NodeMap;

        [NonSerialized]
        private Dictionary<string, ParameterEdge> _guid2EdgeMap = new Dictionary<string, ParameterEdge>();
        public Dictionary<string, ParameterEdge> Guid2EdgeMap => _guid2EdgeMap;

        #region 构造

        public ShizukuMethod()
        {
            GUID = Guid.NewGuid().ToString();
            Name = "NewMethod";
        }

        public ShizukuMethod(string name)
        {
            GUID = Guid.NewGuid().ToString();
            Name = name;
        }

        #endregion

        #region 运行时初始化

        /// <summary>
        /// 初始化函数子图：构建内部映射表，初始化节点和边。
        /// 同时将内部节点/边注册到父图的全局映射中，保证运行时查找统一。
        /// </summary>
        public void Init(ShizukuGraphBase parentGraph)
        {
            // 清理反序列化失败的 null 节点/边
            _nodes.RemoveAll(n => n == null);
            _edges.RemoveAll(e => e == null);

            _guid2NodeMap.Clear();
            foreach (var node in _nodes)
            {
                _guid2NodeMap[node.GUID] = node;
                node.Init(parentGraph);

                // 同时注册到父图的全局映射，运行时 Execute 链可以统一查找
                parentGraph.Guid2NodeMap[node.GUID] = node;
            }

            _guid2EdgeMap.Clear();
            foreach (var edge in _edges)
            {
                _guid2EdgeMap[edge.GUID] = edge;
                // 边的连接在函数内部节点之间，使用函数自己的节点列表来连接
                ConnectEdge(edge);

                parentGraph.Guid2EdgeMap[edge.GUID] = edge;
            }
        }

        /// <summary>
        /// 在函数内部的节点范围内连接边的端口
        /// </summary>
        private void ConnectEdge(ParameterEdge edge)
        {
            _guid2NodeMap.TryGetValue(edge.OutputNodeGuid, out var outputNode);
            _guid2NodeMap.TryGetValue(edge.InputNodeGuid, out var inputNode);

            if (outputNode == null || inputNode == null)
            {
                Debug.LogError($"[ShizukuMethod:{Name}] Edge connects missing node: {edge.OutputNodeGuid} -> {edge.InputNodeGuid}");
                return;
            }

            inputNode.DependentNodes.Add(outputNode);

            var outputPort = outputNode.SelfOutputPorts.Find(p => p.Name == edge.OutputPortName);
            var inputPort = inputNode.SelfInputPorts.Find(p => p.Name == edge.InputPortName);

            if (outputPort != null && inputPort != null)
            {
                if (outputPort.GetType() == inputPort.GetType())
                {
                    inputPort.SameTypeConnectedPort = outputPort;
                }
                else
                {
                    Debug.LogError(
                        $"[ShizukuMethod:{Name}] Type mismatch: {outputNode.Title}.{edge.OutputPortName} ({outputPort.GetType()}) -> {inputNode.Title}.{edge.InputPortName} ({inputPort.GetType()})");
                }
            }
        }

        #endregion

        #region 节点 / 边管理

        public void AddNode(ShizukuNodeBase node)
        {
            _nodes.Add(node);
        }

        public void RemoveNode(string guid)
        {
            _nodes.RemoveAll(n => n.GUID == guid);
            // 同时清理相关的边
            _edges.RemoveAll(e => e.OutputNodeGuid == guid || e.InputNodeGuid == guid);
        }

        public ShizukuNodeBase GetNodeByGUID(string guid)
        {
            return _nodes.Find(n => n.GUID == guid);
        }

        public void AddEdge(ParameterEdge edge)
        {
            _edges.Add(edge);
        }

        public void AddParameterEdge(ShizukuNodeBase sourceNode, string outputPortName,
            ShizukuNodeBase targetNode, string inputPortName)
        {
            var edge = new ParameterEdge(
                sourceNode.GUID,
                outputPortName,
                targetNode.GUID,
                inputPortName
            );
            _edges.Add(edge);
        }

        public void RemoveEdge(string guid)
        {
            _edges.RemoveAll(e => e.GUID == guid);
        }

        #endregion
    }
}

