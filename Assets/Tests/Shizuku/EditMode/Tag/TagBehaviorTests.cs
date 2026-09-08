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
                Object.DestroyImmediate(config);
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
                Object.DestroyImmediate(config);
            }
        }
    }
}
