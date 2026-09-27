using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Shizuku.Graph;
using UnityEngine;
using UnityEngine.TestTools;

namespace Shizuku.Tests.PlayMode
{
    public abstract class BlueprintLifecycleIntermediate<T> : BlueprintBehavior<T> where T : BlueprintBehavior<T>
    {
        protected override void Start() { base.Start(); }
        protected override void OnDestroy() { base.OnDestroy(); }
        public void RepeatStartForTest() { base.Start(); }
        public void DestroyForTest() { base.OnDestroy(); }
    }

    public class BlueprintLifecycleTestBehavior : BlueprintLifecycleIntermediate<BlueprintLifecycleTestBehavior>
    {
        public void TriggerContextProbe()
        {
            TryExecuteBlueprintOverride(nameof(TriggerContextProbe));
        }
    }

    [Serializable]
    public sealed class BlueprintLifecycleProbeNode : ShizukuNodeBase
    {
        public int DisposeCount;

        public override void DisposeRuntime()
        {
            DisposeCount++;
            base.DisposeRuntime();
        }
    }

    [Serializable]
    public sealed class BlueprintExecutionContextProbeNode : ShizukuRunnableNode
    {
        public ShizukuGraphBase CapturedGraph;
        public int ExecuteCount;
        public bool InitializedAndBound;
        [NonSerialized] public int DisposeCount;

        protected override void OnExecute()
        {
            CapturedGraph = ShizukuExecutionContext.Current?.GraphAsset;
            ExecuteCount++;
            var graph = (BlueprintLifecycleTestGraph)RootGraph;
            InitializedAndBound = graph.InitializeBehaviorCount == 1 &&
                graph.RuntimeOwner.GetComponent<BlueprintLifecycleTestBehavior>().Blueprint == graph;
        }

