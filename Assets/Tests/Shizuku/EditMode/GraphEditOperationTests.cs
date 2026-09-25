using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.Graph.Editor;
using Shizuku.Graph.Editor.Mcp;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Shizuku.Tests.EditMode
{
    public sealed class GraphEditOperationTests
    {
        private const string TempFolder = "Assets/__GraphEditOperationTests";
        private const string GraphPath = TempFolder + "/Graph.asset";

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);
            AssetDatabase.CreateFolder("Assets", "__GraphEditOperationTests");
        }

        [TearDown]
        public void TearDown()
        {
            var graph = AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(GraphPath);
            if (graph != null)
                Undo.ClearUndo(graph);
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void Apply_BatchesCreateMoveConnectAndDeleteWithStableEdgeGuid()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var source = new AddNode_Float { GUID = "source" };
            var target = new AddNode_Float { GUID = "target" };

            GraphEditService.Apply(graph, string.Empty, new GraphEditOperation[]
            {
                new CreateNodeOperation(source, new float4(10, 20, 200, 100)),
                new CreateNodeOperation(target, new float4(300, 20, 200, 100)),
                new ConnectParameterOperation("source", "Result", "target", "A", "edge-guid"),
                new MoveNodeOperation("target", new float4(420, 80, 240, 120))
            });

            Assert.That(graph.Nodes, Has.Count.EqualTo(2));
            Assert.That(graph.Edges, Has.Count.EqualTo(1));
            Assert.That(graph.Edges[0].GUID, Is.EqualTo("edge-guid"));
            Assert.That(target.PositionAndSize, Is.EqualTo(new float4(420, 80, 240, 120)));

            GraphEditService.Apply(
                graph,
                string.Empty,
                new GraphEditOperation[] { new DeleteNodesOperation(new[] { "source" }) });

            Assert.That(graph.Nodes.Single().GUID, Is.EqualTo("target"));
            Assert.That(graph.Edges, Is.Empty);
            Assert.That(graph.Guid2NodeMap.ContainsKey("source"), Is.False);
            Assert.That(graph.Guid2EdgeMap.ContainsKey("edge-guid"), Is.False);
        }

        [Test]
        public void DeleteNode_ClearsIncomingControlReference()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var root = new ShizukuRootNode { GUID = "root" };
            var middle = new ShizukuLogNode { GUID = "middle" };
            var tail = new ShizukuLogNode { GUID = "tail" };

            GraphEditService.Apply(graph, string.Empty, new GraphEditOperation[]
            {
                new CreateNodeOperation(root, default),
                new CreateNodeOperation(middle, default),
                new CreateNodeOperation(tail, default),
                new ConnectControlOperation("root", "next", "middle"),
                new ConnectControlOperation("middle", "next", "tail")
            });

            GraphEditService.Apply(
                graph,
                string.Empty,
                new GraphEditOperation[] { new DeleteNodesOperation(new[] { "middle" }) });

            Assert.That(root.ChainPorts["next"].NextNodeGuid, Is.Null);
            Assert.That(graph.RootNodeGUID, Is.EqualTo("root"));
            Assert.That(graph.Nodes.Select(node => node.GUID), Is.EquivalentTo(new[] { "root", "tail" }));
        }

        [Test]
        public void Executor_RollsBackWholeBatchWhenAnyOperationFails()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var first = new AddNode_Float { GUID = "duplicate" };
            var second = new AddNode_Float { GUID = "duplicate" };

            Assert.Throws<InvalidOperationException>(() => UnityGraphEditExecutor.Execute(
                graph,
                string.Empty,
                new GraphEditOperation[]
                {
                    new CreateNodeOperation(first, default),
                    new CreateNodeOperation(second, default)
                },
                "失败事务",
                new GraphEditExecutionOptions { RecordUndo = false }));

            Assert.That(graph.Nodes, Is.Empty);
            Assert.That(graph.Edges, Is.Empty);
        }

        [Test]
        public void Executor_UsesUnityUndoAsTheOnlyHistoryStack()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            AssetDatabase.CreateAsset(graph, GraphPath);

            UnityGraphEditExecutor.Execute(
                graph,
                string.Empty,
                new GraphEditOperation[]
                {
                    new CreateNodeOperation(
                        new AddNode_Float { GUID = "undo-node" },
                        new float4(10, 20, 200, 100))
                },
                "创建测试节点");
            Undo.FlushUndoRecordObjects();
            Assert.That(graph.Nodes.Single().GUID, Is.EqualTo("undo-node"));

            Undo.PerformUndo();
            graph.Init();
            Assert.That(graph.Nodes, Is.Empty);

            Undo.PerformRedo();
            graph.Init();
            Assert.That(graph.Nodes.Single().GUID, Is.EqualTo("undo-node"));
        }

        [Test]
        public void Executor_MoveKeepsLiveManagedReferenceInSync()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var node = new AddNode_Float
            {
                GUID = "move-node",
                PositionAndSize = new float4(10, 20, 200, 100)
            };
            graph.AddNode(node);
            AssetDatabase.CreateAsset(graph, GraphPath);

            UnityGraphEditExecutor.Execute(
                graph,
                string.Empty,
                new GraphEditOperation[]
                {
                    new MoveNodeOperation("move-node", new float4(110, 120, 220, 130))
                },
                "移动测试节点");

            Assert.That(graph.Nodes[0], Is.SameAs(node),
                "记录 Undo 不能让已经打开的 NodeView 继续引用旧节点实例");
            Assert.That(node.PositionAndSize, Is.EqualTo(new float4(110, 120, 220, 130)));
        }

        [Test]
        public void GraphView_MoveBatchUpdatesTheDisplayedNodeAndGroupInstances()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var node = new AddNode_Float
            {
                GUID = "view-node",
                PositionAndSize = new float4(10, 20, 200, 100)
            };
            var groupData = new GroupData("测试分组", new float4(30, 40, 300, 200));
            graph.AddNode(node);
            graph.Groups.Add(groupData);
            AssetDatabase.CreateAsset(graph, GraphPath);
            AssetDatabase.SaveAssets();

            var graphView = new ShizukuGraphView();
            graphView.LoadFromAsset(graph);
            var nodeView = graphView.nodes.OfType<ShizukuNodeView>().Single();
            var group = graphView.graphElements.OfType<CustomGroup>().Single();
            Assert.That(nodeView.RuntimeNode, Is.SameAs(node));
            Assert.That(group.Data, Is.SameAs(groupData));

            nodeView.SetPosition(new Rect(110, 120, 220, 130));
            group.SetPosition(new Rect(230, 240, 300, 200));
            graphView.graphViewChanged(new GraphViewChange
            {
                movedElements = new System.Collections.Generic.List<GraphElement> { nodeView, group }
            });

            Assert.That(graph.Nodes[0], Is.SameAs(node));
            Assert.That(graph.Groups[0], Is.SameAs(groupData));
            Assert.That(node.PositionAndSize, Is.EqualTo(new float4(110, 120, 220, 130)));
            Assert.That(groupData.PositionAndSize, Is.EqualTo(new float4(230, 240, 300, 200)));
        }

        [Test]
        public void McpCommit_ReturnsAndPersistsGeneratedParameterEdgeGuid()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            AssetDatabase.CreateAsset(graph, GraphPath);
            AssetDatabase.SaveAssets();

            var operations = new JArray
            {
                CreateNodeJson("source", 10),
                CreateNodeJson("target", 300),
                new JObject
                {
                    ["op"] = "connect_parameter",
                    ["sourceGuid"] = "source",
                    ["outputPort"] = "Result",
                    ["targetGuid"] = "target",
                    ["inputPort"] = "A"
                }
            };

            var committed = (JObject)ShizukuMcpGraphService.Handle("graph_apply", new JObject
            {
                ["assetPath"] = GraphPath,
                ["expectedRevision"] = ShizukuMcpGraphService.ReadGraph(new JObject { ["assetPath"] = GraphPath })["revision"],
                ["dryRun"] = false,
                ["operations"] = operations
            });

            var edgeGuid = committed["operations"]?[2]?.Value<string>("edgeGuid");
            Assert.That(edgeGuid, Is.Not.Null.And.Not.Empty);
            Assert.That(graph.Edges.Single().GUID, Is.EqualTo(edgeGuid));
        }

        private static JObject CreateNodeJson(string guid, float x)
        {
            return new JObject
            {
                ["op"] = "create_node",
                ["type"] = typeof(AddNode_Float).Assembly.GetName().Name + ":" + typeof(AddNode_Float).FullName,
                ["guid"] = guid,
                ["position"] = new JArray(x, 20, 200, 100)
            };
        }
    }
}
