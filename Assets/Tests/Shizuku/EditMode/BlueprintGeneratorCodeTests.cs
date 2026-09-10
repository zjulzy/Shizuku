using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.Graph.Editor;
using Shizuku.Tests.EditMode.GeneratorFixtures;

public sealed class BlueprintGeneratorGlobalNamespaceBehavior
    : BlueprintBehavior<BlueprintGeneratorGlobalNamespaceBehavior>
{
}

namespace Shizuku.Tests.EditMode.GeneratorFixtures
{
    public sealed class NamespacedBlueprintBehavior
        : BlueprintBehavior<NamespacedBlueprintBehavior>
    {
    }

    public static class NestedBehaviorContainer
    {
        public sealed class NestedBlueprintBehavior
            : BlueprintBehavior<NestedBlueprintBehavior>
        {
        }
    }
}

namespace @class
{
    public sealed class @event : BlueprintBehavior<@event>
    {
    }
}

namespace Shizuku.Tests.EditMode
{
    public sealed class BlueprintGeneratorCodeTests
    {
        [Test]
        public void GenerateBlueprintCode_UsesFullyQualifiedBehaviorTypeEverywhere()
        {
            var code = BlueprintGeneratorTab.GenerateBlueprintCode(
                typeof(NamespacedBlueprintBehavior),
                "GeneratedNamespacedBlueprint");

            const string expectedType =
                "global::Shizuku.Tests.EditMode.GeneratorFixtures.NamespacedBlueprintBehavior";

            StringAssert.Contains(
                $"ShizukuBluePrint<{expectedType}>",
                code);
            StringAssert.Contains(
                $"InitializeBehavior({expectedType} behavior)",
                code);
        }

        [Test]
        public void GetCSharpTypeReference_SupportsGlobalNamespaceType()
        {
            var result = BlueprintGeneratorTab.GetCSharpTypeReference(
                typeof(BlueprintGeneratorGlobalNamespaceBehavior));

            Assert.That(
                result,
                Is.EqualTo("global::BlueprintGeneratorGlobalNamespaceBehavior"));
        }

        [Test]
        public void GetCSharpTypeReference_UsesCSharpSyntaxForNestedType()
        {
            var result = BlueprintGeneratorTab.GetCSharpTypeReference(
                typeof(NestedBehaviorContainer.NestedBlueprintBehavior));

            Assert.That(
                result,
                Is.EqualTo(
                    "global::Shizuku.Tests.EditMode.GeneratorFixtures." +
                    "NestedBehaviorContainer.NestedBlueprintBehavior"));
        }

        [Test]
        public void GetCSharpTypeReference_EscapesReservedKeywords()
        {
            var result = BlueprintGeneratorTab.GetCSharpTypeReference(typeof(global::@class.@event));

            Assert.That(result, Is.EqualTo("global::@class.@event"));
        }
    }
}
