using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.Graph.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier2")]
    public sealed class TimelineNodeTests
    {
        [Serializable]
        private sealed class DisposableProbeNode : ShizukuNodeBase
        {
            public int DisposeCount;

            public override void DisposeRuntime()
            {
                DisposeCount++;
                base.DisposeRuntime();
            }
        }

        [Test]
        public void SyncBindingPorts_PreservesRenamedTrackEdgeAndRemovesDeletedTrackEdge()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            try
            {
                var animationTrack = timeline.CreateTrack<AnimationTrack>(null, "Actor");
                var audioTrack = timeline.CreateTrack<AudioTrack>(null, "Voice");
                var source = new FindGameObjectNode();
                var node = new PlayTimelineNode { Timeline = timeline };
                graph.AddNode(source);
                graph.AddNode(node);

                Assert.That(node.SyncBindingPorts(graph), Is.True);
                Assert.That(node.BindingPorts, Has.Count.EqualTo(2));

                var animationPort = node.BindingPorts.Single(port => port.Track == animationTrack);
                var audioPort = node.BindingPorts.Single(port => port.Track == audioTrack);
                Assert.That(animationPort.Port.Name, Does.Contain("Actor"));
                Assert.That(animationPort.Port.Name, Does.Contain(nameof(Animator)));
                Assert.That(audioPort.Port.Name, Does.Contain("Voice"));
                Assert.That(audioPort.Port.Name, Does.Contain(nameof(AudioSource)));

                graph.AddParameterEdge(source, "result", node, animationPort.Port.Name);
                graph.AddParameterEdge(source, "result", node, audioPort.Port.Name);

                var retainedPort = animationPort.Port;
                animationTrack.name = "Hero";
                Assert.That(node.SyncBindingPorts(graph), Is.True);

                animationPort = node.BindingPorts.Single(port => port.Track == animationTrack);
                Assert.That(animationPort.Port, Is.SameAs(retainedPort));
                Assert.That(animationPort.Port.Name, Does.Contain("Hero"));
                Assert.That(graph.Edges.Single(edge => edge.InputPortName.Contains("Hero")).InputPortName,
                    Is.EqualTo(animationPort.Port.Name));

                timeline.DeleteTrack(audioTrack);
                Assert.That(node.SyncBindingPorts(graph), Is.True);
                Assert.That(node.BindingPorts, Has.Count.EqualTo(1));
                Assert.That(graph.Edges, Has.Count.EqualTo(1));
                Assert.That(graph.Edges[0].InputPortName, Is.EqualTo(animationPort.Port.Name));
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void Execute_BindsRequiredComponentAndRejectsReentryWhilePlaying()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var animationClip = new AnimationClip { name = "TimelineNodeTestClip" };
            var host = new GameObject("TimelineNodeTestHost");
            var target = new GameObject("TimelineNodeTestTarget");

            try
            {
                var animator = target.AddComponent<Animator>();
                var animationTrack = timeline.CreateTrack<AnimationTrack>(null, "Actor");
                var timelineClip = animationTrack.CreateClip<AnimationPlayableAsset>();
                ((AnimationPlayableAsset)timelineClip.asset).clip = animationClip;
                timelineClip.duration = 10d;

                var node = new PlayTimelineNode { Timeline = timeline };
                graph.AddNode(node);
                graph.Init(host);

                node.BindingPorts.Single().Port.DefaultValue = target;
                node.Execute();

                var director = GetRuntimeDirector(node);
                Assert.That(director, Is.Not.Null);
                Assert.That(director.transform.parent, Is.EqualTo(host.transform));
                Assert.That(director.playableAsset, Is.SameAs(timeline));
                Assert.That(director.GetGenericBinding(animationTrack), Is.SameAs(animator));
                Assert.That(node.IsPlaying, Is.True);

                director.time = 1.25d;
                LogAssert.Expect(LogType.Error, new Regex("Latent 节点不允许重入"));
                node.Execute();

                Assert.That(GetRuntimeDirector(node), Is.SameAs(director));
                Assert.That(director.time, Is.EqualTo(1.25d).Within(0.001d),
                    "播放中的重入必须报错，但不能抢占原播放或把时间重置到 0");

                var directorObject = director.gameObject;
                graph.DisposeRuntime();
                Assert.That(directorObject == null, Is.True,
                    "释放运行时图时必须同步销毁节点持有的 Director GameObject");
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(timeline);
                UnityEngine.Object.DestroyImmediate(animationClip);
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Execute_WithMissingBinding_AbortsBeforeCreatingDirector()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            try
            {
                timeline.CreateTrack<AnimationTrack>(null, "Actor");
                var node = new PlayTimelineNode { Timeline = timeline };
                var failed = new LatentControlFlowProbeNode();
                graph.AddNode(node);
                graph.AddNode(failed);
                graph.Init();
                node.ChainPorts["Failed"].NextNodeGuid = failed.GUID;

                LogAssert.Expect(LogType.Error, new Regex("轨道 'Actor' 未绑定 GameObject"));
                node.Execute();

                Assert.That(GetRuntimeDirector(node), Is.Null);
                Assert.That(node.IsLatentActive, Is.False);
                Assert.That(graph.ActiveLatentNodeCount, Is.Zero);
                Assert.That(failed.ExecuteCount, Is.EqualTo(1));
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void Init_ExposesStartedCompletedAndFailedControlPorts()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();

            try
            {
                var node = new PlayTimelineNode();
                graph.AddNode(node);
                graph.Init();

                Assert.That(
                    node.ChainPorts.Keys,
                    Is.EquivalentTo(new[] { "Started", "Completed", "Failed" }));
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void DisposeRuntime_ReleasesNodesInMainGraphAndMethodGraphsExactlyOnce()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();

            try
            {
                var mainProbe = new DisposableProbeNode();
                var methodProbe = new DisposableProbeNode();
                var method = new ShizukuMethod("Lifecycle");
                method.AddNode(methodProbe);
                graph.AddNode(mainProbe);
                graph.Methods.Add(method);

                graph.Init();
                graph.DisposeRuntime();
                graph.DisposeRuntime();

                Assert.That(mainProbe.DisposeCount, Is.EqualTo(1));
                Assert.That(methodProbe.DisposeCount, Is.EqualTo(1));
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void GraphRunner_OwnsCloneWithoutReplacingOrDestroyingSourceAsset()
        {
            var sourceGraph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var sourceProbe = new DisposableProbeNode();
            sourceGraph.AddNode(sourceProbe);
            var host = new GameObject("GraphRunnerLifecycleTest");

            try
            {
                var runner = host.AddComponent<GraphRunner>();
                runner.GraphAsset = sourceGraph;
                InvokePrivate(runner, "Start");

                var runtimeGraph = GetRuntimeGraph(runner);
                Assert.That(runner.GraphAsset, Is.SameAs(sourceGraph));
                Assert.That(runtimeGraph, Is.Not.Null);
                Assert.That(runtimeGraph, Is.Not.SameAs(sourceGraph));
                Assert.That(runtimeGraph.RuntimeOwner, Is.SameAs(host));

                var runtimeProbe = (DisposableProbeNode)runtimeGraph.Nodes.Single();
                // EditMode 中 DestroyImmediate 不保证模拟完整的 Player 生命周期，
                // 这里显式调用组件的销毁入口；真实销毁路径另由 Play Mode 验证。
                InvokePrivate(runner, "OnDestroy");

                Assert.That(runtimeProbe.DisposeCount, Is.EqualTo(1));
                Assert.That(sourceProbe.DisposeCount, Is.EqualTo(0));
                Assert.That(sourceGraph == null, Is.False,
                    "GraphRunner 销毁时不能销毁 Inspector 中引用的源 Graph 资产");
            }
            finally
            {
                if (host != null)
                    UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(sourceGraph);
            }
        }

        [Test]
        public void TimelineBindingPortsAndEdges_SurviveAssetCopyAndReload()
        {
            const string folder = "Assets/__ShizukuTimelineNodeTests";
            const string timelinePath = folder + "/Timeline.playable";
            const string graphPath = folder + "/Graph.asset";
            const string graphCopyPath = folder + "/GraphCopy.asset";

            if (AssetDatabase.IsValidFolder(folder))
                AssetDatabase.DeleteAsset(folder);
            AssetDatabase.CreateFolder("Assets", "__ShizukuTimelineNodeTests");

            ShizukuGraphBase graph = null;
            ShizukuGraphBase loaded = null;
            try
            {
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(timeline, timelinePath);
                var animationTrack = timeline.CreateTrack<AnimationTrack>(null, "Actor");

                graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
                var source = new FindGameObjectNode();
                var node = new PlayTimelineNode { Timeline = timeline };
                graph.AddNode(source);
                graph.AddNode(node);
                node.SyncBindingPorts(graph);
                graph.AddParameterEdge(source, "result", node, node.BindingPorts.Single().Port.Name);
                AssetDatabase.CreateAsset(graph, graphPath);

                EditorUtility.SetDirty(timeline);
                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();

                Assert.That(AssetDatabase.CopyAsset(graphPath, graphCopyPath), Is.True);
                AssetDatabase.ImportAsset(graphCopyPath, ImportAssetOptions.ForceSynchronousImport);
                loaded = AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(graphCopyPath);
                loaded.Init();

                var loadedNode = loaded.Nodes.OfType<PlayTimelineNode>().Single();
                Assert.That(loadedNode.Timeline, Is.SameAs(timeline));
                Assert.That(loadedNode.BindingPorts, Has.Count.EqualTo(1));
                Assert.That(loadedNode.BindingPorts[0].Track, Is.SameAs(animationTrack));
                Assert.That(loaded.Edges, Has.Count.EqualTo(1));
                Assert.That(loaded.Edges[0].InputPortName,
                    Is.EqualTo(loadedNode.BindingPorts[0].Port.Name));
            }
            finally
            {
                loaded?.DisposeRuntime();
                graph?.DisposeRuntime();
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void GraphView_RendersTimelineBindingPortsBeforeAndAfterRefresh()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            try
            {
                timeline.CreateTrack<AnimationTrack>(null, "Actor");
                timeline.CreateTrack<AudioTrack>(null, "Voice");
                var node = new PlayTimelineNode { Timeline = timeline };
                graph.AddNode(node);

                var graphView = new ShizukuGraphView();
                graphView.LoadFromAsset(graph);
                AssertTimelinePorts(graphView, node, 2);

                graphView.RefreshCurrentView();
                AssertTimelinePorts(graphView, node, 2);
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void DebuggerStop_DestroysCurrentGraphSnapshotClone()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();

            try
            {
                graph.Init();
                var snapshot = graph.CaptureSnapshot("test-node");
                var snapshotClone = snapshot.GraphClone;
                var currentSnapshotField = typeof(ShizukuDebugger).GetField(
                    "_currentSnapshot",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(currentSnapshotField, Is.Not.Null);
                currentSnapshotField.SetValue(null, snapshot);

                ShizukuDebugger.Stop();

                Assert.That(snapshotClone == null, Is.True,
                    "停止调试时必须销毁断点快照持有的 Graph 克隆");
            }
            finally
            {
                ShizukuDebugger.Stop();
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        private static PlayableDirector GetRuntimeDirector(PlayTimelineNode node)
        {
            var field = typeof(PlayTimelineNode).GetField(
                "_director",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(node) as PlayableDirector;
        }

        private static ShizukuGraphBase GetRuntimeGraph(GraphRunner runner)
        {
            var field = typeof(GraphRunner).GetField(
                "_runtimeGraph",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(runner) as ShizukuGraphBase;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }

        private static void AssertTimelinePorts(
            ShizukuGraphView graphView,
            PlayTimelineNode node,
            int expectedCount)
        {
            var nodeView = graphView.nodes.OfType<ShizukuNodeView>()
                .Single(view => ReferenceEquals(view.RuntimeNode, node));
            var inputPortNames = nodeView.inputContainer.Children()
                .OfType<Port>()
                .Select(port => port.portName)
                .ToArray();

            Assert.That(inputPortNames, Has.Length.EqualTo(expectedCount));
            Assert.That(inputPortNames, Is.EquivalentTo(node.BindingPorts.Select(port => port.Port.Name)));
        }
    }
}
