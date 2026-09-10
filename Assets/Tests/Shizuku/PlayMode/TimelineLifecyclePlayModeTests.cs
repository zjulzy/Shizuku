using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Shizuku.Graph;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.Timeline;

namespace Shizuku.Tests.PlayMode
{
    [System.Serializable]
    public sealed class TimelineControlFlowProbeNode : ShizukuRunnableNode
    {
        public int ExecuteCount;

        protected override void OnExecute()
        {
            ExecuteCount++;
        }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = null;
            return false;
        }
    }

    public sealed class TimelineLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator DestroyingGraphRunnerHost_ReleasesTimelineDirectorAndRuntimeGraph()
        {
            var sourceGraph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var animationClip = new AnimationClip { name = "TimelineLifecyclePlayModeClip" };
            var host = new GameObject("TimelineLifecycleGraphRunner");
            var target = new GameObject("TimelineLifecycleTarget");

            try
            {
                target.AddComponent<Animator>();
                var animationTrack = timeline.CreateTrack<AnimationTrack>(null, "Actor");
                var timelineClip = animationTrack.CreateClip<AnimationPlayableAsset>();
                ((AnimationPlayableAsset)timelineClip.asset).clip = animationClip;
                timelineClip.duration = 10d;

                var root = new ShizukuRootNode();
                var timelineNode = new PlayTimelineNode { Timeline = timeline };
                sourceGraph.AddNode(root);
                sourceGraph.AddNode(timelineNode);
                sourceGraph.RootNodeGUID = root.GUID;
                sourceGraph.Init();
                timelineNode.BindingPorts[0].Port.DefaultValue = target;
                root.ChainPorts["next"].NextNodeGuid = timelineNode.GUID;
                sourceGraph.DisposeRuntime();

                var runner = host.AddComponent<GraphRunner>();
                runner.GraphAsset = sourceGraph;

                yield return null;

                var runtimeGraph = GetPrivateField<ShizukuGraphBase>(runner, "_runtimeGraph");
                Assert.That(runtimeGraph, Is.Not.Null);
                var runtimeNode = (PlayTimelineNode)runtimeGraph.Guid2NodeMap[timelineNode.GUID];
                var director = GetPrivateField<PlayableDirector>(runtimeNode, "_director");
                Assert.That(director, Is.Not.Null);
                Assert.That(runtimeNode.IsPlaying, Is.True);
                Assert.That(director.transform.parent, Is.EqualTo(host.transform));

                var directorObject = director.gameObject;
                var firstDirector = director;
                yield return null;

                Assert.That(GetPrivateField<PlayableDirector>(runtimeNode, "_director"), Is.SameAs(firstDirector),
                    "Latent 节点等待期间必须阻止普通 Graph 从 Root 重入");

                Object.Destroy(host);
                yield return null;

                Assert.That(directorObject == null, Is.True);
                Assert.That(runtimeGraph == null, Is.True);
            }
            finally
            {
                if (host != null)
                    Object.Destroy(host);
                if (target != null)
                    Object.Destroy(target);
                if (sourceGraph != null)
                {
                    sourceGraph.DisposeRuntime();
                    Object.Destroy(sourceGraph);
                }
                if (timeline != null)
                    Object.Destroy(timeline);
                if (animationClip != null)
                    Object.Destroy(animationClip);
            }
        }

        [UnityTest]
        public IEnumerator Timeline_ExecutesStartedImmediatelyAndCompletedAfterPlayback()
        {
            var sourceGraph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var animationClip = new AnimationClip { name = "TimelineCompletionClip" };
            var host = new GameObject("TimelineCompletionRunner");
            var target = new GameObject("TimelineCompletionTarget");

            try
            {
                target.AddComponent<Animator>();
                var track = timeline.CreateTrack<AnimationTrack>(null, "Actor");
                var clip = track.CreateClip<AnimationPlayableAsset>();
                ((AnimationPlayableAsset)clip.asset).clip = animationClip;
                clip.duration = 0.05d;

                var root = new ShizukuRootNode();
                var timelineNode = new PlayTimelineNode { Timeline = timeline };
                var started = new TimelineControlFlowProbeNode();
                var completed = new TimelineControlFlowProbeNode();
                var failed = new TimelineControlFlowProbeNode();
                sourceGraph.AddNode(root);
                sourceGraph.AddNode(timelineNode);
                sourceGraph.AddNode(started);
                sourceGraph.AddNode(completed);
                sourceGraph.AddNode(failed);
                sourceGraph.RootNodeGUID = root.GUID;
                sourceGraph.Init();
                timelineNode.BindingPorts[0].Port.DefaultValue = target;
                root.ChainPorts["next"].NextNodeGuid = timelineNode.GUID;
                timelineNode.ChainPorts["Started"].NextNodeGuid = started.GUID;
                timelineNode.ChainPorts["Completed"].NextNodeGuid = completed.GUID;
                timelineNode.ChainPorts["Failed"].NextNodeGuid = failed.GUID;
                sourceGraph.DisposeRuntime();

                var runner = host.AddComponent<GraphRunner>();
                runner.GraphAsset = sourceGraph;

                yield return null;

                var runtimeGraph = GetPrivateField<ShizukuGraphBase>(runner, "_runtimeGraph");
                var runtimeTimeline = (PlayTimelineNode)runtimeGraph.Guid2NodeMap[timelineNode.GUID];
                var runtimeStarted = (TimelineControlFlowProbeNode)runtimeGraph.Guid2NodeMap[started.GUID];
                var runtimeCompleted = (TimelineControlFlowProbeNode)runtimeGraph.Guid2NodeMap[completed.GUID];
                var runtimeFailed = (TimelineControlFlowProbeNode)runtimeGraph.Guid2NodeMap[failed.GUID];

                Assert.That(runtimeStarted.ExecuteCount, Is.EqualTo(1));
                Assert.That(runtimeCompleted.ExecuteCount, Is.Zero);
                Assert.That(runtimeTimeline.IsLatentActive, Is.True);

                var timeout = Time.realtimeSinceStartup + 2f;
                while (runtimeCompleted.ExecuteCount == 0 && Time.realtimeSinceStartup < timeout)
                    yield return null;

                Assert.That(runtimeCompleted.ExecuteCount, Is.EqualTo(1));
                Assert.That(runtimeFailed.ExecuteCount, Is.Zero);
                Assert.That(runtimeTimeline.IsLatentActive, Is.False);
            }
            finally
            {
                if (host != null)
                    Object.Destroy(host);
                if (target != null)
                    Object.Destroy(target);
                if (sourceGraph != null)
                {
                    sourceGraph.DisposeRuntime();
                    Object.Destroy(sourceGraph);
                }
                if (timeline != null)
                    Object.Destroy(timeline);
                if (animationClip != null)
                    Object.Destroy(animationClip);
            }
        }

        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(target) as T;
        }
    }
}
