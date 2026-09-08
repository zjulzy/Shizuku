using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.Graph.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier2")]
    public sealed class GraphPersistenceTests
    {
        private const string TempFolder = "Assets/__ShizukuPersistenceTests";
        private const string TempAssetPath = TempFolder + "/Graph.asset";

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);

            AssetDatabase.CreateFolder("Assets", "__ShizukuPersistenceTests");
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void RefreshAndRepeatedLoad_DoNotMutateGraphData()
        {
            var graph = CreateGraphWithParameterEdge();
            graph.Groups.Add(new GroupData("测试分组", new float4(10f, 20f, 300f, 200f)));
            SaveGraph(graph);

            var graphView = new ShizukuGraphView();
            graphView.LoadFromAsset(graph);
            AssertGraphCounts(graph, graphView, 2, 1, 1);

            graphView.RefreshCurrentView();
            AssertGraphCounts(graph, graphView, 2, 1, 1);

            graphView.LoadFromAsset(graph);
            graphView.LoadFromAsset(graph);
            AssertGraphCounts(graph, graphView, 2, 1, 1);
        }

        [Test]
        public void ParameterEdge_IsDirtyAndSurvivesReloadThenSave()
        {
            var graph = CreateGraphAsset();
            graph.AddNode(new AddNode_Float { PositionAndSize = new float4(10f, 20f, 200f, 100f) });
            graph.AddNode(new AddNode_Float { PositionAndSize = new float4(350f, 20f, 200f, 100f) });
            SaveGraph(graph);

            var graphView = new ShizukuGraphView();
            graphView.LoadFromAsset(graph);
            var nodeViews = graphView.nodes.OfType<ShizukuNodeView>().ToList();
            var output = FindPort(nodeViews[0], Direction.Output, "Result");
            var input = FindPort(nodeViews[1], Direction.Input, "A");
            var edge = output.ConnectTo(input);

            graphView.graphViewChanged(new GraphViewChange
            {
                edgesToCreate = new List<Edge> { edge }
            });
            graphView.AddElement(edge);

            Assert.That(graph.Edges, Has.Count.EqualTo(1));
            Assert.That(EditorUtility.IsDirty(graph), Is.True, "创建参数边后必须标记资产为已修改");

            graphView.SaveToAsset();
            graphView.LoadFromAsset(graph);
            graphView.LoadFromAsset(graph);
            graphView.SaveToAsset();

            Assert.That(graph.Nodes, Has.Count.EqualTo(2));
            Assert.That(graph.Edges, Has.Count.EqualTo(1), "重复加载后再次保存不应把边覆盖为空");
        }

        [Test]
        public void NewControlFlowEdge_CanBePersistedAndDeleted()
        {
            var graph = CreateGraphAsset();
            var graphView = new ShizukuGraphView();
            graphView.LoadFromAsset(graph);

            graphView.CreateNodeFromType(typeof(ShizukuRootNode), new Vector2(10f, 20f));
            graphView.CreateNodeFromType(typeof(ShizukuLogNode), new Vector2(350f, 20f));

            var root = graph.Nodes.OfType<ShizukuRootNode>().Single();
            var log = graph.Nodes.OfType<ShizukuLogNode>().Single();
            var rootView = FindNodeView(graphView, root);
            var logView = FindNodeView(graphView, log);
            var output = FindPort(rootView, Direction.Output, "next", controlFlow: true);
            var input = FindPort(logView, Direction.Input, "Previous", controlFlow: true);
            var edge = output.ConnectTo(input);

            SaveGraph(graph);
            graphView.graphViewChanged(new GraphViewChange
            {
                edgesToCreate = new List<Edge> { edge }
            });
            graphView.AddElement(edge);

            Assert.That(root.ChainPorts.ContainsKey("next"), Is.True,
                "编辑器中新建的节点必须立即初始化控制流端口");
            Assert.That(root.ChainPorts["next"].NextNodeGuid, Is.EqualTo(log.GUID));
            Assert.That(EditorUtility.IsDirty(graph), Is.True, "创建控制流边后必须标记资产为已修改");

            SaveGraph(graph);
            graphView.DeleteElements(new[] { edge });

            Assert.That(root.ChainPorts["next"].NextNodeGuid, Is.Null);
            Assert.That(EditorUtility.IsDirty(graph), Is.True, "删除控制流边后必须标记资产为已修改");
        }

        [Test]
        public void MovingNodeAndGroup_UpdatesDataAndMarksAssetDirty()
        {
            var graph = CreateGraphAsset();
            var node = new AddNode_Float { PositionAndSize = new float4(10f, 20f, 200f, 100f) };
            var groupData = new GroupData("测试分组", new float4(30f, 40f, 300f, 200f));
            graph.AddNode(node);
            graph.Groups.Add(groupData);
            SaveGraph(graph);

            var graphView = new ShizukuGraphView();
            graphView.LoadFromAsset(graph);
            var nodeView = FindNodeView(graphView, node);
            var group = graphView.graphElements.OfType<CustomGroup>().Single();

            nodeView.SetPosition(new Rect(110f, 120f, 220f, 130f));
            group.SetPosition(new Rect(230f, 240f, 300f, 200f));
            graphView.graphViewChanged(new GraphViewChange
            {
                movedElements = new List<GraphElement> { nodeView, group }
            });

            Assert.That(node.PositionAndSize.x, Is.EqualTo(110f));
            Assert.That(node.PositionAndSize.y, Is.EqualTo(120f));
            Assert.That(groupData.PositionAndSize.x, Is.EqualTo(230f));
            Assert.That(groupData.PositionAndSize.y, Is.EqualTo(240f));
            Assert.That(groupData.PositionAndSize.z, Is.EqualTo(300f));
            Assert.That(groupData.PositionAndSize.w, Is.EqualTo(200f));
            Assert.That(EditorUtility.IsDirty(graph), Is.True, "移动节点或分组后必须标记资产为已修改");
        }

        private static ShizukuGraphBase CreateGraphWithParameterEdge()
        {
            var graph = CreateGraphAsset();
            var source = new AddNode_Float { PositionAndSize = new float4(10f, 20f, 200f, 100f) };
            var target = new AddNode_Float { PositionAndSize = new float4(350f, 20f, 200f, 100f) };
            graph.AddNode(source);
            graph.AddNode(target);
            graph.AddParameterEdge(source, "Result", target, "A");
            return graph;
        }

        private static ShizukuGraphBase CreateGraphAsset()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            AssetDatabase.CreateAsset(graph, TempAssetPath);
            return graph;
        }

        private static void SaveGraph(ShizukuGraphBase graph)
        {
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            Assert.That(EditorUtility.IsDirty(graph), Is.False, "测试前置保存应清除 dirty 标记");
        }

        private static ShizukuNodeView FindNodeView(ShizukuGraphView graphView, ShizukuNodeBase node)
        {
            return graphView.nodes.OfType<ShizukuNodeView>()
                .Single(view => ReferenceEquals(view.RuntimeNode, node));
        }

        private static Port FindPort(
            ShizukuNodeView nodeView,
            Direction direction,
            string portName,
            bool controlFlow = false)
        {
            return nodeView.Query<Port>().ToList()
                .Single(port => port.direction == direction
                                && port.portName == portName
                                && (port is ControlFlowPort) == controlFlow);
        }

        private static void AssertGraphCounts(
            ShizukuGraphBase graph,
            ShizukuGraphView graphView,
            int nodes,
            int edges,
            int groups)
        {
            Assert.That(graph.Nodes, Has.Count.EqualTo(nodes));
            Assert.That(graph.Edges, Has.Count.EqualTo(edges));
            Assert.That(graph.Groups, Has.Count.EqualTo(groups));
            Assert.That(graphView.nodes.Count(), Is.EqualTo(nodes));
            Assert.That(graphView.edges.Count(), Is.EqualTo(edges));
            Assert.That(graphView.graphElements.OfType<CustomGroup>().Count(), Is.EqualTo(groups));
        }
    }
}
