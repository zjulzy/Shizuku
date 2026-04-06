using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [CreateAssetMenu(fileName = "ShizukuGraph", menuName = "Shizuku/Graph", order = 1)]
    public partial class ShizukuGraphBase : ScriptableObject
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

        // 函数系统
        [SerializeField]
        private List<ShizukuMethod> _methods = new List<ShizukuMethod>();
        public List<ShizukuMethod> Methods => _methods;

        [NonSerialized]
        private Dictionary<string , ShizukuNodeBase> _guid2NodeMap = new Dictionary<string, ShizukuNodeBase>();
        public Dictionary<string , ShizukuNodeBase> Guid2NodeMap => _guid2NodeMap;

        [NonSerialized]
        private Dictionary<string , ParameterEdge> _guid2EdgeMap = new Dictionary<string, ParameterEdge>();
        public Dictionary<string , ParameterEdge> Guid2EdgeMap => _guid2EdgeMap;

        // 运行时变量存储
        [NonSerialized] private RuntimeVariableStore _variableStore;
        public RuntimeVariableStore VariableStore => _variableStore;

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
            // 清理反序列化失败的 null 节点/边（[SerializeReference] 类型变更后会出现）
            int removedNodes = _nodes.RemoveAll(n => n == null);
            int removedEdges = _edges.RemoveAll(e => e == null);
            if (removedNodes > 0 || removedEdges > 0)
            {
                Debug.LogWarning($"[ShizukuGraph] 检测到 {removedNodes} 个无效节点和 {removedEdges} 条无效边已被清理（可能是类型变更导致反序列化失败）");
            }

            // 初始化主图节点
            _guid2NodeMap.Clear();
            foreach (var node in _nodes)
            {
                _guid2NodeMap[node.GUID] = node;
                node.Init(this);
            }

            // 初始化主图边
            _guid2EdgeMap.Clear();
            foreach (var edge in _edges)
            {
                _guid2EdgeMap[edge.GUID] = edge;
                edge.ConnectPorts(this);
            }

            // 初始化函数子图（内部节点/边会被注册到全局映射）
            foreach (var method in _methods)
            {
                method.Init(this);
            }

            // 初始化变量
            InitVariables();
        }

        public void Update()
        {
            if (string.IsNullOrEmpty(RootNodeGUID))
                return;

    #if UNITY_EDITOR
            if (ShizukuDebugger.Enabled)
            {
                DebugUpdate();
                return;
            }
    #endif

            // ---- 正常模式：递归一帧跑完 ----
            if (_guid2NodeMap.TryGetValue(RootNodeGUID, out var rootNode) && rootNode is ShizukuRootNode root)
            {
                root.StartExcute();
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

        // 自定义类型通用访问（零装箱，泛型走类型化字典）
        public bool TryGetCustomVariable<T>(string guid, out T value)
        {
            var dict = _variableStore.GetOrCreateCustomDict<T>();
            return dict.TryGetValue(guid, out value);
        }

        public void SetCustomVariable<T>(string guid, T value)
        {
            var dict = _variableStore.GetOrCreateCustomDict<T>();
            dict[guid] = value;
        }

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

        #region 函数管理
        

        public ShizukuMethod GetMethodByGUID(string guid)
        {
            return _methods.Find(m => m.GUID == guid);
        }

        public ShizukuMethod GetMethodByName(string name)
        {
            return _methods.Find(m => m.Name == name);
        }

        public void AddMethod(ShizukuMethod method)
        {
            _methods.Add(method);
        }

        public void RemoveMethod(string guid)
        {
            _methods.RemoveAll(m => m.GUID == guid);
        }

        public bool RenameMethod(string guid, string newName)
        {
            var method = GetMethodByGUID(guid);
            if (method != null)
            {
                method.Name = newName;
                return true;
            }
            return false;
        }

        #endregion
    }

}
