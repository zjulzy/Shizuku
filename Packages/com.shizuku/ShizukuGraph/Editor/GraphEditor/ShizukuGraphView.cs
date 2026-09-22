using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shizuku.Graph.Editor
{
    using Shizuku.Graph;
    using Shizuku.Core;
    public partial class ShizukuGraphView : GraphView
    {
        private Vector2 _localMousePosition;
        private ShizukuGraphBase _runtimeGraph;
        internal ShizukuGraphBase RuntimeGraph => _runtimeGraph;

        /// <summary>
        /// 当前正在编辑的函数（null 表示编辑主图）
        /// </summary>
        private ShizukuMethod _currentMethod;
        public ShizukuMethod CurrentMethod => _currentMethod;

        /// <summary>
        /// 是否正在编辑函数子图
        /// </summary>
        public bool IsEditingMethod => _currentMethod != null;

        // ---- 上下文感知的数据访问 ----
        private List<ShizukuNodeBase> CurrentNodes => IsEditingMethod ? _currentMethod.Nodes : _runtimeGraph.Nodes;
        private List<ParameterEdge> CurrentEdges => IsEditingMethod ? _currentMethod.Edges : _runtimeGraph.Edges;
        private List<GroupData> CurrentGroups => IsEditingMethod ? _currentMethod.Groups : _runtimeGraph.Groups;
        private INodeContext CurrentContext => IsEditingMethod ? (INodeContext)_currentMethod : _runtimeGraph;
        public INodeContext CurrentNodeContext => CurrentContext;

        private Dictionary<string, ShizukuNodeView> _guidToNodeViewMap = new Dictionary<string, ShizukuNodeView>();
        private bool _isRebuildingView;
        private int _frameAllAttemptsRemaining;
        private Vector2 _lastFrameLayoutSize;
        private int _stableFrameLayoutTicks;

        public System.Action OnGraphChanged;
        public System.Action<ShizukuNodeBase> OnNodeSelected;

        /// <summary>
        /// 当编辑上下文切换时触发（进入/退出函数编辑）
        /// </summary>
        public System.Action<ShizukuMethod> OnEditingContextChanged;

        #region 生命周期

        public ShizukuGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 创建网格背景
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // 注册鼠标事件以捕获正确的位置
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<DetachFromPanelEvent>(_ => CancelPendingFrameAll());

            // 注册 graphViewChanged 委托来检测环
            graphViewChanged += OnGraphViewChanged;

            // 设置删除回调，支持删除节点，边和分组
            deleteSelection = (operationName, askUser) =>
            {
                DeleteElements(selection.OfType<GraphElement>().ToList());
            };

            // GraphView 只提供复制粘贴的命令管线，自定义节点的数据复制由这里接入。
            ConfigureClipboard();
            SetupAuthoring();

            // 普通节点创建使用 Unity 原生分组搜索树。
            nodeCreationRequest = context =>
            {
                SearchWindow.Open(
                    new SearchWindowContext(context.screenMousePosition),
                    CreateNodeSearchWindowProvider(context.screenMousePosition));
            };

            // 监听选择变化事件
            RegisterCallback<MouseUpEvent>(evt =>
            {
                // 延迟一帧，确保选择已更新
                schedule.Execute(() =>
                {
                    var selectedNode = selection.OfType<ShizukuNodeView>().FirstOrDefault();
                    OnNodeSelected?.Invoke(selectedNode?.RuntimeNode);
                }).ExecuteLater(0);
            });

            styleSheets.Add(Resources.Load<StyleSheet>("ShizukuGraphView"));

        }

        /// <summary>
        /// 创建节点搜索窗口提供者
        /// </summary>
        private NodeSearchWindowProvider CreateNodeSearchWindowProvider(Vector2 screenMousePosition)
        {
            var windowRoot = EditorWindow.focusedWindow.rootVisualElement;
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(
                windowRoot.parent,
                screenMousePosition - EditorWindow.focusedWindow.position.position
            );
            var graphMousePosition = contentViewContainer.WorldToLocal(windowMousePosition);

            return CreateNodeSearchWindowProviderAt(graphMousePosition);
        }

        private NodeSearchWindowProvider CreateNodeSearchWindowProviderAt(Vector2 graphPosition)
        {
            var provider = ScriptableObject.CreateInstance<NodeSearchWindowProvider>();
            provider.Initialize(this, graphPosition, _runtimeGraph);
            return provider;
        }

        public void OpenNodeSearchAtViewCenter(Vector2 screenPosition)
        {
            if (_runtimeGraph == null)
                return;

            var graphPosition = contentViewContainer.WorldToLocal(worldBound.center);
            SearchWindow.Open(
                new SearchWindowContext(screenPosition),
                CreateNodeSearchWindowProviderAt(graphPosition));
        }

        public void FrameAllNodes()
        {
            var nodeViews = nodes.OfType<ShizukuNodeView>().ToList();
            if (nodeViews.Count == 0)
                return;

            var viewportSize = layout.size;
            if (!IsFinitePositive(viewportSize.x) || !IsFinitePositive(viewportSize.y))
            {
                FrameAll();
                return;
            }

            var hasBounds = false;
            var contentBounds = new Rect();
            foreach (var nodeView in nodeViews)
            {
                var nodeRect = nodeView.GetPosition();
                if (!IsFinitePositive(nodeRect.width) || !IsFinitePositive(nodeRect.height))
                {
                    var serializedRect = nodeView.RuntimeNode.PositionAndSize;
                    nodeRect = new Rect(serializedRect.x, serializedRect.y,
                        Mathf.Max(serializedRect.z, 190f), Mathf.Max(serializedRect.w, 100f));
                }

                contentBounds = hasBounds ? Union(contentBounds, nodeRect) : nodeRect;
                hasBounds = true;
            }

            if (!hasBounds)
                return;

            const float padding = 80f;
            var availableWidth = Mathf.Max(1f, viewportSize.x - padding * 2f);
            var availableHeight = Mathf.Max(1f, viewportSize.y - padding * 2f);
            var scale = Mathf.Min(
                availableWidth / Mathf.Max(1f, contentBounds.width),
                availableHeight / Mathf.Max(1f, contentBounds.height));
            scale = Mathf.Clamp(scale, ContentZoomer.DefaultMinScale, 1f);

            var viewportCenter = viewportSize * 0.5f;
            var translation = viewportCenter - contentBounds.center * scale;
            UpdateViewTransform(
                new Vector3(translation.x, translation.y, 0f),
                new Vector3(scale, scale, 1f));
        }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }

        private static Rect Union(Rect first, Rect second)
        {
            var xMin = Mathf.Min(first.xMin, second.xMin);
            var yMin = Mathf.Min(first.yMin, second.yMin);
            var xMax = Mathf.Max(first.xMax, second.xMax);
            var yMax = Mathf.Max(first.yMax, second.yMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        public void FrameAllWhenReady()
        {
            // UI Toolkit 的元素调度器在窗口尚未显示时不会运行。使用 Editor 更新回调
            // 等待窗口扩展（例如 Blueprint 左侧属性面板）完成布局，再计算可视区域。
            _frameAllAttemptsRemaining = 120;
            _lastFrameLayoutSize = new Vector2(float.NaN, float.NaN);
            _stableFrameLayoutTicks = 0;
            EditorApplication.update -= TryFrameAllWhenReady;
            EditorApplication.update += TryFrameAllWhenReady;
        }

        private void TryFrameAllWhenReady()
        {
            _frameAllAttemptsRemaining--;
            var currentSize = layout.size;
            var hasValidLayout = panel != null
                                 && IsFinitePositive(currentSize.x)
                                 && IsFinitePositive(currentSize.y)
                                 && nodes.Any();

            if (hasValidLayout)
            {
                if (Approximately(currentSize, _lastFrameLayoutSize))
                    _stableFrameLayoutTicks++;
                else
                {
                    _lastFrameLayoutSize = currentSize;
                    _stableFrameLayoutTicks = 0;
                }

                // 连续三帧尺寸稳定，确保 Blueprint/自定义扩展的侧栏已经完成布局。
                if (_stableFrameLayoutTicks >= 2)
                {
                    FrameAllNodes();
                    CancelPendingFrameAll();
                    return;
                }
            }

            if (_frameAllAttemptsRemaining <= 0)
                CancelPendingFrameAll();
        }

        private void CancelPendingFrameAll()
        {
            _frameAllAttemptsRemaining = 0;
            EditorApplication.update -= TryFrameAllWhenReady;
        }

        private static bool Approximately(Vector2 first, Vector2 second)
        {
            return Mathf.Abs(first.x - second.x) < 0.1f
                   && Mathf.Abs(first.y - second.y) < 0.1f;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Home)
                return;

            FrameAllNodes();
            evt.StopImmediatePropagation();
        }

        #endregion

        #region 编辑器操作

        private void InitializeNodeForCurrentContext(ShizukuNodeBase node)
        {
            var context = CurrentContext;
            if (node == null || context == null)
                return;

            context.Guid2NodeMap[node.GUID] = node;
            node.Init(context);
        }

        private string CurrentMethodGuid => _currentMethod?.GUID ?? string.Empty;

        private void ExecuteGraphEdits(string undoName, IEnumerable<GraphEditOperation> operations)
        {
            UnityGraphEditExecutor.Execute(
                _runtimeGraph,
                CurrentMethodGuid,
                operations,
                undoName,
                new GraphEditExecutionOptions());
        }

        private ShizukuNodeView AddNodeView(ShizukuNodeBase node, Rect position, bool select = false)
        {
            var nodeView = new ShizukuNodeView(node, _runtimeGraph);
            nodeView.InitPort();
            nodeView.SetPosition(position);
            _guidToNodeViewMap[node.GUID] = nodeView;
            AddElement(nodeView);
            if (select)
                AddToSelection(nodeView);
            return nodeView;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // GraphView 默认的“创建节点”入口统一打开 NodeSearchWindowProvider。
            base.BuildContextualMenu(evt);

            // 根节点和蓝图事件是结构节点，只能通过受控入口创建。
            evt.menu.AppendSeparator();
            if (!IsEditingMethod && _runtimeGraph != null && string.IsNullOrEmpty(_runtimeGraph.RootNodeGUID))
                evt.menu.AppendAction("创建根节点", _ => CreateNode<ShizukuRootNode>(_localMousePosition));
            BuildBlueprintEventMenu(evt);

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("创建分组", _ => CreateGroup(_localMousePosition));

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("检查当前图配置", _ => ValidateAuthoringGraph());
            evt.menu.AppendAction("清空所有节点", _ => ClearAllNodes());

            // 调试菜单
            BuildDebugContextualMenu(evt);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            // 将鼠标位置转换为内容容器的本地坐标并保存，目前主要给右键菜单定位用
            _localMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
        }

        /// <summary>
        /// 创建调用函数节点
        /// </summary>
        public void CreateInvokeMethodNode(ShizukuMethod method, Vector2 mousePosition)
        {
            var node = new InvokeMethodNode
            {
                TargetMethodGUID = method.GUID,
                TargetMethodName = method.Name,
            };
            node.SyncPortsFromMethod(method);

            var rect = new Rect(mousePosition, new Vector2(200, 100));
            ExecuteGraphEdits(
                "创建节点",
                new[] { new CreateNodeOperation(node, ToFloat4(rect)) });
            AddNodeView(node, rect);

            OnGraphChanged?.Invoke();
        }

        private void BuildBlueprintEventMenu(ContextualMenuPopulateEvent evt)
        {
            var behaviorType = GetBehaviorType();
            if (behaviorType == null)
            {
                evt.menu.AppendAction("蓝图事件/需要 ShizukuBluePrint 类型", null, DropdownMenuAction.Status.Disabled);
                return;
            }

            var methods = behaviorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<BlueprintOverridableAttribute>() != null)
                .ToArray();

            if (methods.Length == 0)
            {
                evt.menu.AppendAction("蓝图事件/无可覆写方法", null, DropdownMenuAction.Status.Disabled);
                return;
            }

            var existingEvents = GetExistingEventNames();

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<BlueprintOverridableAttribute>();
                var eventName = attr.EventName ?? method.Name;
                var parameters = method.GetParameters();
                var paramStr = parameters.Length > 0 
                    ? $"({string.Join(", ", parameters.Select(p => p.ParameterType.Name))})" 
                    : "()";
                var displayName = $"蓝图事件/{eventName}{paramStr}";

                if (existingEvents.Contains(eventName))
                {
                    evt.menu.AppendAction(displayName, null, DropdownMenuAction.Status.Disabled);
                }
                else
                {
                    evt.menu.AppendAction(displayName, (a) => CreateBlueprintEventNode(eventName, method, _localMousePosition));
                }
            }
        }

        private void CreateBlueprintEventNode(string eventName, MethodInfo method, Vector2 mousePosition)
        {
            var node = new BlueprintEventNode
            {
                EventName = eventName
            };

            foreach (var param in method.GetParameters())
            {
                var eventParam = new EventParameter
                {
                    Name = param.Name,
                    TypeName = param.ParameterType.Name,
                    OutputPort = CreatePortForType(param.Name, param.ParameterType)
                };
                node.EventParameters.Add(eventParam);
            }

            // 如果方法有返回值，自动创建 BlueprintReturnNode 并关联
            BlueprintReturnNode returnNode = null;
            if (method.ReturnType != typeof(void))
            {
                returnNode = new BlueprintReturnNode
                {
                    EventName = eventName,
                    ReturnPort = CreatePortForType("返回值", method.ReturnType, isOut: false)
                };
                node.ReturnNodeGUID = returnNode.GUID;
            }

            var eventRect = new Rect(mousePosition, new Vector2(200, 100));
            var operations = new List<GraphEditOperation>
            {
                new CreateNodeOperation(node, ToFloat4(eventRect))
            };
            Rect returnRect = default;
            if (returnNode != null)
            {
                returnRect = new Rect(mousePosition + new Vector2(400, 0), new Vector2(200, 100));
                operations.Add(new CreateNodeOperation(returnNode, ToFloat4(returnRect)));
            }

            ExecuteGraphEdits("创建蓝图事件", operations);
            AddNodeView(node, eventRect);

            // 添加返回节点（放在事件节点右侧）
            if (returnNode != null)
                AddNodeView(returnNode, returnRect);

            OnGraphChanged?.Invoke();
        }

        private ParameterEdgePort CreatePortForType(string name, Type type, bool isOut = true)
        {
            if (type == typeof(float))
                return new FloatParameterEdgePort { IsOut = isOut, Name = name };
            else if (type == typeof(int))
                return new IntParameterEdgePort { IsOut = isOut, Name = name };
            else if (type == typeof(bool))
                return new BoolParameterEdgePort { IsOut = isOut, Name = name };
            else if (type == typeof(string))
                return new StringParameterEdgePort { IsOut = isOut, Name = name };
            else if (type == typeof(Vector2))
                return new Vector2ParameterEdgePort { IsOut = isOut, Name = name };
            else if (type == typeof(Vector3))
                return new Vector3ParameterEdgePort { IsOut = isOut, Name = name };
            else if (type == typeof(GameObject))
                return new GameObjectParameterEdgePort { IsOut = isOut, Name = name };
            else if (type == typeof(Transform))
                return new TransformParameterEdgePort { IsOut = isOut, Name = name };
            else if (type == typeof(Color))
                return new ColorParameterEdgePort { IsOut = isOut, Name = name };
            else
                return new ObjectParameterEdgePort { IsOut = isOut, Name = name };
        }

        private Type GetBehaviorType()
        {
            if (_runtimeGraph == null) return null;

            var graphType = _runtimeGraph.GetType();
            while (graphType != null && graphType != typeof(object))
            {
                if (graphType.IsGenericType)
                {
                    var genericDef = graphType.GetGenericTypeDefinition();
                    if (genericDef.Name.StartsWith("ShizukuBluePrint"))
                    {
                        var genericArgs = graphType.GetGenericArguments();
                        if (genericArgs.Length > 0)
                        {
                            return genericArgs[0];
                        }
                    }
                }
                graphType = graphType.BaseType;
            }
            return null;
        }

        private HashSet<string> GetExistingEventNames()
        {
            var existingEvents = new HashSet<string>();
            if (_runtimeGraph != null)
            {
                foreach (var node in _runtimeGraph.Nodes)
                {
                    if (node is BlueprintEventNode eventNode)
                    {
                        existingEvents.Add(eventNode.EventName);
                    }
                }
            }
            return existingEvents;
        }

        /// <summary>
        /// 聚焦到指定事件名称的节点
        /// </summary>
        private void FocusOnEventNode(string eventName)
        {
            if (_runtimeGraph == null)
                return;

            // 查找事件节点
            BlueprintEventNode targetEventNode = null;
            foreach (var node in _runtimeGraph.Nodes)
            {
                if (node is BlueprintEventNode eventNode && eventNode.EventName == eventName)
                {
                    targetEventNode = eventNode;
                    break;
                }
            }

            if (targetEventNode == null)
            {
                Debug.LogWarning($"未找到事件节点: {eventName}");
                return;
            }

            // 查找对应的节点视图
            if (_guidToNodeViewMap.TryGetValue(targetEventNode.GUID, out var nodeView))
            {
                // 取消所有选择
                ClearSelection();

                // 选中目标节点
                AddToSelection(nodeView);

                // 使用 FrameSelection 方法来聚焦，这是 Unity GraphView 内置的方法
                FrameSelection();

                Debug.Log($"已聚焦到事件节点: {eventName}");
            }
            else
            {
                Debug.LogWarning($"未找到事件节点的视图: {eventName}");
            }
        }

        private void CreateNode<TNode>(Vector2 mousePosition) where TNode : ShizukuNodeBase, new()
        {
            CreateNodeFromType(typeof(TNode), mousePosition);
        }

        /// <summary>
        /// 通过 Type 创建节点（SearchWindow 和右键菜单统一入口）
        /// </summary>
        public void CreateNodeFromType(Type nodeType, Vector2 mousePosition)
        {
            if (!typeof(ShizukuNodeBase).IsAssignableFrom(nodeType))
            {
                Debug.LogError($"类型 {nodeType.Name} 不是有效的节点类型！");
                return;
            }

            if (IsEditingMethod && typeof(ShizukuLatentNode).IsAssignableFrom(nodeType))
            {
                EditorUtility.DisplayDialog(
                    "无法创建 Latent 节点",
                    "ShizukuMethod 暂不支持 Latent 节点。请在普通 Graph 或无返回值的 Blueprint Event 中使用。",
                    "确定");
                return;
            }

            try
            {
                var node = Activator.CreateInstance(nodeType) as ShizukuNodeBase;
                if (node == null)
                {
                    Debug.LogError($"无法创建节点实例: {nodeType.Name}");
                    return;
                }

                var rect = new Rect(mousePosition, new Vector2(200, 100));
                ExecuteGraphEdits(
                    "创建节点",
                    new[] { new CreateNodeOperation(node, ToFloat4(rect)) });
                AddNodeView(node, rect);

                OnGraphChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建节点失败: {ex.Message}");
            }
        }

        private void CreateGroup(Vector2 mousePosition)
        {
            var groupData = new GroupData("新建分组", new float4(mousePosition.x, mousePosition.y, 300, 200));
            ExecuteGraphEdits("创建分组", new[] { new AddGroupOperation(groupData) });
            var group = new CustomGroup(groupData)
            {
                title = "新建分组"
            };
            group.SetPosition(new Rect(mousePosition, new Vector2(300, 200)));

            AddElement(group);
            OnGraphChanged?.Invoke();
        }

        /// <summary>
        /// 清空所有节点、边和分组
        /// </summary>
        private void ClearAllNodes()
        {
            var contextName = IsEditingMethod ? $"函数 \"{_currentMethod.Name}\"" : "主图";
            if (EditorUtility.DisplayDialog("确认清空", $"确定要清空{contextName}中所有节点、边和分组吗？可使用撤销恢复。", "确定", "取消"))
            {
                ExecuteGraphEdits("清空图", new GraphEditOperation[] { new ClearGraphContextOperation() });
                LoadCurrentContext();
                OnGraphChanged?.Invoke();
            }
        }

        private static float4 ToFloat4(Rect rect)
        {
            return new float4(rect.x, rect.y, rect.width, rect.height);
        }

        #endregion

        #region 节点间连接操作

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    // 通过类型判断是否是控制流端口
                    bool isStartControlFlow = startPort is ControlFlowPort;
                    bool isTargetControlFlow = port is ControlFlowPort;

                    if (isStartControlFlow != isTargetControlFlow)
                        return; // 类型不匹配

                    // 如果是参数端口，检查类型兼容性
                    if (!isStartControlFlow)
                    {
                        var startValueType = GetPortValueType(startPort);
                        var endValueType = GetPortValueType(port);

                        if (startValueType != null && endValueType != null)
                        {
                            // 同类型：直接兼容
                            if (startValueType == endValueType)
                            {
                                compatiblePorts.Add(port);
                            }
                            // 可转换类型：也兼容，会自动插入转换节点
                            else if (ConverterNodeRegistry.CanConvert(startPort.direction == Direction.Output ? startValueType : endValueType,
                                startPort.direction == Direction.Output ? endValueType : startValueType))
                            {
                                // 设置端口颜色为蓝色，表示需要转换
                                port.portColor = new Color(0.5f, 0.7f, 1f);
                                compatiblePorts.Add(port);
                            }
                            // 不可转换：不添加到兼容列表
                        }
                        else
                        {
                            // 如果无法获取类型信息，默认兼容（向后兼容）
                            compatiblePorts.Add(port);
                        }
                    }
                    else
                    {
                        // 控制流端口直接兼容
                        compatiblePorts.Add(port);
                    }
                }
            });

            return compatiblePorts;
        }

        /// <summary>
        /// 获取端口的值类型
        /// </summary>
        private Type GetPortValueType(Port port)
        {
            if (port == null || port.portType == null)
                return null;

            // 如果自身是泛型端口（如 ParameterEdgePort<float>），获取泛型参数
            if (port.portType.IsGenericType)
            {
                var genericArgs = port.portType.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    return genericArgs[0];
                }
            }

            // 如果自身不是泛型，但基类是泛型（如 FloatParameterEdgePort : ParameterEdgePort<float>）
            var baseType = port.portType.BaseType;
            if (baseType != null && baseType.IsGenericType)
            {
                var genericArgs = baseType.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    return genericArgs[0];
                }
            }

            return null;
        }

        /// <summary>
        /// 插入类型转换节点
        /// </summary>
        /// <returns>返回两条新的边：输出节点 → 转换节点、转换节点 → 输入节点</returns>
        private List<Edge> InsertConverterNode(
            Edge originalEdge,
            Type fromType,
            Type toType,
            ICollection<GraphEditOperation> operations)
        {
            try
            {
                // 1. 创建转换节点
                var converterNode = ConverterNodeRegistry.CreateConverterNode(fromType, toType);
                if (converterNode == null)
                {
                    Debug.LogError($"  ❌ 无法创建转换节点: {fromType.Name} → {toType.Name}");
                    return null;
                }

                Debug.Log($"  ✅ 创建转换节点: {converterNode.Title}");

                // 2. 初始化临时节点，实际写入与两条新边一起由当前事务完成。
                InitializeNodeForCurrentContext(converterNode);

                // 3. 计算转换节点位置（在两个节点中间）
                var outputNodeView = originalEdge.output.node as ShizukuNodeView;
                var inputNodeView = originalEdge.input.node as ShizukuNodeView;

                var outputPos = outputNodeView.AuthoringPosition.position;
                var inputPos = inputNodeView.AuthoringPosition.position;
                var midPosition = (outputPos + inputPos) / 2f;

                converterNode.PositionAndSize = new float4(midPosition.x, midPosition.y, 150, 80);

                // 4. 创建转换节点的视图
                var converterNodeView = new ShizukuNodeView(converterNode, _runtimeGraph);
                converterNodeView.InitPort();
                converterNodeView.SetPosition(new Rect(midPosition, new Vector2(150, 80)));
                _guidToNodeViewMap[converterNode.GUID] = converterNodeView;
                AddElement(converterNodeView);

                // 5. 获取转换节点的端口
                Port converterInputPort = null;
                Port converterOutputPort = null;

                foreach (var port in converterNodeView.inputContainer.Children().OfType<Port>())
                {
                    if (!(port is ControlFlowPort))
                    {
                        converterInputPort = port;
                        break;
                    }
                }

                foreach (var port in converterNodeView.outputContainer.Children().OfType<Port>())
                {
                    if (!(port is ControlFlowPort))
                    {
                        converterOutputPort = port;
                        break;
                    }
                }

                if (converterInputPort == null || converterOutputPort == null)
                {
                    Debug.LogError("  ❌ 转换节点端口未找到");
                    RemoveElement(converterNodeView);
                    CurrentContext.Guid2NodeMap.Remove(converterNode.GUID);
                    _guidToNodeViewMap.Remove(converterNode.GUID);
                    return null;
                }

                operations.Add(new CreateNodeOperation(
                    converterNode,
                    converterNode.PositionAndSize,
                    assignRootIfEmpty: false));

                // 6. 创建新的边
                var edge1 = originalEdge.output.ConnectTo(converterInputPort);
                var edge2 = converterOutputPort.ConnectTo(originalEdge.input);

                // 7. 添加边到视图
                AddElement(edge1);
                AddElement(edge2);

                Debug.Log($"  ✅ 已插入转换节点: {outputNodeView.RuntimeNode.Title} → {converterNode.Title} → {inputNodeView.RuntimeNode.Title}");

                return new List<Edge> { edge1, edge2 };
            }
            catch (Exception ex)
            {
                Debug.LogError($"  ❌ 插入转换节点失败: {ex.Message}");
                return null;
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (_isRebuildingView || _runtimeGraph == null)
                return graphViewChange;

            var operations = new List<GraphEditOperation>();
            var graphStructureChanged = false;

            // 检查新添加的边是否会形成环，以及是否需要插入转换节点
            if (graphViewChange.edgesToCreate != null)
            {
                var edgesToRemove = new List<Edge>();
                var edgesToAdd = new List<Edge>();

                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    // 检查这条边是否会形成环
                    if (this.WouldCreateCycle(edge))
                    {
                        ShowAuthoringFeedback("这条线会形成循环，未创建连线。");
                        edgesToRemove.Add(edge);
                        continue;
                    }

                    // 检查是否需要插入类型转换节点
                    if (!(edge.input is ControlFlowPort) && !(edge.output is ControlFlowPort))
                    {
                        var outputType = GetPortValueType(edge.output);
                        var inputType = GetPortValueType(edge.input);

                        // 如果类型不同且可以转换，插入转换节点
                        if (outputType != null && inputType != null && 
                            outputType != inputType && 
                            ConverterNodeRegistry.CanConvert(outputType, inputType))
                        {
                            ShowAuthoringFeedback($"已自动插入类型转换：{outputType.Name} → {inputType.Name}");

                            // 插入转换节点
                            var newEdges = InsertConverterNode(edge, outputType, inputType, operations);
                            if (newEdges != null && newEdges.Count == 2)
                            {
                                // 移除原始边，添加新的边
                                edgesToRemove.Add(edge);
                                edgesToAdd.AddRange(newEdges);
                            }
                        }
                    }
                }

                // 移除需要替换的边
                foreach (var edge in edgesToRemove)
                {
                    graphViewChange.edgesToCreate.Remove(edge);
                }

                // 添加新的边（通过转换节点连接）
                foreach (var edge in edgesToAdd)
                {
                    graphViewChange.edgesToCreate.Add(edge);
                }
            }

            // 将所有新增连线转换为数据操作。
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var targetNode = (edge.input?.node as ShizukuNodeView)?.RuntimeNode;
                    var sourceNode = (edge.output?.node as ShizukuNodeView)?.RuntimeNode;
                    if (targetNode == null || sourceNode == null)
                        continue;

                    if (edge.input is ControlFlowPort)
                    {
                        operations.Add(new ConnectControlOperation(
                            sourceNode.GUID,
                            edge.output.portName,
                            targetNode.GUID));
                        graphStructureChanged = true;
                    }
                    else
                    {
                        operations.Add(new ConnectParameterOperation(
                            sourceNode.GUID,
                            edge.output.portName,
                            targetNode.GUID,
                            edge.input.portName));
                        graphStructureChanged = true;
                    }
                }
            }

            var removedNodeGuids = new HashSet<string>(StringComparer.Ordinal);
            if (graphViewChange.elementsToRemove != null)
            {
                graphViewChange.elementsToRemove = ExpandRemovalWithConnectedEdges(
                    graphViewChange.elementsToRemove);

                foreach (var nodeView in graphViewChange.elementsToRemove.OfType<ShizukuNodeView>())
                    removedNodeGuids.Add(nodeView.RuntimeNode.GUID);

                foreach (var element in graphViewChange.elementsToRemove)
                {
                    if (element is Edge edge)
                    {
                        var sourceNode = (edge.output?.node as ShizukuNodeView)?.RuntimeNode;
                        var targetNode = (edge.input?.node as ShizukuNodeView)?.RuntimeNode;

                        if (sourceNode == null || targetNode == null ||
                            removedNodeGuids.Contains(sourceNode.GUID) ||
                            removedNodeGuids.Contains(targetNode.GUID))
                            continue;

                        if (edge.input is ControlFlowPort)
                        {
                            operations.Add(new DisconnectControlOperation(
                                sourceNode.GUID,
                                edge.output.portName,
                                targetNode.GUID));
                            graphStructureChanged = true;
                        }
                        else
                        {
                            var edgeToRemove = CurrentEdges.FirstOrDefault(candidate =>
                                candidate.OutputNodeGuid == sourceNode.GUID &&
                                candidate.OutputPortName == edge.output.portName &&
                                candidate.InputNodeGuid == targetNode.GUID &&
                                candidate.InputPortName == edge.input.portName);
                            operations.Add(edgeToRemove != null
                                ? new DisconnectParameterOperation(edgeGuid: edgeToRemove.GUID)
                                : new DisconnectParameterOperation(
                                    sourceGuid: sourceNode.GUID,
                                    outputPortName: edge.output.portName,
                                    targetGuid: targetNode.GUID,
                                    inputPortName: edge.input.portName));
                            graphStructureChanged = true;
                        }
                    }
                }

                if (removedNodeGuids.Count > 0)
                {
                    operations.Add(new DeleteNodesOperation(removedNodeGuids));
                    graphStructureChanged = true;
                }

                var removedGroupGuids = graphViewChange.elementsToRemove
                    .OfType<CustomGroup>()
                    .Where(group => group.Data != null)
                    .Select(group => group.Data.GUID)
                    .ToArray();
                if (removedGroupGuids.Length > 0)
                {
                    operations.Add(new DeleteGroupsOperation(removedGroupGuids));
                    graphStructureChanged = true;
                }
            }

            if (graphViewChange.movedElements != null)
            {
                foreach (var element in graphViewChange.movedElements)
                {
                    if (element is CustomGroup customGroup)
                    {
                        var rect = customGroup.AuthoringPosition;
                        operations.Add(new UpdateGroupOperation(
                            customGroup.Data.GUID,
                            customGroup.title,
                            ToFloat4(rect)));
                    }
                    else if (element is ShizukuNodeView nodeView)
                    {
                        operations.Add(new MoveNodeOperation(
                            nodeView.RuntimeNode.GUID,
                            ToFloat4(nodeView.AuthoringPosition)));
                    }
                }
            }

            if (operations.Count == 0)
                return graphViewChange;

            try
            {
                ExecuteGraphEdits(GetUndoName(graphViewChange), operations);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ShizukuGraph] 图编辑失败，已回滚: {exception.Message}");
                LoadCurrentContext();
                graphViewChange.edgesToCreate?.Clear();
                graphViewChange.elementsToRemove?.Clear();
                graphViewChange.movedElements?.Clear();
                return graphViewChange;
            }

            foreach (var edge in (IEnumerable<Edge>)graphViewChange.edgesToCreate ?? Enumerable.Empty<Edge>())
            {
                if (edge.input is ControlFlowPort)
                    SetControlFlowEdgeVisualState(edge, true);
                else
                    (edge.input?.node as ShizukuNodeView)?.OnPortConnectionChanged(edge.input, true);
            }

            foreach (var edge in graphViewChange.elementsToRemove?.OfType<Edge>() ?? Enumerable.Empty<Edge>())
            {
                if (edge.input is ControlFlowPort)
                    SetControlFlowEdgeVisualState(edge, false);
                else
                    (edge.input?.node as ShizukuNodeView)?.OnPortConnectionChanged(edge.input, false);
            }

            foreach (var guid in removedNodeGuids)
                _guidToNodeViewMap.Remove(guid);

            if (graphStructureChanged)
                OnGraphChanged?.Invoke();

            return graphViewChange;
        }

        private static string GetUndoName(GraphViewChange change)
        {
            var hasCreatedEdges = change.edgesToCreate?.Count > 0;
            var hasRemovedElements = change.elementsToRemove?.Count > 0;
            var hasMovedElements = change.movedElements?.Count > 0;
            if (hasCreatedEdges && !hasRemovedElements && !hasMovedElements)
                return "连接节点";
            if (hasRemovedElements && !hasCreatedEdges && !hasMovedElements)
                return "删除图元素";
            if (hasMovedElements && !hasCreatedEdges && !hasRemovedElements)
                return "移动图元素";
            return "修改图结构";
        }

        private List<GraphElement> ExpandRemovalWithConnectedEdges(
            IEnumerable<GraphElement> requestedElements)
        {
            var requested = requestedElements.Where(element => element != null).ToList();
            var nodesToRemove = requested.OfType<ShizukuNodeView>().ToHashSet();
            if (nodesToRemove.Count == 0)
                return requested;

            // GraphView 删除节点时不会保证把相连的 Edge 一并放进回调。
            // 先处理可视边，既能清理序列化数据，也能避免画面留下幽灵线。
            var connectedEdges = edges
                .Where(edge => nodesToRemove.Contains(edge.input?.node as ShizukuNodeView)
                               || nodesToRemove.Contains(edge.output?.node as ShizukuNodeView))
                .Cast<GraphElement>();

            return connectedEdges
                .Concat(requested)
                .Distinct()
                .ToList();
        }

        private static void SetControlFlowEdgeVisualState(Edge edge, bool connected)
        {
            (edge?.input as ControlFlowPort)?.SetConnectedVisual(connected);
            (edge?.output as ControlFlowPort)?.SetConnectedVisual(connected);
        }

        #endregion

        #region 资产保存及读取

        public GraphAssetCompatibilityReport LoadFromAsset(ShizukuGraphBase graphAsset)
        {
            var compatibility = ShizukuGraphMigrationService.EnsureCurrent(graphAsset);
            if (!compatibility.CanOpen)
            {
                UnloadAsset();
                throw new GraphAssetCompatibilityException(compatibility);
            }

            _runtimeGraph = compatibility.Graph;
            _currentMethod = null; // 加载资产时重置为主图

            _runtimeGraph.Init();
            if (_runtimeGraph.DynamicPortsChangedDuringInitialization)
                EditorUtility.SetDirty(_runtimeGraph);

            LoadCurrentContext(false);

            OnEditingContextChanged?.Invoke(null);
            return compatibility;
        }

        internal void UnloadAsset()
        {
            _isRebuildingView = true;
            try
            {
                DeleteElements(graphElements.ToList());
                _guidToNodeViewMap.Clear();
                _runtimeGraph = null;
                _currentMethod = null;
            }
            finally
            {
                _isRebuildingView = false;
            }

            OnEditingContextChanged?.Invoke(null);
        }

        /// <summary>
        /// 进入函数子图编辑模式
        /// </summary>
        public void EnterMethodGraph(ShizukuMethod method)
        {
            if (method == null || _runtimeGraph == null) return;

            _currentMethod = method;

            // 确保入口/出口节点存在并同步参数
            EnsureMethodNodes(method);

            LoadCurrentContext();

            OnEditingContextChanged?.Invoke(method);
        }

        /// <summary>
        /// 确保函数的入口/出口节点存在，并同步端口与参数定义
        /// </summary>
        private void EnsureMethodNodes(ShizukuMethod method)
        {
            bool dirty = false;

            // 确保入口节点存在
            MethodEntryNode entryNode = null;
            if (!string.IsNullOrEmpty(method.EntryNodeGUID))
            {
                entryNode = method.GetNodeByGUID(method.EntryNodeGUID) as MethodEntryNode;
            }

            if (entryNode == null)
            {
                entryNode = new MethodEntryNode
                {
                    MethodGUID = method.GUID,
                    PositionAndSize = new Unity.Mathematics.float4(100, 200, 200, 100)
                };
                method.AddNode(entryNode);
                method.EntryNodeGUID = entryNode.GUID;
                dirty = true;
            }

            // 同步入口端口与函数输入参数
            entryNode.SyncPortsFromMethod(method);
            InitializeNodeForCurrentContext(entryNode);

            // 确保返回节点存在（仅当函数有输出参数时）
            MethodReturnNode returnNode = null;
            if (!string.IsNullOrEmpty(method.ReturnNodeGUID))
            {
                returnNode = method.GetNodeByGUID(method.ReturnNodeGUID) as MethodReturnNode;
            }

            if (returnNode == null && method.OutputParameters.Count > 0)
            {
                returnNode = new MethodReturnNode
                {
                    MethodGUID = method.GUID,
                    PositionAndSize = new Unity.Mathematics.float4(600, 200, 200, 100)
                };
                method.AddNode(returnNode);
                method.ReturnNodeGUID = returnNode.GUID;
                dirty = true;
            }

            // 同步返回端口与函数输出参数
            if (returnNode != null)
            {
                returnNode.SyncPortsFromMethod(method);
                InitializeNodeForCurrentContext(returnNode);
            }

            if (dirty)
            {
                UnityEditor.EditorUtility.SetDirty(_runtimeGraph);
            }
        }

        /// <summary>
        /// 返回主图编辑模式
        /// </summary>
        public void ReturnToMainGraph()
        {
            if (_runtimeGraph == null) return;

            _currentMethod = null;

            LoadCurrentContext();

            OnEditingContextChanged?.Invoke(null);
        }

        /// <summary>
        /// 加载当前编辑上下文的节点/边/分组到视图
        /// </summary>
        private void LoadCurrentContext(bool synchronizeDynamicPorts = true)
        {
            _isRebuildingView = true;
            try
            {
                if (synchronizeDynamicPorts &&
                    SynchronizeDynamicParameterPortNodes(CurrentNodes, CurrentContext))
                    EditorUtility.SetDirty(_runtimeGraph);

                // 这里只是在重建编辑器视图，不能让 GraphView 的删除回调修改资产数据。
                DeleteElements(graphElements.ToList());
                _guidToNodeViewMap.Clear();

                var currentNodes = CurrentNodes;
                var currentEdges = CurrentEdges;
                var currentGroups = CurrentGroups;
                var serializedProperties = new GraphSerializedPropertyIndex(_runtimeGraph);

                // 初始化节点
                foreach (var nodeData in currentNodes)
                {
                    var nodeView = new ShizukuNodeView(nodeData, _runtimeGraph, serializedProperties);
                    nodeView.InitPort();
                    nodeView.SetPosition(new Rect(nodeData.PositionAndSize.x, nodeData.PositionAndSize.y,
                        nodeData.PositionAndSize.z, nodeData.PositionAndSize.w));
                    _guidToNodeViewMap[nodeData.GUID] = nodeView;
                    AddElement(nodeView);
                }

                // 初始化控制流连接
                foreach (var nodeData in currentNodes)
                {
                    if (!_guidToNodeViewMap.TryGetValue(nodeData.GUID, out var currentNodeView)) continue;
                    if (currentNodeView.RuntimeNode is ShizukuNormalNode normalNode)
                    {
                        foreach (var chainPort in normalNode.ChainPorts)
                        {
                            if (!string.IsNullOrEmpty(chainPort.Value.NextNodeGuid))
                            {
                                if (_guidToNodeViewMap.TryGetValue(chainPort.Value.NextNodeGuid, out var nextNodeView))
                                {
                                    var outputPort = currentNodeView.ControlFlowContainer.RightContainer.Children()
                                        .OfType<Port>().FirstOrDefault(p => p.portName == chainPort.Value.Name);
                                    var inputPort = nextNodeView.ControlFlowContainer.LeftContainer.Children()
                                        .OfType<Port>().FirstOrDefault(p => p.portName == "Previous");
                                    if (outputPort != null && inputPort != null)
                                    {
                                        var edge = outputPort.ConnectTo(inputPort);
                                        AddElement(edge);
                                        SetControlFlowEdgeVisualState(edge, true);
                                    }
                                }
                            }
                        }
                    }
                }

                // 初始化参数边
                foreach (var edgeData in currentEdges)
                {
                    if (!_guidToNodeViewMap.TryGetValue(edgeData.OutputNodeGuid, out var sourceNodeView)) continue;
                    if (!_guidToNodeViewMap.TryGetValue(edgeData.InputNodeGuid, out var targetNodeView)) continue;

                    var outputPort = sourceNodeView.outputContainer.Children().OfType<Port>()
                        .FirstOrDefault(p => p.portName == edgeData.OutputPortName);
                    var inputPort = targetNodeView.inputContainer.Children().OfType<Port>()
                        .FirstOrDefault(p => p.portName == edgeData.InputPortName);
                    if (outputPort != null && inputPort != null)
                    {
                        var edge = outputPort.ConnectTo(inputPort);
                        AddElement(edge);
                    }
                }

                // 初始化分组
                foreach (var groupData in currentGroups)
                {
                    var group = new CustomGroup(groupData)
                    {
                        title = groupData.Title
                    };
                    group.SetPosition(new Rect(groupData.PositionAndSize.x, groupData.PositionAndSize.y,
                        groupData.PositionAndSize.z, groupData.PositionAndSize.w));
                    AddElement(group);
                }

                // 刷新断点视觉标记
                RefreshAllBreakpointVisuals();
            }
            finally
            {
                _isRebuildingView = false;
            }
        }

        private static bool SynchronizeDynamicParameterPortNodes(
            IEnumerable<ShizukuNodeBase> nodes,
            INodeContext context)
        {
            if (nodes == null || context == null)
                return false;

            var changed = false;
            foreach (var dynamicPortProvider in nodes.OfType<IDynamicParameterPortProvider>())
                changed |= dynamicPortProvider.SynchronizeDynamicParameterPorts(context);

            return changed;
        }

        public void SaveToAsset()
        {
            if (_runtimeGraph == null)
                return;

            // 在保存前更新所有Group的位置和标题数据
            foreach (var element in graphElements)
            {
                if (element is CustomGroup customGroup)
                {
                    customGroup.UpdateData();
                }
            }

            // 直接将runtimeGraph保存到asset中
            EditorUtility.SetDirty(_runtimeGraph);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 刷新节点标题（用于变量节点名称更新）
        /// </summary>
        public void RefreshNodeTitle(ShizukuNodeBase node)
        {
            if (node == null) return;

            if (_guidToNodeViewMap.TryGetValue(node.GUID, out var nodeView))
            {
                nodeView.title = node.Title;
            }
        }

        /// <summary>
        /// 重新加载当前编辑上下文的所有节点/边/分组（用于参数变更后刷新视图）
        /// </summary>
        public void RefreshCurrentView()
        {
            if (_runtimeGraph == null)
                return;

            LoadCurrentContext();
        }

        #endregion

    }

}
