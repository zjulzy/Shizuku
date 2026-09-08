using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Shizuku.Graph;
using UnityEngine;
using UnityEngine.TestTools;

namespace Shizuku.Tests.PlayMode
{
    public class BlueprintLifecycleTestBehavior : BlueprintBehavior<BlueprintLifecycleTestBehavior>
    {
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

    public sealed class BlueprintBehaviorLifecyclePlayModeTests
    {
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
