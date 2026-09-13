using System;
using System.Reflection;
using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.SkillEditor;
using Shizuku.SkillEditor.GraphIntegration;
using UnityEngine;

namespace Shizuku.Tests.EditMode
{
    [Serializable]
    public sealed class RuntimeExecutionContextProbeNode : ShizukuRunnableNode
    {
        public ShizukuGraphBase CapturedGraph;
        public GameObject CapturedOwner;
        public int ExecuteCount;

        protected override void OnExecute()
        {
            CapturedGraph = ShizukuExecutionContext.Current?.GraphAsset;
            CapturedOwner = ShizukuExecutionContext.Current?.Owner;
            ExecuteCount++;
        }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = null;
            return false;
        }
    }

    [Serializable]
    public sealed class RuntimeDisposeProbeNode : ShizukuNodeBase
    {
        public int DisposeCount;

        public override void DisposeRuntime()
        {
            DisposeCount++;
            base.DisposeRuntime();
        }
    }

    [Category("Tier2")]
    public sealed class ShizukuGraphRuntimeTests
    {
        [Test]
        public void CreateExecuteAndDispose_OwnsCloneAndUsesCloneExecutionContext()
        {
            var source = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var owner = new GameObject("RuntimeGraphOwner");
            var previousContextState = ShizukuExecutionContext.Enabled;
            ShizukuGraphRuntime<ShizukuGraphBase> runtime = null;

            try
            {
                ShizukuExecutionContext.Enabled = true;
                var root = new ShizukuRootNode();
                var sourceProbe = new RuntimeExecutionContextProbeNode();
                var sourceDisposeProbe = new RuntimeDisposeProbeNode();
                source.AddNode(root);
                source.AddNode(sourceProbe);
                source.RootNodeGUID = root.GUID;
                source.Init();
                root.ChainPorts["next"].NextNodeGuid = sourceProbe.GUID;
                source.DisposeRuntime();
                source.AddNode(sourceDisposeProbe);

                runtime = ShizukuGraphRuntime<ShizukuGraphBase>.Create(source, owner);
                var runtimeGraph = runtime.Instance;
                var runtimeProbe = (RuntimeExecutionContextProbeNode)runtimeGraph.Guid2NodeMap[sourceProbe.GUID];
                var runtimeDisposeProbe = (RuntimeDisposeProbeNode)runtimeGraph.Guid2NodeMap[sourceDisposeProbe.GUID];

                runtime.ExecuteRootOnce();

                Assert.That(runtime.SourceAsset, Is.SameAs(source));
                Assert.That(runtimeGraph, Is.Not.SameAs(source));
                Assert.That(runtimeGraph.RuntimeOwner, Is.SameAs(owner));
                Assert.That(runtimeProbe.ExecuteCount, Is.EqualTo(1));
                Assert.That(runtimeProbe.CapturedGraph, Is.SameAs(runtimeGraph));
                Assert.That(runtimeProbe.CapturedOwner, Is.SameAs(owner));
                Assert.That(sourceProbe.ExecuteCount, Is.Zero);

                runtime.Dispose();
                runtime.Dispose();

                Assert.That(runtime.IsDisposed, Is.True);
                Assert.That(runtime.Instance, Is.Null);
                Assert.That(runtimeGraph == null, Is.True);
                Assert.That(runtimeDisposeProbe.DisposeCount, Is.EqualTo(1));
                Assert.That(sourceDisposeProbe.DisposeCount, Is.Zero);
                Assert.That(source == null, Is.False);
            }
            finally
            {
                runtime?.Dispose();
                ShizukuExecutionContext.Enabled = previousContextState;
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void Create_WhenPostInitializationFails_DisposesAndDestroysPartialClone()
        {
            var source = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var sourceProbe = new RuntimeDisposeProbeNode();
            source.AddNode(sourceProbe);
            ShizukuGraphBase capturedClone = null;
            RuntimeDisposeProbeNode runtimeProbe = null;

            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                    ShizukuGraphRuntime<ShizukuGraphBase>.Create(
                        source,
                        afterInitialize: graph =>
                        {
                            capturedClone = graph;
                            runtimeProbe = (RuntimeDisposeProbeNode)graph.Nodes[0];
                            throw new InvalidOperationException("Expected initialization failure");
                        }));

                Assert.That(runtimeProbe, Is.Not.Null);
                Assert.That(runtimeProbe.DisposeCount, Is.EqualTo(1));
                Assert.That(capturedClone == null, Is.True);
                Assert.That(sourceProbe.DisposeCount, Is.Zero);
                Assert.That(source == null, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void TickWithoutRoot_AdvancesLatentButNeverRestartsRoot()
        {
            var source = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            ShizukuGraphRuntime<ShizukuGraphBase> runtime = null;

            try
            {
                var root = new ShizukuRootNode();
                var latent = new ManualLatentTestNode();
                source.AddNode(root);
                source.AddNode(latent);
                source.RootNodeGUID = root.GUID;
                source.Init();
                root.ChainPorts["next"].NextNodeGuid = latent.GUID;
                source.DisposeRuntime();

                runtime = ShizukuGraphRuntime<ShizukuGraphBase>.Create(source);
                var runtimeLatent = (ManualLatentTestNode)runtime.Instance.Guid2NodeMap[latent.GUID];

                runtime.ExecuteRootOnce();
                runtime.Tick(executeRoot: false);

                Assert.That(runtimeLatent.StartCount, Is.EqualTo(1));
                Assert.That(runtimeLatent.TickCount, Is.EqualTo(1));
                Assert.That(runtime.Instance.ActiveLatentNodeCount, Is.EqualTo(1));

                runtimeLatent.TickResult = ShizukuLatentTickResult.Completed;
                runtime.Tick(executeRoot: false);
                runtime.Tick(executeRoot: false);

                Assert.That(runtimeLatent.StartCount, Is.EqualTo(1));
                Assert.That(runtimeLatent.TickCount, Is.EqualTo(2));
                Assert.That(runtime.Instance.ActiveLatentNodeCount, Is.Zero);

                runtime.Tick(executeRoot: true);
                Assert.That(runtimeLatent.StartCount, Is.EqualTo(2));
            }
            finally
            {
                runtime?.Dispose();
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void GraphClip_WhenRootIsOneShot_StillTicksLatentUntilCompletion()
        {
            var source = ScriptableObject.CreateInstance<SkillGraph>();
            var owner = new GameObject("GraphClipRuntimeOwner");
            var handler = new GraphClipHandler();
            var clip = new GraphClipData
            {
                GraphAsset = source,
                TickEveryFrame = false
            };
            var context = new SkillContext { Caster = owner };
            SkillGraph runtimeGraph = null;

            try
            {
                var root = new ShizukuRootNode();
                var latent = new ManualLatentTestNode();
                var completed = new LatentControlFlowProbeNode();
                source.AddNode(root);
                source.AddNode(latent);
                source.AddNode(completed);
                source.RootNodeGUID = root.GUID;
                source.Init();
                root.ChainPorts["next"].NextNodeGuid = latent.GUID;
                latent.ChainPorts["Completed"].NextNodeGuid = completed.GUID;
                source.DisposeRuntime();

                handler.OnEnter(clip, context);
                var runtime = GetGraphClipRuntime(handler);
                runtimeGraph = runtime.Instance;
                var runtimeLatent = (ManualLatentTestNode)runtimeGraph.Guid2NodeMap[latent.GUID];
                var runtimeCompleted = (LatentControlFlowProbeNode)runtimeGraph.Guid2NodeMap[completed.GUID];

                Assert.That(runtimeGraph.SkillContext, Is.SameAs(context));
                Assert.That(runtimeGraph.RuntimeOwner, Is.SameAs(owner));
                Assert.That(runtimeLatent.StartCount, Is.EqualTo(1));

                runtimeLatent.TickResult = ShizukuLatentTickResult.Completed;
                handler.OnUpdate(clip, 0.1f, 0.1f, context);
                handler.OnUpdate(clip, 0.2f, 0.1f, context);

                Assert.That(runtimeCompleted.ExecuteCount, Is.EqualTo(1));
                Assert.That(runtimeLatent.StartCount, Is.EqualTo(1),
                    "TickEveryFrame=false must suppress Root re-execution without freezing Latent work.");
                Assert.That(runtimeGraph.ActiveLatentNodeCount, Is.Zero);

                handler.OnExit(clip, context);
                Assert.That(runtimeGraph == null, Is.True);
            }
            finally
            {
                if (runtimeGraph != null)
                    handler.OnExit(clip, context);
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static ShizukuGraphRuntime<SkillGraph> GetGraphClipRuntime(GraphClipHandler handler)
        {
            var field = typeof(GraphClipHandler).GetField(
                "_runtime",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (ShizukuGraphRuntime<SkillGraph>)field.GetValue(handler);
        }
    }
}
