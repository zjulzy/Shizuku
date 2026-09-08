using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shizuku.Core;
using Shizuku.Graph;
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

    }
}
