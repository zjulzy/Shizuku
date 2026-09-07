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
                    "Graph 每帧再次执行节点时不能抢占或创建第二个 Director");

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

        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(target) as T;
        }
    }
}
