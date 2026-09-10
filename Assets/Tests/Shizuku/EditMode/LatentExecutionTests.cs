using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Shizuku.Graph;
using UnityEngine;
using UnityEngine.TestTools;

namespace Shizuku.Tests.EditMode
{
    [Serializable]
    public sealed class ManualLatentTestNode : ShizukuLatentNode
    {
        [SerializeField] public ChainPort Started = new() { Name = "Started" };
        [SerializeField] public ChainPort Completed = new() { Name = "Completed" };
        [SerializeField] public ChainPort Failed = new() { Name = "Failed" };

        public bool StartSucceeds = true;
        public ShizukuLatentTickResult TickResult = ShizukuLatentTickResult.Running;
        public int StartCount;
        public int TickCount;
        public int CancelCount;

        protected override ChainPort StartedPort => Started;
        protected override ChainPort CompletedPort => Completed;
        protected override ChainPort FailedPort => Failed;

        protected override bool OnLatentStart()
        {
            StartCount++;
            return StartSucceeds;
        }

        protected override ShizukuLatentTickResult OnLatentTick()
        {
            TickCount++;
            return TickResult;
        }

        protected override void OnLatentCancel()
        {
            CancelCount++;
        }
    }

    [Serializable]
    public sealed class LatentControlFlowProbeNode : ShizukuRunnableNode
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

    [Category("Tier2")]
    public sealed class LatentExecutionTests
    {
        [Test]
        public void RootExecution_WaitsForLatentCompletionBeforeRestarting()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();