        public override void DisposeRuntime() { DisposeCount++; base.DisposeRuntime(); }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = null;
            return false;
        }
    }

    [Serializable]
    public sealed class BlueprintLifecycleDelayNode : ShizukuLatentNode
    {
        public ChainPort Completed = new() { Name = "Completed" };
        public int TickCount;
        public int LastTickFrame = -1;
        public bool DuplicateTick;
        protected override ChainPort CompletedPort => Completed;
        protected override bool OnLatentStart() => true;
        protected override ShizukuLatentTickResult OnLatentTick()
        {
            DuplicateTick |= LastTickFrame == Time.frameCount;
            LastTickFrame = Time.frameCount;
            return ++TickCount >= 3 ? ShizukuLatentTickResult.Completed : ShizukuLatentTickResult.Running;
        }
    }

    public sealed class BlueprintBehaviorLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator Lifecycle_StartOnceAfterBinding_UpdateAndTickOnce_DestroyBeforeDispose()
        {
            var source = ScriptableObject.CreateInstance<BlueprintLifecycleTestGraph>();
            var host = new GameObject("LifecycleEventsHost");
            try
            {
                var start = new BlueprintEventNode { EventName = "OnStart" };
                var destroy = new BlueprintEventNode { EventName = "OnDestroy" };
                var update = new BlueprintEventNode { EventName = "OnUpdate" };
                update.EventParameters.Add(new EventParameter { Name = "deltaTime", TypeName = nameof(Single),
                    OutputPort = new FloatParameterEdgePort { Name = "deltaTime", IsOut = true } });
                var startProbe = new BlueprintExecutionContextProbeNode();
                var destroyProbe = new BlueprintExecutionContextProbeNode();
                var updateProbe = new BlueprintExecutionContextProbeNode();
                var root = new ShizukuRootNode();
                var rootProbe = new BlueprintExecutionContextProbeNode();
                foreach (var node in new ShizukuNodeBase[] { start, destroy, update, startProbe, destroyProbe, updateProbe, root, rootProbe })
                    source.AddNode(node);
                source.RootNodeGUID = root.GUID;
                source.Init();
                start.ChainPorts["next"].NextNodeGuid = startProbe.GUID;
                destroy.ChainPorts["next"].NextNodeGuid = destroyProbe.GUID;
                update.ChainPorts["next"].NextNodeGuid = updateProbe.GUID;
                root.ChainPorts["next"].NextNodeGuid = rootProbe.GUID;
                source.DisposeRuntime();
                var behavior = host.AddComponent<BlueprintLifecycleTestBehavior>();
                SetSourceBlueprint(behavior, source);
                yield return null;
                var runtime = (BlueprintLifecycleTestGraph)behavior.Blueprint;
                var started = (BlueprintExecutionContextProbeNode)runtime.Guid2NodeMap[startProbe.GUID];
                var destroyed = (BlueprintExecutionContextProbeNode)runtime.Guid2NodeMap[destroyProbe.GUID];
                var rooted = (BlueprintExecutionContextProbeNode)runtime.Guid2NodeMap[rootProbe.GUID];
                var updated = (BlueprintExecutionContextProbeNode)runtime.Guid2NodeMap[updateProbe.GUID];
                var updateEvent = (BlueprintEventNode)runtime.Guid2NodeMap[update.GUID];
                Assert.That(started.ExecuteCount, Is.EqualTo(1));
                Assert.That(started.InitializedAndBound, Is.True);
                behavior.RepeatStartForTest();
                Assert.That(behavior.Blueprint, Is.SameAs(runtime));
                Assert.That(started.ExecuteCount, Is.EqualTo(1));
                var updates = 0;
                var lastFrame = -1;
                var rootCount = rooted.ExecuteCount;
                var updateCount = updated.ExecuteCount;
                behavior.RegisterBlueprintEvent("OnUpdate", args =>
                {
                    Assert.That(args.Length, Is.EqualTo(1));
                    Assert.That((float)args[0], Is.EqualTo(Time.deltaTime));
                    Assert.That(lastFrame, Is.Not.EqualTo(Time.frameCount));
                    lastFrame = Time.frameCount;
                    updates++;
                    updateEvent.TriggerEventWithReturn(args);
                    Assert.That(updateEvent.EventParameters[0].Value, Is.EqualTo(Time.deltaTime));
                    return null;
                });
                yield return null;
                yield return null;
                Assert.That(updates, Is.GreaterThan(0));
                Assert.That(rooted.ExecuteCount - rootCount, Is.EqualTo(updates));
                Assert.That(updated.ExecuteCount - updateCount, Is.EqualTo(updates));
                UnityEngine.Object.Destroy(host);
                yield return null;
                Assert.That(destroyed.ExecuteCount, Is.EqualTo(1));
                Assert.That(destroyed.InitializedAndBound, Is.True);
                Assert.That(destroyed.DisposeCount, Is.EqualTo(1));
                Assert.That(runtime == null, Is.True);
            }
            finally
            {
                if (host != null) UnityEngine.Object.Destroy(host);
                UnityEngine.Object.Destroy(source);
            }
        }

        [UnityTest]
        public IEnumerator OnStart_LatentChainResumesWithoutDuplicateTicks()
        {
            var source = ScriptableObject.CreateInstance<BlueprintLifecycleTestGraph>();
            var host = new GameObject("LifecycleDelayHost");
            try
            {
                var start = new BlueprintEventNode { EventName = "OnStart" };
                var delay = new BlueprintLifecycleDelayNode();
                var completed = new BlueprintExecutionContextProbeNode();
                source.AddNode(start); source.AddNode(delay); source.AddNode(completed);
                source.Init();
                start.ChainPorts["next"].NextNodeGuid = delay.GUID;
                delay.ChainPorts["Completed"].NextNodeGuid = completed.GUID;
                source.DisposeRuntime();
                var behavior = host.AddComponent<BlueprintLifecycleTestBehavior>();
                SetSourceBlueprint(behavior, source);
                yield return null;
                var runtime = behavior.Blueprint;
                var runtimeDelay = (BlueprintLifecycleDelayNode)runtime.Guid2NodeMap[delay.GUID];
                var probe = (BlueprintExecutionContextProbeNode)runtime.Guid2NodeMap[completed.GUID];
                for (var i = 0; i < 8 && probe.ExecuteCount == 0; i++) yield return null;
                Assert.That(probe.ExecuteCount, Is.EqualTo(1));
                Assert.That(runtimeDelay.TickCount, Is.EqualTo(3));
                Assert.That(runtimeDelay.DuplicateTick, Is.False);
                Assert.That(runtime.ActiveLatentNodeCount, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.Destroy(host);
                UnityEngine.Object.Destroy(source);
            }
        }

        [UnityTest]
        public IEnumerator DestroyException_StillClearsBindingsAndDisposesOnce()
        {
            var source = ScriptableObject.CreateInstance<BlueprintLifecycleTestGraph>();
            var host = new GameObject("LifecycleDestroyExceptionHost");
            try
            {
                source.AddNode(new BlueprintLifecycleProbeNode());
                var behavior = host.AddComponent<BlueprintLifecycleTestBehavior>();
                SetSourceBlueprint(behavior, source);
                yield return null;
                var runtime = behavior.Blueprint;
                var probe = (BlueprintLifecycleProbeNode)runtime.Nodes[0];
                var calls = 0;
                behavior.RegisterPropertyGetter("probe", () => 42);
                behavior.RegisterBlueprintEvent("OnDestroy", args =>
                {
                    calls++;
                    Assert.That(behavior.Blueprint, Is.SameAs(runtime));
                    Assert.That(probe.DisposeCount, Is.Zero);
                    Assert.That(behavior.TryGetBlueprintProperty("probe", out _), Is.True);
                    // Reentrant base.OnDestroy must not invoke this event again.
                    behavior.DestroyForTest();
                    throw new InvalidOperationException("destroy-test");
                });
                Assert.Throws<InvalidOperationException>(() => behavior.DestroyForTest());
                Assert.That(probe.DisposeCount, Is.EqualTo(1));
                Assert.That(behavior.TryGetBlueprintProperty("probe", out _), Is.False);
                behavior.DestroyForTest();
                Assert.That(calls, Is.EqualTo(1));
                UnityEngine.Object.Destroy(host);
                yield return null;
                Assert.That(runtime == null, Is.True);
            }
            finally
            {
                if (host != null) UnityEngine.Object.Destroy(host);
                UnityEngine.Object.Destroy(source);
            }
        }

        [UnityTest]
        public IEnumerator NoBlueprint_DefaultLifecycleIsNoOp()
        {
            var host = new GameObject("LifecycleNoBlueprintHost");
            var behavior = host.AddComponent<BlueprintLifecycleTestBehavior>();
            yield return null;
            Assert.That(behavior.Blueprint, Is.Null);
            behavior.RepeatStartForTest();
            behavior.DestroyForTest();
            UnityEngine.Object.Destroy(host);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BlueprintEvent_UsesRuntimeCloneInExecutionContext()
        {
            var source = ScriptableObject.CreateInstance<BlueprintLifecycleTestGraph>();
            var host = new GameObject("BlueprintContextHost");
            var previousContextState = ShizukuExecutionContext.Enabled;

            try
            {
                ShizukuExecutionContext.Enabled = true;
                var eventNode = new BlueprintEventNode
                {
                    EventName = nameof(BlueprintLifecycleTestBehavior.TriggerContextProbe)
                };
                var probe = new BlueprintExecutionContextProbeNode();
                source.AddNode(eventNode);
                source.AddNode(probe);
                source.Init();
                eventNode.ChainPorts["next"].NextNodeGuid = probe.GUID;
                source.DisposeRuntime();

                var behavior = host.AddComponent<BlueprintLifecycleTestBehavior>();
                SetSourceBlueprint(behavior, source);
                yield return null;

                var runtime = (BlueprintLifecycleTestGraph)behavior.Blueprint;
                var runtimeProbe = (BlueprintExecutionContextProbeNode)runtime.Guid2NodeMap[probe.GUID];
                behavior.TriggerContextProbe();

                Assert.That(runtime, Is.Not.SameAs(source));
                Assert.That(runtime.InitializeBehaviorCount, Is.EqualTo(1));
                Assert.That(source.InitializeBehaviorCount, Is.Zero,
                    "Blueprint custom initialization must run on the runtime clone only.");
                Assert.That(runtimeProbe.CapturedGraph, Is.SameAs(runtime));
            }
            finally
            {
                ShizukuExecutionContext.Enabled = previousContextState;
                if (host != null)
                    UnityEngine.Object.Destroy(host);
                if (source != null)
                    UnityEngine.Object.Destroy(source);
            }
        }

        [UnityTest]
        public IEnumerator TwoBehaviors_CloneSharedAssetAndDisposeRuntimeGraphsIndependently()
        {
            var source = ScriptableObject.CreateInstance<BlueprintLifecycleTestGraph>();
            var sourceProbe = new BlueprintLifecycleProbeNode();
            source.AddNode(sourceProbe);
            var firstHost = new GameObject("FirstBlueprintBehaviorHost");
            var secondHost = new GameObject("SecondBlueprintBehaviorHost");

            try
            {
                var firstBehavior = firstHost.AddComponent<BlueprintLifecycleTestBehavior>();
                var secondBehavior = secondHost.AddComponent<BlueprintLifecycleTestBehavior>();
                SetSourceBlueprint(firstBehavior, source);
                SetSourceBlueprint(secondBehavior, source);

                yield return null;

                var firstRuntime = firstBehavior.Blueprint;
                var secondRuntime = secondBehavior.Blueprint;
                Assert.That(firstRuntime, Is.Not.Null);
                Assert.That(secondRuntime, Is.Not.Null);
                Assert.That(firstRuntime, Is.Not.SameAs(source));
                Assert.That(secondRuntime, Is.Not.SameAs(source));
                Assert.That(firstRuntime, Is.Not.SameAs(secondRuntime));
                Assert.That(firstRuntime.RuntimeOwner, Is.SameAs(firstHost));
                Assert.That(secondRuntime.RuntimeOwner, Is.SameAs(secondHost));
                Assert.That(firstRuntime.Nodes[0], Is.Not.SameAs(sourceProbe));
                Assert.That(secondRuntime.Nodes[0], Is.Not.SameAs(sourceProbe));
                Assert.That(firstRuntime.Nodes[0], Is.Not.SameAs(secondRuntime.Nodes[0]));

                var firstRuntimeProbe = (BlueprintLifecycleProbeNode)firstRuntime.Nodes[0];
                var secondRuntimeProbe = (BlueprintLifecycleProbeNode)secondRuntime.Nodes[0];

                UnityEngine.Object.Destroy(firstHost);
                yield return null;

                Assert.That(firstRuntime == null, Is.True);
                Assert.That(firstRuntimeProbe.DisposeCount, Is.EqualTo(1));
                Assert.That(secondRuntime == null, Is.False);
                Assert.That(secondRuntimeProbe.DisposeCount, Is.EqualTo(0));
                Assert.That(source == null, Is.False);
                Assert.That(sourceProbe.DisposeCount, Is.EqualTo(0));

                UnityEngine.Object.Destroy(secondHost);
                yield return null;

                Assert.That(secondRuntime == null, Is.True);
                Assert.That(secondRuntimeProbe.DisposeCount, Is.EqualTo(1));
                Assert.That(source == null, Is.False);
                Assert.That(sourceProbe.DisposeCount, Is.EqualTo(0));
            }
            finally
            {
                if (firstHost != null)
                    UnityEngine.Object.Destroy(firstHost);
                if (secondHost != null)
                    UnityEngine.Object.Destroy(secondHost);
                if (source != null)
                {
                    source.DisposeRuntime();
                    UnityEngine.Object.Destroy(source);
                }
            }
        }

        private static void SetSourceBlueprint(
            BlueprintLifecycleTestBehavior behavior,
            BlueprintLifecycleTestGraph source)
        {
            var field = typeof(BlueprintBehavior<BlueprintLifecycleTestBehavior>).GetField(
                "_blueprint",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(behavior, source);
        }
    }
}
