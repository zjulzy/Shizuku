using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ShizukuGraphView : GraphView
{
    private Vector2 _localMousePosition;
    private ShizukuGraphBase _runtimeGraph = new();
    private TestNodeView _entryNode;
    
    private Dictionary<string, TestNodeView> _guidToNodeViewMap = new Dictionary<string, TestNodeView>();

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
        
        InitialEntryNode();

        // 注册 graphViewChanged 委托来检测环
        graphViewChanged += OnGraphViewChanged;

        styleSheets.Add(Resources.Load<StyleSheet>("ShizukuGraphView"));
    }

    
    private void InitialEntryNode()
    {
        _entryNode = new TestNodeView();
        _entryNode.SetPosition(new Rect(100, 100, 200, 150));
        _entryNode.InitPort();

        AddElement(_entryNode);
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
        evt.menu.AppendAction("创建节点/测试节点", (a) => CreateTestNode(_localMousePosition));
        
        if(evt.target is TestNodeView nodeView)
        {
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("设置为root", (a) => _runtimeGraph.RootNodeGUID = nodeView.RuntimeNode.GUID);
        }
    }
    
    private void OnMouseDown(MouseDownEvent evt)
    {
        // 将鼠标位置转换为内容容器的本地坐标并保存，目前主要给右键菜单定位用
        _localMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
    }
    
    private void CreateTestNode(Vector2 mousePosition)
    {
        var node = new TestNodeView();
        node.InitPort();
        
        node.SetPosition(new Rect(mousePosition, new Vector2(200,100)));
        _runtimeGraph.AddNode(node.RuntimeNode);
        AddElement(node);
    }

    #endregion


    #region 节点间连接

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        
        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
            {
                if(startPort.portName == "Next" && port.portName != "Previous")
                    return;
                if(startPort.portName == "Previous" && port.portName != "Next")
                    return;
                
                compatiblePorts.Add(port);
            }
        });
        
        return compatiblePorts;
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        // 检查新添加的边是否会形成环
        if (graphViewChange.edgesToCreate != null)
        {
            var edgesToRemove = new List<Edge>();
            
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                // 检查这条边是否会形成环
                if (this.WouldCreateCycle(edge))
                {
                    edgesToRemove.Add(edge);
                }
            }
            
            // 移除会形成环的边
            foreach (var edge in edgesToRemove)
            {
                graphViewChange.edgesToCreate.Remove(edge);
            }
        }
        
        // 通知图和节点进行相应的更新
        if (graphViewChange.edgesToCreate != null)
        {
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                var targetNode = (edge.input.node as TestNodeView).RuntimeNode;
                var sourceNode = (edge.output.node as TestNodeView).RuntimeNode;
                // 暂时使用字符串来区分端口，如果端口名是previous或next则认为是控制流边，否则认为是参数边
                // 控制流边只设置节点间的连接关系，参数边则需要在图中添加边数据
                if (edge.input.portName == "Previous" && edge.output.portName == "Next")
                {
                    sourceNode.NextNodeGuid = targetNode.GUID;
                }
                else
                {
                    _runtimeGraph.AddParameterEdge(
                        sourceNode,
                        edge.output.portName,
                        targetNode,
                        edge.input.portName
                    );
                }
                
            }
        }

        if (graphViewChange.elementsToRemove != null)
        {
            foreach (var element in graphViewChange.elementsToRemove)
            {
                // 处理边的移除
                if (element is Edge edge)
                {
                    var sourceNode = (edge.output.node as TestNodeView)?.RuntimeNode;
                    var targetNode = (edge.input.node as TestNodeView)?.RuntimeNode;
                    
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
                else if (element is TestNodeView nodeView)  
                {
                    _runtimeGraph.Nodes.Remove(nodeView.RuntimeNode);
                    
                    // 同时移除所有与该节点相关的边
                    _runtimeGraph.Edges.RemoveAll(e =>
                        e.OutputNodeGuid == nodeView.RuntimeNode.GUID ||
                        e.InputNodeGuid == nodeView.RuntimeNode.GUID
                    );
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

        if(_entryNode != null)
            RemoveElement(_entryNode);
        
        // 初始化节点
        graphAsset.Nodes.ForEach(nodeData =>
        {
            var nodeView = new TestNodeView(nodeData);
            nodeView.InitPort();
            nodeView.SetPosition(new Rect(nodeData.PositionAndSize.x, nodeData.PositionAndSize.y, nodeData.PositionAndSize.z,
                nodeData.PositionAndSize.w));
            _guidToNodeViewMap[nodeData.GUID] = nodeView;
            AddElement(nodeView);
        });
        graphAsset.Nodes.ForEach(nodeData =>
        {
            var currentNodeView = _guidToNodeViewMap[nodeData.GUID];
            // 设置控制流连接
            if (!string.IsNullOrEmpty(nodeData.NextNodeGuid))
            {
                if (_guidToNodeViewMap.TryGetValue(nodeData.NextNodeGuid, out var nextNodeView))
                {
                    var outputPort = currentNodeView.outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == "Next");
                    var inputPort = nextNodeView.inputContainer  .Children().OfType<Port>().FirstOrDefault(p => p.portName == "Previous");
                    if (outputPort != null && inputPort != null)
                    {
                        var edge = outputPort.ConnectTo(inputPort);
                        AddElement(edge);
                    }
                }
            }
        });
        
        
        // 初始化边
        graphAsset.Edges.ForEach(edgeData =>
        {
            var sourceNodeView = this.nodes.ToList().Find(n => (n as TestNodeView).RuntimeNode.GUID == edgeData.OutputNodeGuid) as TestNodeView;
            var targetNodeView = this.nodes.ToList().Find(n => (n as TestNodeView).RuntimeNode.GUID == edgeData.InputNodeGuid) as TestNodeView;
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
    }
    
    public void SaveToAsset()
    {
        // 直接将runtimeGraph保存到asset中
        EditorUtility.SetDirty(_runtimeGraph);
        AssetDatabase.SaveAssets();
    }

    #endregion
    
}

public class TestNodeView : Node
{
    private ShizukuNodeBase _node;
    public ShizukuNodeBase RuntimeNode => _node;

    public TestNodeView()
    {
        _node = new ShizukuNodeBase()
        {
            GUID = System.Guid.NewGuid().ToString(),
        };
        title = "Test Node";
    }
    
    public TestNodeView(ShizukuNodeBase node)
    {
        _node = node;
        title = "Test Node";
    }

    public void InitPort()
    {
        var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Chain));
        port.portName = "Next";
        outputContainer.Add(port);
        
        port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(Chain));
        port.portName = "Previous";
        inputContainer.Add(port);
        
        port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(int));
        port.portName = _node.Parameter.Name;
        inputContainer.Add(port);
        
        port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(int));
        port .portName = _node.ParameterResult.Name;
        outputContainer.Add(port);
        
        RefreshExpandedState();
        RefreshPorts();
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        _node.PositionAndSize = new float4(newPos.x, newPos.y, newPos.width, newPos.height);
    }
}

