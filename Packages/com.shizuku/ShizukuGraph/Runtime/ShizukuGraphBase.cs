using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [CreateAssetMenu(fileName = "ShizukuGraph", menuName = "Shizuku/Graph", order = 1)]
    public partial class ShizukuGraphBase : ScriptableObject, INodeContext
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

        /// <summary>
        /// INodeContext 实现：图自身就是根图
        /// </summary>
        public ShizukuGraphBase RootGraph => this;

        [NonSerialized]
        private GameObject _runtimeOwner;
        public GameObject RuntimeOwner => _runtimeOwner;

        [NonSerialized]
        private bool _runtimeInitialized;

        [NonSerialized]
        private GameObject _pendingRuntimeOwner;

        [NonSerialized]
        private bool _hasPendingRuntimeOwner;

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

        /// <summary>
        /// 使用指定宿主初始化运行时图。保留无参虚方法作为兼容入口，
        /// 这样现有派生图对 Init() 的覆写仍会正常执行。
        /// </summary>
        public void Init(GameObject runtimeOwner)
        {
            _pendingRuntimeOwner = runtimeOwner;
            _hasPendingRuntimeOwner = true;
            Init();
        }

        public virtual void Init()
        {
            var runtimeOwner = _hasPendingRuntimeOwner ? _pendingRuntimeOwner : null;
            _pendingRuntimeOwner = null;
            _hasPendingRuntimeOwner = false;

            if (_runtimeInitialized)
                DisposeRuntime();

            _runtimeOwner = runtimeOwner;
            _runtimeInitialized = true;

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

            // 初始化函数子图
            foreach (var method in _methods)
            {
                method.Init(this);
            }

            // 初始化变量
            InitVariables();
        }

        /// <summary>
        /// 释放当前运行时图实例及函数子图中所有节点持有的持续性状态。
        /// 此方法可重复调用，运行入口必须在销毁克隆资产前调用。
        /// </summary>
        public virtual void DisposeRuntime()
        {
            if (!_runtimeInitialized)
                return;

            CancelActiveLatentExecutions();

            foreach (var method in _methods)
            {
                if (method == null)
                    continue;

                try
                {
                    method.DisposeRuntime();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            foreach (var node in _nodes)
            {
                if (node == null)
                    continue;

                try
                {
                    node.DisposeRuntime();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            _guid2NodeMap.Clear();
            _guid2EdgeMap.Clear();
            _variableStore = null;
            _runtimeOwner = null;
            _rootExecutionDepth = 0;
            _latentRestrictionDepth = 0;
            _latentRestrictionReason = null;
            _runtimeInitialized = false;
        }

        protected virtual void OnDestroy()
        {
            DisposeRuntime();
        }

        public void Update()
        {
    #if UNITY_EDITOR
            if (ShizukuDebugger.Enabled)
            {
                DebugUpdate();
                return;
            }
    #endif

            var rootWasBlocked = TickLatentExecutions();
            if (string.IsNullOrEmpty(RootNodeGUID) ||
                rootWasBlocked ||
                HasBlockingRootExecution())
            {
                return;
            }

            // ---- 正常模式：同步链一帧跑完；Latent 链在完成前阻止 Root 重入 ----
            if (_guid2NodeMap.TryGetValue(RootNodeGUID, out var rootNode) && rootNode is ShizukuRootNode root)
            {
                ExecuteRoot(root);
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
        public bool TryGetVariableInt(string guid, out int value) => TryGetVariable(_variableStore?.Ints, guid, out value);
        public bool TryGetVariableFloat(string guid, out float value) => TryGetVariable(_variableStore?.Floats, guid, out value);
        public bool TryGetVariableBool(string guid, out bool value) => TryGetVariable(_variableStore?.Bools, guid, out value);
        public bool TryGetVariableString(string guid, out string value) => TryGetVariable(_variableStore?.Strings, guid, out value);
        public bool TryGetVariableVector2(string guid, out Vector2 value) => TryGetVariable(_variableStore?.Vector2s, guid, out value);
        public bool TryGetVariableVector3(string guid, out Vector3 value) => TryGetVariable(_variableStore?.Vector3s, guid, out value);
        public bool TryGetVariableGameObject(string guid, out GameObject value) => TryGetVariable(_variableStore?.GameObjects, guid, out value);
        public bool TryGetVariableTransform(string guid, out Transform value) => TryGetVariable(_variableStore?.Transforms, guid, out value);
        public bool TryGetVariableColor(string guid, out Color value) => TryGetVariable(_variableStore?.Colors, guid, out value);

        public void SetVariableInt(string guid, int value) => SetExistingVariable(_variableStore?.Ints, guid, value, VariableType.Int);
        public void SetVariableFloat(string guid, float value) => SetExistingVariable(_variableStore?.Floats, guid, value, VariableType.Float);
        public void SetVariableBool(string guid, bool value) => SetExistingVariable(_variableStore?.Bools, guid, value, VariableType.Bool);
        public void SetVariableString(string guid, string value) => SetExistingVariable(_variableStore?.Strings, guid, value, VariableType.String);
        public void SetVariableVector2(string guid, Vector2 value) => SetExistingVariable(_variableStore?.Vector2s, guid, value, VariableType.Vector2);
        public void SetVariableVector3(string guid, Vector3 value) => SetExistingVariable(_variableStore?.Vector3s, guid, value, VariableType.Vector3);
        public void SetVariableGameObject(string guid, GameObject value) => SetExistingVariable(_variableStore?.GameObjects, guid, value, VariableType.GameObject);
        public void SetVariableTransform(string guid, Transform value) => SetExistingVariable(_variableStore?.Transforms, guid, value, VariableType.Transform);
        public void SetVariableColor(string guid, Color value) => SetExistingVariable(_variableStore?.Colors, guid, value, VariableType.Color);

        private static bool TryGetVariable<T>(Dictionary<string, T> values, string guid, out T value)
        {
            if (values != null && !string.IsNullOrEmpty(guid))
                return values.TryGetValue(guid, out value);

            value = default;
            return false;
        }

        private static void SetExistingVariable<T>(Dictionary<string, T> values, string guid, T value, VariableType type)
        {
            if (values != null && !string.IsNullOrEmpty(guid) && values.ContainsKey(guid))
            {
                values[guid] = value;
                return;
            }

            Debug.LogError($"[ShizukuGraph] 无法设置 {type} 变量：GUID '{guid ?? "<null>"}' 不存在、类型不匹配或图尚未初始化。");
        }

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
            return _variables.Find(v => v != null && v.GUID == guid);
        }

        public GraphVariable GetVariableByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var normalizedName = name.Trim();
            return _variables.Find(v =>
                v != null && string.Equals(v.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        }

        public bool AddVariable(GraphVariable variable)
        {
            if (variable == null ||
                string.IsNullOrWhiteSpace(variable.Name) ||
                string.IsNullOrEmpty(variable.GUID) ||
                GetVariableByGUID(variable.GUID) != null ||
                !IsVariableNameAvailable(variable.Name))
            {
                return false;
            }

            variable.Name = variable.Name.Trim();
            _variables.Add(variable);
            return true;
        }

        public int CountVariableReferences(string guid)
        {
            var count = GraphVariableReferenceUtility.CountReferences(_nodes, guid);
            foreach (var method in _methods)
            {
                if (method != null)
                    count += method.CountVariableReferences(guid);
            }

            return count;
        }

        public int RemoveVariable(string guid)
        {
            var removedReferenceCount = GraphVariableReferenceUtility.RemoveReferences(
                _nodes,
                _edges,
                _guid2NodeMap,
                _guid2EdgeMap,
                guid);

            foreach (var method in _methods)
            {
                if (method != null)
                    removedReferenceCount += method.RemoveVariableReferences(guid);
            }

            _variables.RemoveAll(v => v != null && v.GUID == guid);
            _variableStore?.Remove(guid);
            return removedReferenceCount;
        }

        public bool IsVariableNameAvailable(string name, string ignoredGuid = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var normalizedName = name.Trim();
            return !_variables.Exists(v =>
                v != null &&
                v.GUID != ignoredGuid &&
                string.Equals(v.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        }

        public bool RenameVariable(string guid, string newName)
        {
            var variable = GetVariableByGUID(guid);
            if (variable == null || !IsVariableNameAvailable(newName, guid))
                return false;

            variable.Name = newName.Trim();
            return true;
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