            try
            {
                var root = new ShizukuRootNode();
                var latent = new ManualLatentTestNode();
                var started = new LatentControlFlowProbeNode();
                var completed = new LatentControlFlowProbeNode();
                graph.AddNode(root);
                graph.AddNode(latent);
                graph.AddNode(started);
                graph.AddNode(completed);
                graph.RootNodeGUID = root.GUID;
                graph.Init();

                root.ChainPorts["next"].NextNodeGuid = latent.GUID;
                latent.ChainPorts["Started"].NextNodeGuid = started.GUID;
                latent.ChainPorts["Completed"].NextNodeGuid = completed.GUID;

                graph.Update();

                Assert.That(latent.StartCount, Is.EqualTo(1));
                Assert.That(started.ExecuteCount, Is.EqualTo(1));
                Assert.That(completed.ExecuteCount, Is.Zero);
                Assert.That(graph.ActiveLatentNodeCount, Is.EqualTo(1));

                graph.Update();

                Assert.That(latent.StartCount, Is.EqualTo(1),
                    "Root must not re-enter while its latent execution is running.");
                Assert.That(latent.TickCount, Is.EqualTo(1));

                latent.TickResult = ShizukuLatentTickResult.Completed;
                graph.Update();

                Assert.That(completed.ExecuteCount, Is.EqualTo(1));
                Assert.That(graph.ActiveLatentNodeCount, Is.Zero);
                Assert.That(latent.StartCount, Is.EqualTo(1),
                    "Root must not restart in the same frame that the latent node completes.");

                graph.Update();

                Assert.That(latent.StartCount, Is.EqualTo(2));
                Assert.That(started.ExecuteCount, Is.EqualTo(2));
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void ActiveLatentNode_ReentryLogsErrorWithoutRestarting()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();

            try
            {
                var latent = new ManualLatentTestNode();
                graph.AddNode(latent);
                graph.Init();

                latent.Execute();
                LogAssert.Expect(LogType.Error, new Regex("Latent 节点不允许重入"));
                latent.Execute();

                Assert.That(latent.StartCount, Is.EqualTo(1));
                Assert.That(graph.ActiveLatentNodeCount, Is.EqualTo(1));
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void DisposeRuntime_CancelsLatentWithoutExecutingTerminalBranches()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();

            try
            {
                var latent = new ManualLatentTestNode();
                var completed = new LatentControlFlowProbeNode();
                var failed = new LatentControlFlowProbeNode();
                graph.AddNode(latent);
                graph.AddNode(completed);
                graph.AddNode(failed);
                graph.Init();
                latent.ChainPorts["Completed"].NextNodeGuid = completed.GUID;
                latent.ChainPorts["Failed"].NextNodeGuid = failed.GUID;

                latent.Execute();
                graph.DisposeRuntime();

                Assert.That(latent.CancelCount, Is.EqualTo(1));
                Assert.That(latent.IsLatentActive, Is.False);
                Assert.That(completed.ExecuteCount, Is.Zero);
                Assert.That(failed.ExecuteCount, Is.Zero);
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void ShizukuMethod_RejectsLatentNodeBeforeStartingIt()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();

            try
            {
                var method = new ShizukuMethod("LatentMethod");
                var entry = new MethodEntryNode { MethodGUID = method.GUID };
                var latent = new ManualLatentTestNode();
                method.AddNode(entry);
                method.AddNode(latent);
                method.EntryNodeGUID = entry.GUID;
                graph.Methods.Add(method);

                var invoke = new InvokeMethodNode
                {
                    TargetMethodGUID = method.GUID,
                    TargetMethodName = method.Name
                };
                graph.AddNode(invoke);
                graph.Init();
                entry.ChainPorts["next"].NextNodeGuid = latent.GUID;

                LogAssert.Expect(LogType.Error, new Regex("ShizukuMethod 'LatentMethod' 暂不支持 Latent 节点"));
                invoke.Execute();

                Assert.That(latent.StartCount, Is.Zero);
                Assert.That(graph.ActiveLatentNodeCount, Is.Zero);
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void ReturnValueBlueprintEvent_RejectsLatentNodeBeforeStartingIt()
        {
            var graph = ScriptableObject.CreateInstance<BlueprintBehaviorTestGraph>();
            var host = new GameObject("ReturnValueLatentRestrictionHost");

            try
            {
                var behavior = host.AddComponent<BlueprintBehaviorTestHost>();
                var eventNode = new BlueprintEventNode
                {
                    EventName = nameof(BlueprintBehaviorTestHost.Evaluate),
                    ReturnNodeGUID = "return-node"
                };
                eventNode.EventParameters.Add(new EventParameter
                {
                    Name = "value",
                    TypeName = nameof(Int32),
                    OutputPort = new IntParameterEdgePort { IsOut = true, Name = "value" }
                });
                var latent = new ManualLatentTestNode();
                graph.AddNode(eventNode);
                graph.AddNode(latent);
                graph.InitializeBehavior(behavior);
                eventNode.ChainPorts["next"].NextNodeGuid = latent.GUID;

                LogAssert.Expect(LogType.Error, new Regex("带返回值的 Blueprint Event 'Evaluate' 不支持 Latent 节点"));
                var result = behavior.Evaluate(12);

                Assert.That(result, Is.Zero);
                Assert.That(latent.StartCount, Is.Zero);
                Assert.That(graph.ActiveLatentNodeCount, Is.Zero);
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void VoidBlueprintEvent_AllowsLatentNode()
        {
            var graph = ScriptableObject.CreateInstance<BlueprintBehaviorTestGraph>();
            var host = new GameObject("VoidLatentEventHost");

            try
            {
                var behavior = host.AddComponent<BlueprintBehaviorTestHost>();
                var eventNode = new BlueprintEventNode
                {
                    EventName = nameof(BlueprintBehaviorTestHost.HandleNullable)
                };
                eventNode.EventParameters.Add(new EventParameter
                {
                    Name = "value",
                    TypeName = nameof(String),
                    OutputPort = new StringParameterEdgePort { IsOut = true, Name = "value" }
                });
                var latent = new ManualLatentTestNode();
                graph.AddNode(eventNode);
                graph.AddNode(latent);
                graph.InitializeBehavior(behavior);
                eventNode.ChainPorts["next"].NextNodeGuid = latent.GUID;

                behavior.HandleNullable("start");

                Assert.That(latent.StartCount, Is.EqualTo(1));
                Assert.That(latent.IsLatentActive, Is.True);
                Assert.That(graph.ActiveLatentNodeCount, Is.EqualTo(1));

                LogAssert.Expect(LogType.Error, new Regex("重入已拒绝"));
                behavior.HandleNullable("must-not-overwrite");

                Assert.That(latent.StartCount, Is.EqualTo(1));
                Assert.That(eventNode.EventParameters[0].Value, Is.EqualTo("start"),
                    "Rejected event reentry must not overwrite parameters used by the pending continuation.");
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void VoidBlueprintEvent_ReentryFindsActiveLatentBeyondInactiveLatent()
        {
            var graph = ScriptableObject.CreateInstance<BlueprintBehaviorTestGraph>();
            var host = new GameObject("NestedLatentEventHost");

            try
            {
                var behavior = host.AddComponent<BlueprintBehaviorTestHost>();
                var eventNode = new BlueprintEventNode
                {
                    EventName = nameof(BlueprintBehaviorTestHost.HandleNullable)
                };
                eventNode.EventParameters.Add(new EventParameter
                {
                    Name = "value",
                    TypeName = nameof(String),
                    Value = "original",
                    OutputPort = new StringParameterEdgePort { IsOut = true, Name = "value" }
                });
                var inactiveLatent = new ManualLatentTestNode();
                var activeLatent = new ManualLatentTestNode();
                graph.AddNode(eventNode);
                graph.AddNode(inactiveLatent);
                graph.AddNode(activeLatent);
                graph.InitializeBehavior(behavior);
                eventNode.ChainPorts["next"].NextNodeGuid = inactiveLatent.GUID;
                inactiveLatent.ChainPorts["Started"].NextNodeGuid = activeLatent.GUID;

                activeLatent.Execute();
                Assert.That(activeLatent.IsLatentActive, Is.True);

                LogAssert.Expect(LogType.Error, new Regex("重入已拒绝"));
                behavior.HandleNullable("must-not-overwrite");

                Assert.That(inactiveLatent.StartCount, Is.Zero);
                Assert.That(activeLatent.StartCount, Is.EqualTo(1));
                Assert.That(eventNode.EventParameters[0].Value, Is.EqualTo("original"));
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
