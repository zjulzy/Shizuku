using System;
using System.IO;
using NUnit.Framework;
using Shizuku.Graph.Editor;
using UnityEngine.UIElements;

namespace Shizuku.Tests.EditMode
{
    public class GeneratorPathSettingsTests
    {
        private string _assetsPath;

        [SetUp]
        public void SetUp()
        {
            _assetsPath = Path.Combine(Path.GetTempPath(), "ShizukuGeneratorPathTests", "Assets");
        }

        [TestCase("Assets/Generated/Nodes", "Assets/Generated/Nodes")]
        [TestCase("Assets\\Generated\\Nodes\\", "Assets/Generated/Nodes")]
        [TestCase("Assets", "Assets")]
        public void NormalizeAssetFolderPath_AcceptsProjectRelativeAssetsPaths(
            string input,
            string expected)
        {
            var success = ShizukuGeneratorPathUtility.TryNormalizeAssetFolderPath(
                input,
                _assetsPath,
                out var normalized,
                out var error);

            Assert.That(success, Is.True, error);
            Assert.That(normalized, Is.EqualTo(expected));
        }

        [Test]
        public void NormalizeAssetFolderPath_AcceptsAbsolutePathInsideAssets()
        {
            var input = Path.Combine(_assetsPath, "Generated", "Nodes");

            var success = ShizukuGeneratorPathUtility.TryNormalizeAssetFolderPath(
                input,
                _assetsPath,
                out var normalized,
                out var error);

            Assert.That(success, Is.True, error);
            Assert.That(normalized, Is.EqualTo("Assets/Generated/Nodes"));
        }

        [TestCase("")]
        [TestCase("Packages/com.example")]
        [TestCase("Assets/../Packages/com.example")]
        public void NormalizeAssetFolderPath_RejectsPathsOutsideAssets(string input)
        {
            var success = ShizukuGeneratorPathUtility.TryNormalizeAssetFolderPath(
                input,
                _assetsPath,
                out _,
                out var error);

            Assert.That(success, Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void ResolveOutputFilePath_UsesConfiguredPathForNewFile()
        {
            var result = ShizukuGeneratorPathUtility.ResolveOutputFilePath(
                "GeneratedNode.cs",
                "Assets/Configured",
                Array.Empty<string>());

            Assert.That(result, Is.EqualTo("Assets/Configured/GeneratedNode.cs"));
        }

        [Test]
        public void ResolveOutputFilePath_PreservesExistingFileLocation()
        {
            var result = ShizukuGeneratorPathUtility.ResolveOutputFilePath(
                "GeneratedNode.cs",
                "Assets/Configured",
                new[] { "Assets/Existing/GeneratedNode.cs" });

            Assert.That(result, Is.EqualTo("Assets/Existing/GeneratedNode.cs"));
        }

        [Test]
        public void ResolveOutputFilePath_RejectsDuplicateGeneratedFiles()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ShizukuGeneratorPathUtility.ResolveOutputFilePath(
                    "GeneratedNode.cs",
                    "Assets/Configured",
                    new[]
                    {
                        "Assets/First/GeneratedNode.cs",
                        "Assets/Second/GeneratedNode.cs"
                    }));
        }

        [TestCase(
            "CustomParameterEdgePorts.Generated.cs",
            "Assets/Scripts/Node/Generated/CustomParameterEdgePorts.Generated.cs")]
        [TestCase(
            "GetVariableNodes.Custom.Generated.cs",
            "Assets/Scripts/Node/VariableNodes/Generated/GetVariableNodes.Custom.Generated.cs")]
        [TestCase(
            "SetVariableNodes.Custom.Generated.cs",
            "Assets/Scripts/Node/VariableNodes/Generated/SetVariableNodes.Custom.Generated.cs")]
        public void ResolveOutputFilePath_FindsExistingImportedAggregateFile(
            string fileName,
            string expected)
        {
            var result = ShizukuGeneratorPathUtility.ResolveOutputFilePath(
                fileName,
                "Assets/NewLocation");

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void PathPanel_ContainsOneEditableFieldPerRequestedKind()
        {
            var unifiedPanel = ShizukuGeneratorPathPanel.Create(
                GeneratorOutputPathKind.FunctionNode,
                GeneratorOutputPathKind.VariableNode,
                GeneratorOutputPathKind.PortType);
            var blueprintPanel = ShizukuGeneratorPathPanel.Create(
                GeneratorOutputPathKind.Blueprint);

            Assert.That(blueprintPanel.Q<TextField>("generator-output-path-blueprint"), Is.Not.Null);
            Assert.That(unifiedPanel.Q<TextField>("generator-output-path-functionnode"), Is.Not.Null);
            Assert.That(unifiedPanel.Q<TextField>("generator-output-path-variablenode"), Is.Not.Null);
            Assert.That(unifiedPanel.Q<TextField>("generator-output-path-porttype"), Is.Not.Null);
        }

        [Test]
        public void GeneratorTabs_ExposeTheirOutputPathControls()
        {
            var blueprintRoot = new VisualElement();
            var unifiedRoot = new VisualElement();

            new BlueprintGeneratorTab().BuildUI(blueprintRoot);
            new UnifiedShizukuGeneratorTab().BuildUI(unifiedRoot);

            Assert.That(blueprintRoot.Q<TextField>("generator-output-path-blueprint"), Is.Not.Null);
            Assert.That(unifiedRoot.Q<TextField>("generator-output-path-functionnode"), Is.Not.Null);
            Assert.That(unifiedRoot.Q<TextField>("generator-output-path-variablenode"), Is.Not.Null);
            Assert.That(unifiedRoot.Q<TextField>("generator-output-path-porttype"), Is.Not.Null);
        }
    }
}
