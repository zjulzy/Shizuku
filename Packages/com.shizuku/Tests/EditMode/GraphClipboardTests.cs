using System;
using System.Linq;
using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.Graph.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier2")]
    public sealed class GraphClipboardTests
    {
        [Serializable]
        private sealed class ClipboardNode : ShizukuRunnableNode
        {
            [SerializeField] public string Configuration;
            [SerializeField] public string ExternalReferenceGUID;
            [SerializeField] public GameObject ReferencedObject;
            [SerializeField] public ChainPort Next = new ChainPort { Name = "next" };

            [SerializeReference]
            public IntParameterEdgePort Input = new IntParameterEdgePort
            {
                IsOut = false,
                Name = "Input"
            };

            [SerializeReference]
            public IntParameterEdgePort Output = new IntParameterEdgePort
            {
                IsOut = true,
                Name = "Output"
            };

            protected override void OnExecute()
            {
            }

            protected override bool OnSelectNextNode(out string nextNodeGUID)
            {
                nextNodeGUID = Next.NextNodeGuid;
                return !string.IsNullOrEmpty(nextNodeGUID);
            }
        }

        [Test]
        public void CopyPaste_CopiesOnlyNodes_PreservesConfigurationAndClearsConnections()
        {
            const string tempFolder = "Assets/__ShizukuClipboardTests";
            const string prefabPath = tempFolder + "/ClipboardReference.prefab";

            if (AssetDatabase.IsValidFolder(tempFolder))
                AssetDatabase.DeleteAsset(tempFolder);
            AssetDatabase.CreateFolder("Assets", "__ShizukuClipboardTests");

            var prefabSource = new GameObject("Clipboard Reference");
            var referencedObject = PrefabUtility.SaveAsPrefabAsset(prefabSource, prefabPath);
            UnityEngine.Object.DestroyImmediate(prefabSource);
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();

            try
            {
                var source = new ClipboardNode
                {
                    Configuration = "configured",
                    ExternalReferenceGUID = "external-guid",
                    ReferencedObject = referencedObject,
                    PositionAndSize = new float4(100f, 200f, 220f, 120f)
                };
                var target = new ClipboardNode
                {
                    Configuration = "target",
                    PositionAndSize = new float4(350f, 260f, 200f, 100f)
                };
                source.Next.NextNodeGuid = target.GUID;
                target.Input.InputEdgeGUID = "existing-edge-guid";

                graph.AddNode(source);
                graph.AddNode(target);
                graph.AddParameterEdge(source, "Output", target, "Input");

                var graphView = new ShizukuGraphView();
                graphView.LoadFromAsset(graph);
                foreach (var nodeView in graphView.nodes.OfType<ShizukuNodeView>())
                    graphView.AddToSelection(nodeView);

                var data = graphView.serializeGraphElements(graphView.selection.OfType<GraphElement>());

                Assert.That(graphView.canPasteSerializedData(data), Is.True);
                graphView.unserializeAndPaste("Paste", data);

                Assert.That(graph.Nodes, Has.Count.EqualTo(4));
                Assert.That(graph.Edges, Has.Count.EqualTo(1), "节点复制不应复制参数边");

                var sourceCopy = (ClipboardNode)graph.Nodes[2];
                var targetCopy = (ClipboardNode)graph.Nodes[3];

                Assert.That(sourceCopy.GUID, Is.Not.EqualTo(source.GUID));
                Assert.That(targetCopy.GUID, Is.Not.EqualTo(target.GUID));
                Assert.That(sourceCopy.GUID, Is.Not.EqualTo(targetCopy.GUID));
                Assert.That(sourceCopy.Configuration, Is.EqualTo("configured"));
                Assert.That(sourceCopy.ExternalReferenceGUID, Is.EqualTo("external-guid"));
                Assert.That(sourceCopy.ReferencedObject, Is.SameAs(referencedObject));
                Assert.That(sourceCopy.Next.NextNodeGuid, Is.Null);
                Assert.That(targetCopy.Input.InputEdgeGUID, Is.Null);
                Assert.That(sourceCopy.DependentNodes, Is.Empty);
                Assert.That(targetCopy.DependentNodes, Is.Empty);

                Assert.That(targetCopy.PositionAndSize.x - sourceCopy.PositionAndSize.x, Is.EqualTo(250f));
                Assert.That(targetCopy.PositionAndSize.y - sourceCopy.PositionAndSize.y, Is.EqualTo(60f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
                AssetDatabase.DeleteAsset(tempFolder);
            }
        }

        [Test]
        public void CopyPaste_MixedSelectionFiltersStructuralNodes()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();

            try
            {
                graph.AddNode(new ClipboardNode { Configuration = "copy me" });
                graph.AddNode(new ShizukuRootNode());
                graph.AddNode(new BlueprintEventNode());
                graph.AddNode(new BlueprintReturnNode());
                graph.AddNode(new MethodEntryNode());
                graph.AddNode(new MethodReturnNode());

                var graphView = new ShizukuGraphView();
                graphView.LoadFromAsset(graph);
                var nodeViews = graphView.nodes.OfType<ShizukuNodeView>().ToList();
                foreach (var nodeView in nodeViews)
                    graphView.AddToSelection(nodeView);

                Assert.That(
                    nodeViews.Single(view => view.RuntimeNode is ClipboardNode).capabilities
                    & Capabilities.Copiable,
                    Is.EqualTo(Capabilities.Copiable));
                Assert.That(
                    nodeViews.Where(view => view.RuntimeNode is not ClipboardNode)
                        .All(view => (view.capabilities & Capabilities.Copiable) == 0),
                    Is.True);

                var data = graphView.serializeGraphElements(graphView.selection.OfType<GraphElement>());
                graphView.unserializeAndPaste("Paste", data);

                Assert.That(graph.Nodes, Has.Count.EqualTo(7));
                Assert.That(graph.Nodes.Last(), Is.TypeOf<ClipboardNode>());

                graphView.ClearSelection();
                foreach (var structuralView in graphView.nodes.OfType<ShizukuNodeView>()
                             .Where(view => view.RuntimeNode is not ClipboardNode))
                {
                    graphView.AddToSelection(structuralView);
                }

                Assert.That(
                    graphView.serializeGraphElements(graphView.selection.OfType<GraphElement>()),
                    Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void Clipboard_DuplicateAndRepeatedPasteOffsetByTwentyPixels()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();

            try
            {
                graph.AddNode(new ClipboardNode
                {
                    Configuration = "offset copy",
                    PositionAndSize = new float4(40f, 80f, 200f, 100f)
                });

                var graphView = new ShizukuGraphView();
                graphView.LoadFromAsset(graph);
                graphView.AddToSelection(graphView.nodes.OfType<ShizukuNodeView>().Single());
                var data = graphView.serializeGraphElements(graphView.selection.OfType<GraphElement>());

                graphView.unserializeAndPaste("Duplicate", data);
                Assert.That(graph.Nodes[1].PositionAndSize.x, Is.EqualTo(60f));
                Assert.That(graph.Nodes[1].PositionAndSize.y, Is.EqualTo(100f));

                graphView.unserializeAndPaste("Paste", data);
                graphView.unserializeAndPaste("Paste", data);
                Assert.That(graph.Nodes[2].PositionAndSize.x, Is.EqualTo(0f));
                Assert.That(graph.Nodes[2].PositionAndSize.y, Is.EqualTo(0f));
                Assert.That(graph.Nodes[3].PositionAndSize.x, Is.EqualTo(20f));
                Assert.That(graph.Nodes[3].PositionAndSize.y, Is.EqualTo(20f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void Clipboard_RejectsAnotherGraphAsset_ButAllowsAnotherContextInSameGraph()
        {
            var sourceGraph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var otherGraph = ScriptableObject.CreateInstance<ShizukuGraphBase>();

            try
            {
                var sourceNode = new ClipboardNode
                {
                    Configuration = "context copy",
                    PositionAndSize = new float4(40f, 80f, 200f, 100f)
                };
                sourceGraph.AddNode(sourceNode);
                var method = new ShizukuMethod("Target Method");
                sourceGraph.AddMethod(method);

                var sourceView = new ShizukuGraphView();
                sourceView.LoadFromAsset(sourceGraph);
                sourceView.AddToSelection(sourceView.nodes.OfType<ShizukuNodeView>().Single());
                var data = sourceView.serializeGraphElements(sourceView.selection.OfType<GraphElement>());

                var otherView = new ShizukuGraphView();
                otherView.LoadFromAsset(otherGraph);
                Assert.That(otherView.canPasteSerializedData(data), Is.False);
                otherView.unserializeAndPaste("Paste", data);
                Assert.That(otherGraph.Nodes, Is.Empty);

                sourceView.EnterMethodGraph(method);
                Assert.That(sourceView.canPasteSerializedData(data), Is.True);
                sourceView.unserializeAndPaste("Paste", data);

                Assert.That(method.Nodes.Count(node => node is ClipboardNode), Is.EqualTo(1));
                var pastedNode = method.Nodes.OfType<ClipboardNode>().Single();
                Assert.That(pastedNode.Configuration, Is.EqualTo("context copy"));
                Assert.That(pastedNode.GUID, Is.Not.EqualTo(sourceNode.GUID));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceGraph);
                UnityEngine.Object.DestroyImmediate(otherGraph);
            }
        }
    }
}
