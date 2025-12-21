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

    public void LoadFromAsset(ShizukuGraphBase graphAsset)
    {
        _runtimeGraph = graphAsset;
        _runtimeGraph.Init();

        if(_entryNode != null)
            RemoveElement(_entryNode);
        graphAsset.Nodes.ForEach(nodeData =>
        {
            var nodeView = new TestNodeView(nodeData);
            nodeView.InitPort();
            nodeView.SetPosition(new Rect(nodeData.PositionAndSize.x, nodeData.PositionAndSize.y, nodeData.PositionAndSize.z,
                nodeData.PositionAndSize.w));
            AddElement(nodeView);
        });
        
        graphAsset.Edges.ForEach(edgeData =>
        {
            var sourceNodeView = this.nodes.ToList().Find(n => (n as TestNodeView).RuntimeNode.GUID == edgeData.OutputNodeGuid) as TestNodeView;
            var targetNodeView = this.nodes.ToList().Find(n => (n as TestNodeView).RuntimeNode.GUID == edgeData.InputNodeGuid) as TestNodeView;
            if (sourceNodeView != null && targetNodeView != null)
            {
                var outputPort = sourceNodeView.outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.name == edgeData.OutputPortName);
                var inputPort = targetNodeView.inputContainer.Children().OfType<Port>().FirstOrDefault(p => p.name == edgeData.InputPortName);
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

    private void InitialEntryNode()
    {
        _entryNode = new TestNodeView();
        _entryNode.SetPosition(new Rect(100, 100, 200, 150));
        _entryNode.InitPort();

        AddElement(_entryNode);
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        // 将鼠标位置转换为内容容器的本地坐标并保存，目前主要给右键菜单定位用
        _localMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        // 保留默认菜单项(可选)
        base.BuildContextualMenu(evt);

        // 添加带分隔符的菜单项
        evt.menu.AppendSeparator();

        // 添加子菜单
        evt.menu.AppendAction("创建节点/测试节点", (a) => CreateTestNode(_localMousePosition));
    }

    private void CreateTestNode(Vector2 mousePosition)
    {
        var node = new TestNodeView();
        node.InitPort();
        
        node.SetPosition(new Rect(mousePosition, new Vector2(200,100)));
        _runtimeGraph.AddNode(node.RuntimeNode);
        AddElement(node);
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        
        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
            {
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
                
            }
        }
        
        return graphViewChange;
    }
    
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
        var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        port.portName = "Next";
        outputContainer.Add(port);

        port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        port.portName = "Previous";
        inputContainer.Add(port);
        RefreshExpandedState();
        RefreshPorts();
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        _node.PositionAndSize = new float4(newPos.x, newPos.y, newPos.width, newPos.height);
    }
}

public static class ShizukuGraphViewExtensions
{
    /// <summary>
    /// 检查添加这条边是否会形成环
    /// 使用DFS（深度优先搜索）检测从目标节点是否能回到源节点
    /// </summary>
    public static bool WouldCreateCycle(this ShizukuGraphView graph, Edge newEdge)
    {
        var sourceNode = newEdge.output.node;
        var targetNode = newEdge.input.node;

        // 使用DFS检查从targetNode出发是否能到达sourceNode
        var visited = new HashSet<Node>();
        return HasPathDFS(targetNode, sourceNode, visited);
    }

    /// <summary>
    /// 深度优先搜索：检查从startNode是否存在路径到达targetNode
    /// </summary>
    private static bool HasPathDFS(Node startNode, Node targetNode, HashSet<Node> visited)
    {
        if (startNode == targetNode)
        {
            return true; // 找到了从target到source的路径，说明会形成环
        }
        
        if (visited.Contains(startNode))
        {
            return false; // 已经访问过这个节点
        }
        
        visited.Add(startNode);
        
        // 遍历当前节点的所有输出边
        var outputPorts = startNode.outputContainer.Query<Port>().ToList();
        foreach (var port in outputPorts)
        {
            if (port.connected)
            {
                foreach (var edge in port.connections)
                {
                    var nextNode = edge.input.node;
                    if (HasPathDFS(nextNode, targetNode, visited))
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
}