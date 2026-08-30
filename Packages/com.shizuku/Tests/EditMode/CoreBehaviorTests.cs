using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shizuku.Core;
using Shizuku.Graph;
using Shizuku.Tag;
using UnityEngine;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier1")]
    public sealed class CoreBehaviorTests
    {
        private sealed class PooledItem
        {
            public int Value;
        }

        [Serializable]
        private sealed class CustomPayload
        {
            public int Value;
        }

        [Test]
        public void ObjectPool_GetAfterRelease_ReusesInstanceAndTracksLifecycle()
        {
            var getCount = 0;
            var releaseCount = 0;
            var pool = new ShizukuObjectPool<PooledItem>(
                onGet: _ => getCount++,
                onRelease: item =>
                {
                    item.Value = 0;
                    releaseCount++;
                },
                defaultCapacity: 1,
                maxSize: 2);

            var first = pool.Get();
            first.Value = 42;

            Assert.That(pool.CountAll, Is.EqualTo(1));
            Assert.That(pool.CountActive, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.Zero);

            pool.Release(first);

            Assert.That(first.Value, Is.Zero);
            Assert.That(releaseCount, Is.EqualTo(1));
            Assert.That(pool.CountActive, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(1));

            var reused = pool.Get();

            Assert.That(reused, Is.SameAs(first));
            Assert.That(getCount, Is.EqualTo(2));
            Assert.That(pool.CountAll, Is.EqualTo(1));
            Assert.That(pool.CountActive, Is.EqualTo(1));
        }

        [Test]
        public void RuntimeVariableStore_LoadAndClone_KeepDictionariesIndependent()
        {
            var intVariable = new GraphVariable("Score", VariableType.Int)
            {
                GUID = "score-guid",
                IntValue = 7
            };
            var customVariable = new GraphVariable("Payload", VariableType.Custom)
            {
                GUID = "payload-guid",
                CustomValue = new CustomPayload { Value = 11 }
            };
            var store = new RuntimeVariableStore();

            store.LoadFromVariables(new List<GraphVariable> { intVariable, customVariable });

            Assert.That(store.Ints["score-guid"], Is.EqualTo(7));
            Assert.That(
                store.GetOrCreateCustomDict<CustomPayload>()["payload-guid"].Value,
                Is.EqualTo(11));

            var clone = store.Clone();
            clone.Ints["score-guid"] = 99;
            clone.GetOrCreateCustomDict<CustomPayload>()["payload-guid"] =
                new CustomPayload { Value = 23 };

            Assert.That(store.Ints["score-guid"], Is.EqualTo(7));
            Assert.That(
                store.GetOrCreateCustomDict<CustomPayload>()["payload-guid"].Value,
                Is.EqualTo(11));
            Assert.That(clone.Ints["score-guid"], Is.EqualTo(99));
            Assert.That(
                clone.GetOrCreateCustomDict<CustomPayload>()["payload-guid"].Value,
                Is.EqualTo(23));
        }

        [Test]
        public void TagConfig_AddTag_CreatesHierarchyThatCollectionCanMatch()
        {
            var config = ScriptableObject.CreateInstance<TagConfig>();
            try
            {
                var state = config.AddTag("State");
                var idle = config.AddTag("State.Idle", state);
                var stunned = config.AddTag("State.Stunned", state);
                var collection = new TagCollection();

                collection.Add(stunned);

                Assert.That(state, Is.EqualTo(0x01000000u));
                Assert.That(idle, Is.EqualTo(0x01010000u));
                Assert.That(stunned, Is.EqualTo(0x01020000u));
                Assert.That(collection.HasExact(state), Is.False);
                Assert.That(collection.HasExact(stunned), Is.True);
                Assert.That(collection.HasAncestor(state), Is.True);
                Assert.That(collection.HasAncestor(idle), Is.False);
                Assert.That(state.IsPrefixOf(stunned), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void TagConfig_BlockAndCancelRules_UseCurrentExactTags()
        {
            var config = ScriptableObject.CreateInstance<TagConfig>();
            try
            {
                var state = config.AddTag("State");
                var stunned = config.AddTag("State.Stunned", state);
                var action = config.AddTag("Action");
                var casting = config.AddTag("Action.Casting", action);
                var collection = new TagCollection();
                var cancelled = new List<uint>();

                config.SetBlockRule("Action.Casting", new List<string> { "State.Stunned" });
                config.SetCancelRule("State.Stunned", new List<string> { "Action.Casting" });

                collection.Add(stunned);
                Assert.That(config.IsBlocked(casting, collection), Is.True);

                collection.Clear();
                collection.Add(casting);
                config.GetCancelledTags(stunned, collection, cancelled);

                Assert.That(cancelled, Is.EqualTo(new[] { casting }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
