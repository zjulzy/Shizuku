using System;
using NUnit.Framework;
using Shizuku.Graph;
using UnityEditor;
using UnityEngine;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier2")]
    public sealed class GraphExecutionTests
    {
        [Serializable]
        private sealed class CountingValueNode : ShizukuValueNode
        {
            [SerializeReference]
            public IntParameterEdgePort Output = new()
            {
                IsOut = true,
                Name = "Value"
            };

            public int NextValue;
            public int ComputeCount;

            protected override void OnComputeOutputValues()
            {
                ComputeCount++;
                Output.Value = NextValue;
            }
        }

        [Serializable]
        private sealed class CapturePairNode : ShizukuRunnableNode
        {
            [SerializeReference]
            public IntParameterEdgePort Left = new()
            {
                IsOut = false,
                Name = "Left"
            };

            [SerializeReference]
            public IntParameterEdgePort Right = new()
            {
                IsOut = false,
                Name = "Right"
            };

            public int CapturedLeft;
            public int CapturedRight;

            protected override void OnExecute()
            {
                CapturedLeft = Left.Value;
                CapturedRight = Right.Value;
            }

            protected override bool OnSelectNextNode(out string nextNodeGUID)
            {
                nextNodeGUID = null;
                return false;
            }
        }

        [Test]
        public void InitializedGraph_ConnectsPortsAndComputesSharedDependencyOncePerPull()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            try
            {
                var source = new CountingValueNode { NextValue = 42 };
                var sink = new CapturePairNode();
                graph.AddNode(source);
                graph.AddNode(sink);
                graph.AddParameterEdge(source, "Value", sink, "Left");
                graph.AddParameterEdge(source, "Value", sink, "Right");

                graph.Init();
                var firstResult = sink.Execute();

                Assert.That(firstResult, Is.EqualTo(ExecuteResult.Continue));
                Assert.That(sink.CapturedLeft, Is.EqualTo(42));
                Assert.That(sink.CapturedRight, Is.EqualTo(42));
                Assert.That(source.ComputeCount, Is.EqualTo(1));

                source.NextValue = 84;
                sink.Execute();

                Assert.That(sink.CapturedLeft, Is.EqualTo(84));
                Assert.That(sink.CapturedRight, Is.EqualTo(84));
                Assert.That(source.ComputeCount, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void GraphInit_LoadsSerializedDefaultsIntoRuntimeVariableStore()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            try
            {
                var score = new GraphVariable("Score", VariableType.Int)
                {
                    GUID = "score-guid",
                    IntValue = 5
                };
                graph.AddVariable(score);

                graph.Init();

                Assert.That(graph.TryGetVariableInt(score.GUID, out var initialValue), Is.True);
                Assert.That(initialValue, Is.EqualTo(5));

                graph.SetVariableInt(score.GUID, 12);
                Assert.That(graph.TryGetVariableInt(score.GUID, out var runtimeValue), Is.True);
                Assert.That(runtimeValue, Is.EqualTo(12));

                graph.Init();

                Assert.That(graph.TryGetVariableInt(score.GUID, out var resetValue), Is.True);
                Assert.That(resetValue, Is.EqualTo(5));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void GraphAsset_CopyAndReload_PreservesPolymorphicNodesAndConnections()
        {
            const string tempFolder = "Assets/__ShizukuTests";
            const string sourcePath = tempFolder + "/GraphSource.asset";
            const string copyPath = tempFolder + "/GraphCopy.asset";

            if (AssetDatabase.IsValidFolder(tempFolder))
                AssetDatabase.DeleteAsset(tempFolder);
            AssetDatabase.CreateFolder("Assets", "__ShizukuTests");

            try
            {
                var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
                var source = new CountingValueNode { NextValue = 37 };
                var sink = new CapturePairNode();
                graph.AddNode(source);
                graph.AddNode(sink);
                graph.AddParameterEdge(source, "Value", sink, "Left");
                graph.AddParameterEdge(source, "Value", sink, "Right");

                AssetDatabase.CreateAsset(graph, sourcePath);
                AssetDatabase.SaveAssets();
                Assert.That(AssetDatabase.CopyAsset(sourcePath, copyPath), Is.True);
                AssetDatabase.ImportAsset(copyPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(copyPath);

                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded, Is.Not.SameAs(graph));
                Assert.That(loaded.Nodes, Has.Count.EqualTo(2));
                Assert.That(loaded.Edges, Has.Count.EqualTo(2));
                Assert.That(loaded.Nodes[0], Is.TypeOf<CountingValueNode>());
                Assert.That(loaded.Nodes[1], Is.TypeOf<CapturePairNode>());

                loaded.Init();
                var loadedSource = (CountingValueNode)loaded.Nodes[0];
                var loadedSink = (CapturePairNode)loaded.Nodes[1];
                loadedSink.Execute();

                Assert.That(loadedSink.CapturedLeft, Is.EqualTo(37));
                Assert.That(loadedSink.CapturedRight, Is.EqualTo(37));
                Assert.That(loadedSource.ComputeCount, Is.EqualTo(1));
            }
            finally
            {
                AssetDatabase.DeleteAsset(tempFolder);
            }
        }
    }
}
