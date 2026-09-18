using System.Collections.Generic;
using NUnit.Framework;
using Shizuku.Tag;
using UnityEngine;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier1")]
    public sealed class TagBehaviorTests
    {
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

                Assert.That(collection.TryAdd(stunned, config), Is.True);

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
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void TagCollection_TryAdd_RejectsUnknownDuplicateAndBlockedTagsWithoutMutation()
        {
            var config = ScriptableObject.CreateInstance<TagConfig>();
            try
            {
                var state = config.AddTag("State");
                var stunned = config.AddTag("State.Stunned", state);
                var action = config.AddTag("Action");
                var casting = config.AddTag("Action.Casting", action);
                var collection = new TagCollection();

                config.SetBlockRule("Action.Casting", new List<string> { "State.Stunned" });
                config.SetExclusionRule("Action.Casting", new List<string> { "State.Stunned" });

                Assert.That(collection.TryAdd(0xFFFFFFFFu, config), Is.False);
                Assert.That(collection.Count, Is.Zero);

                Assert.That(collection.TryAdd(stunned, config), Is.True);
                Assert.That(collection.TryAdd(stunned, config), Is.False);
                Assert.That(collection.TryAdd(casting, config), Is.False);

                Assert.That(collection.HasExact(stunned), Is.True);
                Assert.That(collection.HasExact(casting), Is.False);
                Assert.That(collection.Count, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void TagCollection_TryAdd_AppliesExclusionInConfiguredDirectionOnly()
        {
            var config = ScriptableObject.CreateInstance<TagConfig>();
            try
            {
                var state = config.AddTag("State");
                var stunned = config.AddTag("State.Stunned", state);
                var action = config.AddTag("Action");
                var casting = config.AddTag("Action.Casting", action);
                var collection = new TagCollection();

                config.SetExclusionRule("State.Stunned", new List<string> { "Action.Casting" });

                Assert.That(collection.TryAdd(casting, config), Is.True);
                Assert.That(collection.TryAdd(stunned, config), Is.True);
                Assert.That(collection.HasExact(stunned), Is.True);
                Assert.That(collection.HasExact(casting), Is.False);

                collection.Clear();

                Assert.That(collection.TryAdd(stunned, config), Is.True);
                Assert.That(collection.TryAdd(casting, config), Is.True);

                Assert.That(collection.HasExact(stunned), Is.True);
                Assert.That(collection.HasExact(casting), Is.True,
                    "A 排除 B 不应隐式创建 B 排除 A 的反向规则");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
