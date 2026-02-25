using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ShizukuGraphView : GraphView
{
    private Vector2 _localMousePosition;
    private ShizukuGraphBase _runtimeGraph;
    private ShizukuNodeView _entryNode;

    private Dictionary<string, ShizukuNodeView> _guidToNodeViewMap = new Dictionary<string, ShizukuNodeView>();
    
    public System.Action OnGraphChanged;

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

        // 注册 graphViewChanged 委托来检测环
        graphViewChanged += OnGraphViewChanged;

        // 设置删除回调，支持删除节点，边和分组
        deleteSelection = (operationName, askUser) =>
        {
            DeleteElements(selection.OfType<GraphElement>().ToList());
        };

        // 注册节点创建请求，使用 SearchWindow
        nodeCreationRequest = context =>
        {
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), CreateNodeSearchWindowProvider(context.screenMousePosition));
        };

        styleSheets.Add(Resources.Load<StyleSheet>("ShizukuGraphView"));
        
        // 创建 AI 助手窗口
        CreateAIAssistantWindow();
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
        
        var provider = ScriptableObject.CreateInstance<NodeSearchWindowProvider>();
        provider.Initialize(this, graphMousePosition);
        return provider;
    }

    #endregion

    #region 编辑器操作

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        // 保留默认菜单项(可选)
        base.BuildContextualMenu(evt);

        // 添加带分隔符的菜单项
        evt.menu.AppendSeparator();

        // 添加子菜单
        evt.menu.AppendAction("创建节点/根节点", (a) => CreateNode<ShizukuRootNode>(_localMousePosition));
        evt.menu.AppendAction("创建节点/+1节点", (a) => CreateNode<ShizikuAddOneNode>(_localMousePosition));
        evt.menu.AppendAction("创建节点/打印节点", (a) => CreateNode<ShizukuLogNode>(_localMousePosition));
        evt.menu.AppendAction("创建节点/条件节点", (a) => CreateNode<ShizukuIfNode>(_localMousePosition));

        // 蓝图节点
        evt.menu.AppendSeparator();
        BuildBlueprintEventMenu(evt);
        evt.menu.AppendAction("蓝图节点/获取属性/Float", (a) => CreateNode<GetPropertyNode_Float>(_localMousePosition));
        evt.menu.AppendAction("蓝图节点/获取属性/Int", (a) => CreateNode<GetPropertyNode_Int>(_localMousePosition));
        evt.menu.AppendAction("蓝图节点/获取属性/Bool", (a) => CreateNode<GetPropertyNode_Bool>(_localMousePosition));
        evt.menu.AppendAction("蓝图节点/获取属性/String", (a) => CreateNode<GetPropertyNode_String>(_localMousePosition));
        evt.menu.AppendAction("蓝图节点/获取属性/Generic", (a) => CreateNode<GetPropertyNode>(_localMousePosition));
        evt.menu.AppendAction("蓝图节点/设置属性/Float", (a) => CreateNode<SetPropertyNode_Float>(_localMousePosition));
        evt.menu.AppendAction("蓝图节点/设置属性/Int", (a) => CreateNode<SetPropertyNode_Int>(_localMousePosition));
        evt.menu.AppendAction("蓝图节点/设置属性/Bool", (a) => CreateNode<SetPropertyNode_Bool>(_localMousePosition));
        evt.menu.AppendAction("蓝图节点/设置属性/String", (a) => CreateNode<SetPropertyNode_String>(_localMousePosition));
        evt.menu.AppendAction("蓝图节点/设置属性/Generic", (a) => CreateNode<SetPropertyNode>(_localMousePosition));

        evt.menu.AppendSeparator();
        evt.menu.AppendAction("创建分组", (a) => CreateGroup(_localMousePosition));

        evt.menu.AppendSeparator();
        evt.menu.AppendAction("清空所有节点", (a) => ClearAllNodes());
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        // 将鼠标位置转换为内容容器的本地坐标并保存，目前主要给右键菜单定位用
        _localMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
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

        var nodeView = new ShizukuNodeView(node, _runtimeGraph);
        nodeView.InitPort();
        nodeView.SetPosition(new Rect(mousePosition, new Vector2(200, 100)));

        _runtimeGraph.AddNode(node);
        AddElement(nodeView);
        EditorUtility.SetDirty(_runtimeGraph);
        
        OnGraphChanged?.Invoke();
    }

    private ParameterEdgePort CreatePortForType(string name, Type type)
    {
        if (type == typeof(float))
            return new FloatParameterEdgePort { IsOut = true, Name = name };
        else if (type == typeof(int))
            return new IntParameterEdgePort { IsOut = true, Name = name };
        else if (type == typeof(bool))
            return new BoolParameterEdgePort { IsOut = true, Name = name };
        else if (type == typeof(string))
            return new StringParameterEdgePort { IsOut = true, Name = name };
        else
            return new ObjectParameterEdgePort { IsOut = true, Name = name };
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
        if (typeof(TNode) == typeof(ShizukuRootNode) && _entryNode != null)
        {
            Debug.LogWarning("只能有一个根节点！");
            return;
        }

        var node = new TNode();

        var nodeView = new ShizukuNodeView(node, _runtimeGraph);
        nodeView.InitPort();
        nodeView.SetPosition(new Rect(mousePosition, new Vector2(200, 100)));

        _runtimeGraph.AddNode(node);
        AddElement(nodeView);
        EditorUtility.SetDirty(_runtimeGraph);

        if (typeof(TNode) == typeof(ShizukuRootNode))
        {
            _entryNode = nodeView;
            _runtimeGraph.RootNodeGUID = node.GUID;
        }
    }
    
    /// <summary>
    /// 用于 SearchWindow创建节点
    /// AI-generated
    /// </summary>
    public void CreateNodeFromType(Type nodeType, Vector2 mousePosition)
    {
        if (!typeof(ShizukuNodeBase).IsAssignableFrom(nodeType))
        {
            Debug.LogError($"类型 {nodeType.Name} 不是有效的节点类型！");
            return;
        }

        if (nodeType == typeof(ShizukuRootNode) && _entryNode != null)
        {
            Debug.LogWarning("只能有一个根节点！");
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

            var nodeView = new ShizukuNodeView(node, _runtimeGraph);
            nodeView.InitPort();
            nodeView.SetPosition(new Rect(mousePosition, new Vector2(200, 100)));

            _runtimeGraph.AddNode(node);
            _guidToNodeViewMap[node.GUID] = nodeView;
            AddElement(nodeView);
            EditorUtility.SetDirty(_runtimeGraph);

            if (nodeType == typeof(ShizukuRootNode))
            {
                _entryNode = nodeView;
                _runtimeGraph.RootNodeGUID = node.GUID;
            }
            
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
        var group = new CustomGroup(groupData)
        {
            title = "新建分组"
        };
        group.SetPosition(new Rect(mousePosition, new Vector2(300, 200)));

        // 添加到运行时图中
        if (_runtimeGraph != null)
        {
            _runtimeGraph.Groups.Add(groupData);
            EditorUtility.SetDirty(_runtimeGraph);
        }

        AddElement(group);
    }

    /// <summary>
    /// 清空所有节点、边和分组
    /// </summary>
    private void ClearAllNodes()
    {
        if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有节点、边和分组吗？此操作无法撤销！", "确定", "取消"))
        {
            // 清空所有GraphView元素
            DeleteElements(graphElements.ToList());

            // 清空运行时图数据
            if (_runtimeGraph != null)
            {
                _runtimeGraph.Nodes.Clear();
                _runtimeGraph.Edges.Clear();
                _runtimeGraph.Groups.Clear();
                _runtimeGraph.RootNodeGUID = null;
                EditorUtility.SetDirty(_runtimeGraph);
            }

            // 清空内部引用
            _entryNode = null;
            _guidToNodeViewMap.Clear();
        }
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
                        else if (ConverterNodeRegistry.CanConvert(startValueType, endValueType))
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
    private List<Edge> InsertConverterNode(Edge originalEdge, Type fromType, Type toType)
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
            
            // 2. 添加到图数据中
            _runtimeGraph.AddNode(converterNode);
            
            // 3. 计算转换节点位置（在两个节点中间）
            var outputNodeView = originalEdge.output.node as ShizukuNodeView;
            var inputNodeView = originalEdge.input.node as ShizukuNodeView;
            
            var outputPos = outputNodeView.GetPosition().position;
            var inputPos = inputNodeView.GetPosition().position;
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
                _runtimeGraph.Nodes.Remove(converterNode);
                return null;
            }
            
            // 6. 创建新的边
            var edge1 = originalEdge.output.ConnectTo(converterInputPort);
            var edge2 = converterOutputPort.ConnectTo(originalEdge.input);
            
            // 7. 添加边到视图
            AddElement(edge1);
            AddElement(edge2);
            
            // 8. 标记图已修改
            EditorUtility.SetDirty(_runtimeGraph);
            
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
                    Debug.LogWarning($"  ⚠️ 边会形成环，取消创建");
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
                        Debug.Log($"  🔄 检测到类型转换需求: {outputType.Name} → {inputType.Name}");
                        
                        // 插入转换节点
                        var newEdges = InsertConverterNode(edge, outputType, inputType);
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

        // 通知图和节点进行相应的更新
        if (graphViewChange.edgesToCreate != null)
        {
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                var targetNode = (edge.input.node as ShizukuNodeView).RuntimeNode;
                var sourceNode = (edge.output.node as ShizukuNodeView).RuntimeNode;
                // 暂时使用字符串来区分端口，如果端口名是previous或next则认为是控制流边，否则认为是参数边
                // 控制流边只设置节点间的连接关系，参数边则需要在图中添加边数据
                if (edge.input is ControlFlowPort)
                {
                    var sourceNormalNode = sourceNode as ShizukuNormalNode;
                    if (sourceNormalNode.ChainPorts.TryGetValue(edge.output.portName, out var chainPort))
                    {
                        chainPort.NextNodeGuid = targetNode.GUID;
                    }
                    Debug.Log($"  📌 设置控制流: {sourceNormalNode.Title} -> {targetNode.Title}");
                }
                else
                {
                    _runtimeGraph.AddParameterEdge(
                        sourceNode,
                        edge.output.portName,
                        targetNode,
                        edge.input.portName
                    );
                    Debug.Log($"  📌 添加参数边: {sourceNode.Title}.{edge.output.portName} -> {targetNode.Title}.{edge.input.portName}");
                }
            }
        }

        if (graphViewChange.elementsToRemove != null)
        {
            foreach (var element in graphViewChange.elementsToRemove)
            {
                Debug.Log($"  ❌ 删除元素: {element.GetType().Name}");

                // 处理边的移除
                if (element is Edge edge)
                {
                    var sourceNode = (edge.output.node as ShizukuNodeView)?.RuntimeNode;
                    var targetNode = (edge.input.node as ShizukuNodeView)?.RuntimeNode;

                    if (sourceNode != null && targetNode != null)
                    {
                        // 从 _runtimeGraph 中移除对应的边
                        var edgeToRemove = _runtimeGraph.Edges.FirstOrDefault(e =>
                            e.OutputNodeGuid == sourceNode.GUID &&
                            e.OutputPortName == edge.output.portName &&
                            e.InputNodeGuid == targetNode.GUID &&
                            e.InputPortName == edge.input.portName
                        );

                        if (edgeToRemove != null)
                        {
                            _runtimeGraph.Edges.Remove(edgeToRemove);
                        }
                    }
                }
                // 处理节点的移除
                else if (element is ShizukuNodeView nodeView)
                {
                    // 如果删除的是根节点，清空引用
                    if (nodeView == _entryNode)
                    {
                        _entryNode = null;
                        _runtimeGraph.RootNodeGUID = null;
                    }

                    _runtimeGraph.Nodes.Remove(nodeView.RuntimeNode);

                    // 同时移除所有与该节点相关的边
                    _runtimeGraph.Edges.RemoveAll(e =>
                        e.OutputNodeGuid == nodeView.RuntimeNode.GUID ||
                        e.InputNodeGuid == nodeView.RuntimeNode.GUID
                    );
                }
                // 处理分组的移除
                else if (element is CustomGroup customGroup)
                {
                    if (_runtimeGraph != null && customGroup.Data != null)
                    {
                        _runtimeGraph.Groups.Remove(customGroup.Data);
                    }
                }
            }
        }

        return graphViewChange;
    }

    #endregion

    #region 资产保存及读取

    public void LoadFromAsset(ShizukuGraphBase graphAsset)
    {
        _runtimeGraph = graphAsset;
        _runtimeGraph.Init();

        // 清空所有现有元素
        DeleteElements(graphElements.ToList());
        _entryNode = null;
        _guidToNodeViewMap.Clear();

        // 初始化节点
        graphAsset.Nodes.ForEach(nodeData =>
        {
            var nodeView = new ShizukuNodeView(nodeData, graphAsset);
            nodeView.InitPort();
            nodeView.SetPosition(new Rect(nodeData.PositionAndSize.x, nodeData.PositionAndSize.y, nodeData.PositionAndSize.z,
                nodeData.PositionAndSize.w));
            _guidToNodeViewMap[nodeData.GUID] = nodeView;
            AddElement(nodeView);

            // 如果是根节点，设置_entryNode引用
            if (nodeData is ShizukuRootNode)
            {
                _entryNode = nodeView;
            }
        });

        // 初始化控制流连接
        graphAsset.Nodes.ForEach(nodeData =>
        {
            var currentNodeView = _guidToNodeViewMap[nodeData.GUID];
            if (currentNodeView.RuntimeNode is ShizukuNormalNode normalNode)
            {
                foreach (var chainPort in normalNode.ChainPorts)
                {
                    // 设置控制流连接
                    if (!string.IsNullOrEmpty(chainPort.Value.NextNodeGuid))
                    {
                        if (_guidToNodeViewMap.TryGetValue(chainPort.Value.NextNodeGuid, out var nextNodeView))
                        {
                            var outputPort = currentNodeView.ControlFlowContainer.RightContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == chainPort.Value.Name);
                            var inputPort = nextNodeView.ControlFlowContainer.LeftContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == "Previous");
                            if (outputPort != null && inputPort != null)
                            {
                                var edge = outputPort.ConnectTo(inputPort);
                                AddElement(edge);
                            }
                        }
                    }
                }
            }
        });


        // 初始化参数边
        graphAsset.Edges.ForEach(edgeData =>
        {
            var sourceNodeView = this.nodes.ToList().Find(n => (n as ShizukuNodeView).RuntimeNode.GUID == edgeData.OutputNodeGuid) as ShizukuNodeView;
            var targetNodeView = this.nodes.ToList().Find(n => (n as ShizukuNodeView).RuntimeNode.GUID == edgeData.InputNodeGuid) as ShizukuNodeView;
            if (sourceNodeView != null && targetNodeView != null)
            {
                var outputPort = sourceNodeView.outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == edgeData.OutputPortName);
                var inputPort = targetNodeView.inputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == edgeData.InputPortName);
                if (outputPort != null && inputPort != null)
                {
                    var edge = outputPort.ConnectTo(inputPort);
                    AddElement(edge);
                }
            }
        });

        // 初始化分组
        graphAsset.Groups.ForEach(groupData =>
        {
            var group = new CustomGroup(groupData)
            {
                title = groupData.Title
            };
            group.SetPosition(new Rect(groupData.PositionAndSize.x, groupData.PositionAndSize.y,
                groupData.PositionAndSize.z, groupData.PositionAndSize.w));
            AddElement(group);
        });
    }

    public void SaveToAsset()
    {
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

    #endregion

}
