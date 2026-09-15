using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.Graph.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier2")]
    public sealed class GraphMigrationTests
    {
        private const string TempFolder = "Assets/__ShizukuMigrationTests";
        private const string LegacyAssetPath = TempFolder + "/LegacyGraph.asset";
        private const string MissingTypeAssetPath = TempFolder + "/MissingTypeGraph.asset";
        private const string NewerAssetPath = TempFolder + "/NewerGraph.asset";

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);

            AssetDatabase.CreateFolder("Assets", "__ShizukuMigrationTests");
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void UnversionedLegacyGraph_MigratesAndReimportsWithoutChangingGraphData()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var source = new AddNode_Float
            {
                PositionAndSize = new float4(10f, 20f, 200f, 100f)
            };
            var target = new AddNode_Float
            {
                PositionAndSize = new float4(350f, 20f, 200f, 100f)
            };
            graph.AddNode(source);
            graph.AddNode(target);
            graph.AddParameterEdge(source, "Result", target, "A");
            graph.Variables.Add(new GraphVariable("LegacyValue", VariableType.Float)
            {
                FloatValue = 12.5f
            });

            var method = new ShizukuMethod("LegacyMethod");
            var methodSource = new AddNode_Float();
            var methodTarget = new AddNode_Float();
            method.AddNode(methodSource);
            method.AddNode(methodTarget);
            method.AddParameterEdge(methodSource, "Result", methodTarget, "A");
            graph.AddMethod(method);

            AssetDatabase.CreateAsset(graph, LegacyAssetPath);
            Save(graph);
            var sourceGuid = source.GUID;
            var targetGuid = target.GUID;
            var edgeGuid = graph.Edges.Single().GUID;
            var methodGuid = method.GUID;

            // v0.3.0 - v0.8.0 的真实图资产没有 _schemaVersion 字段。
            RewriteAsset(
                LegacyAssetPath,
                yaml => Regex.Replace(yaml, @"(?m)^  _schemaVersion: .*(\r?\n)?", string.Empty));

            var legacyGraph = Reload(LegacyAssetPath);
            Assert.That(legacyGraph.SchemaVersion, Is.Zero);

            var report = ShizukuGraphMigrationService.EnsureCurrent(legacyGraph);

            Assert.That(report.Status, Is.EqualTo(GraphAssetCompatibilityStatus.Migrated));
            Assert.That(report.SourceVersion, Is.Zero);
            Assert.That(report.Graph.SchemaVersion, Is.EqualTo(ShizukuGraphBase.CurrentSchemaVersion));

            var migratedGraph = Reload(LegacyAssetPath);
            Assert.That(migratedGraph.SchemaVersion, Is.EqualTo(ShizukuGraphBase.CurrentSchemaVersion));
            Assert.That(migratedGraph.Nodes.Select(node => node.GUID),
                Is.EquivalentTo(new[] { sourceGuid, targetGuid }));
            Assert.That(migratedGraph.Edges.Single().GUID, Is.EqualTo(edgeGuid));
            Assert.That(migratedGraph.Edges.Single().OutputNodeGuid, Is.EqualTo(sourceGuid));
            Assert.That(migratedGraph.Edges.Single().InputNodeGuid, Is.EqualTo(targetGuid));
            Assert.That(migratedGraph.Variables.Single().Name, Is.EqualTo("LegacyValue"));
            Assert.That(migratedGraph.Variables.Single().FloatValue, Is.EqualTo(12.5f));
            Assert.That(migratedGraph.Methods.Single().GUID, Is.EqualTo(methodGuid));
            Assert.That(migratedGraph.Methods.Single().Nodes, Has.Count.EqualTo(2));
            Assert.That(migratedGraph.Methods.Single().Edges, Has.Count.EqualTo(1));
            StringAssert.Contains(
                $"_schemaVersion: {ShizukuGraphBase.CurrentSchemaVersion}",
                File.ReadAllText(LegacyAssetPath));
        }

        [Test]
        public void MissingNodeType_BlocksMigrationAndSurvivesInitSaveAndReload()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            graph.AddNode(new ShizukuLogNode
            {
                PositionAndSize = new float4(15f, 25f, 200f, 100f)
            });
            AssetDatabase.CreateAsset(graph, MissingTypeAssetPath);
            Save(graph);

            const string originalType = "class: ShizukuLogNode, ns: Shizuku.Graph, asm: ShizukuGraph.Runtime";
            const string missingType = "class: RemovedNodeForMigrationTest, ns: Shizuku.Graph, asm: ShizukuGraph.Runtime";
            RewriteAsset(MissingTypeAssetPath, yaml =>
            {
                StringAssert.Contains(originalType, yaml);
                return yaml.Replace(originalType, missingType);
            });

            var brokenGraph = Reload(MissingTypeAssetPath);
            var inspection = ShizukuGraphMigrationService.Inspect(brokenGraph);

            Assert.That(inspection.Status,
                Is.EqualTo(GraphAssetCompatibilityStatus.MissingManagedReferenceTypes));
            Assert.That(inspection.MissingTypes.Count, Is.EqualTo(1));
            Assert.That(inspection.MissingTypes[0].ClassName,
                Is.EqualTo("RemovedNodeForMigrationTest"));
            Assert.That(inspection.MissingTypes[0].AssemblyName,
                Is.EqualTo("ShizukuGraph.Runtime"));

            var migration = ShizukuGraphMigrationService.EnsureCurrent(brokenGraph);
            Assert.That(migration.CanOpen, Is.False);
            Assert.That(brokenGraph.SchemaVersion, Is.Zero);
            Assert.That(brokenGraph.Nodes, Has.Count.EqualTo(1));
            Assert.That(brokenGraph.Nodes[0], Is.Null);

            LogAssert.Expect(
                LogType.Error,
                new Regex("检测到 1 个无法反序列化的节点和 0 条无法反序列化的边"));
            brokenGraph.Init();
            Assert.That(brokenGraph.Nodes, Has.Count.EqualTo(1), "Init 不得删除缺失类型占位");

            EditorUtility.SetDirty(brokenGraph);
            AssetDatabase.SaveAssetIfDirty(brokenGraph);
            var reloadedGraph = Reload(MissingTypeAssetPath);

            Assert.That(ShizukuGraphMigrationService.Inspect(reloadedGraph).Status,
                Is.EqualTo(GraphAssetCompatibilityStatus.MissingManagedReferenceTypes));
            StringAssert.Contains(missingType, File.ReadAllText(MissingTypeAssetPath));
        }

        [Test]
        public void NewerGraphVersion_IsRejectedWithoutRewritingAsset()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            graph.AddNode(new AddNode_Float());
            AssetDatabase.CreateAsset(graph, NewerAssetPath);
            Save(graph);

            var newerVersion = ShizukuGraphBase.CurrentSchemaVersion + 1;
            RewriteAsset(
                NewerAssetPath,
                yaml => Regex.Replace(
                    yaml,
                    @"(?m)^  _schemaVersion: .*?$",
                    $"  _schemaVersion: {newerVersion}"));

            var newerGraph = Reload(NewerAssetPath);
            var yamlBefore = File.ReadAllText(NewerAssetPath);
            var graphView = new ShizukuGraphView();

            var exception = Assert.Throws<GraphAssetCompatibilityException>(() =>
                graphView.LoadFromAsset(newerGraph));

            Assert.That(exception.Report.Status,
                Is.EqualTo(GraphAssetCompatibilityStatus.NewerSchemaVersion));
            Assert.That(graphView.RuntimeGraph, Is.Null);
            Assert.That(File.ReadAllText(NewerAssetPath), Is.EqualTo(yamlBefore));
        }

        private static void Save(ShizukuGraphBase graph)
        {
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssetIfDirty(graph);
        }

        private static ShizukuGraphBase Reload(string assetPath)
        {
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(assetPath);
        }

        private static void RewriteAsset(string assetPath, System.Func<string, string> rewrite)
        {
            var yaml = File.ReadAllText(assetPath);
            File.WriteAllText(assetPath, rewrite(yaml));
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
