using System.IO;
using System;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.Graph.Editor;
using Shizuku.Graph.Editor.Mcp;
using UnityEditor;
using UnityEngine;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier2")]
    public sealed class ShizukuMcpTests
    {
        private const string TempFolder = "Assets/__ShizukuMcpTests";
        private const string GraphPath = TempFolder + "/Graph.asset";

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);
            AssetDatabase.CreateFolder("Assets", "__ShizukuMcpTests");
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void GraphApply_DryRunThenCommit_PreservesTransactionContract()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            graph.GUID = "mcp-test-graph";
            AssetDatabase.CreateAsset(graph, GraphPath);
            AssetDatabase.SaveAssets();

            var read = (JObject)ShizukuMcpGraphService.Handle("graph_read", new JObject
            {
                ["assetPath"] = GraphPath
            });
            var revision = read.Value<string>("revision");
            var operations = CreateGraphOperations();

            var preview = (JObject)ShizukuMcpGraphService.Handle("graph_apply", new JObject
            {
                ["assetPath"] = GraphPath,
                ["expectedRevision"] = revision,
                ["dryRun"] = true,
                ["operations"] = operations
            });

            Assert.That(preview.Value<bool>("dryRun"), Is.True);
            Assert.That(preview.Value<bool>("applied"), Is.False);
            Assert.That(preview["validation"]?.Value<bool>("valid"), Is.True);
            Assert.That(graph.Nodes, Is.Empty, "dry-run must not mutate the source asset");

            var committed = (JObject)ShizukuMcpGraphService.Handle("graph_apply", new JObject
            {
                ["assetPath"] = GraphPath,
                ["expectedRevision"] = revision,
                ["dryRun"] = false,
                ["operations"] = operations
            });

            Assert.That(committed.Value<bool>("applied"), Is.True);
            Assert.That(graph.Nodes, Has.Count.EqualTo(2));
            Assert.That(graph.RootNodeGUID, Is.EqualTo("root-guid"));

            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(GraphPath);
            Assert.That(reloaded.Nodes, Has.Count.EqualTo(2));
            var root = reloaded.Nodes.Find(node => node.GUID == "root-guid") as ShizukuRootNode;
            root.Init(reloaded);
            Assert.That(root.ChainPorts["next"].NextNodeGuid, Is.EqualTo("log-guid"));

            var conflict = (JObject)ShizukuMcpGraphService.Handle("graph_apply", new JObject
            {
                ["assetPath"] = GraphPath,
                ["expectedRevision"] = revision,
                ["dryRun"] = false,
                ["operations"] = new JArray()
            });
            Assert.That(conflict.Value<bool>("conflict"), Is.True);
        }

        [Test]
        public void CursorMerge_PreservesExistingServersAndWritesOnlyProjectEntry()
        {
            var directory = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Library", "ShizukuMcp", "Tests");
            Directory.CreateDirectory(directory);
            var configPath = Path.Combine(directory, "cursor-mcp.json");
            File.WriteAllText(configPath, "{\"mcpServers\":{\"existing\":{\"url\":\"http://example.invalid\"}},\"other\":true}");

            ShizukuMcpAgentConfigurator.MergeCursorConfiguration(
                configPath,
                "shizuku-test",
                "C:/Tools/Shizuku.Mcp.Server.exe",
                "U:/Project",
                false);

            var root = JObject.Parse(File.ReadAllText(configPath));
            Assert.That(root["other"]?.Value<bool>(), Is.True);
            Assert.That(root["mcpServers"]?["existing"]?["url"]?.Value<string>(), Is.EqualTo("http://example.invalid"));
            Assert.That(root["mcpServers"]?["shizuku-test"]?["command"]?.Value<string>(), Is.EqualTo("C:/Tools/Shizuku.Mcp.Server.exe"));
            Assert.That(root["mcpServers"]?["shizuku-test"]?["args"]?[1]?.Value<string>(), Is.EqualTo("U:/Project"));
        }

        [Test]
        public void NodeCatalog_UsesStableTypeIdAndExposesKnownNode()
        {
            var catalog = (JArray)ShizukuMcpGraphService.Handle("node_catalog", new JObject());
            Assert.That(catalog, Has.Some.Matches<JToken>(token =>
                token.Value<string>("typeId") == typeof(ShizukuLogNode).Assembly.GetName().Name + ":" + typeof(ShizukuLogNode).FullName));
        }

        private static JArray CreateGraphOperations()
        {
            return new JArray
            {
                new JObject
                {
                    ["op"] = "create_node",
                    ["type"] = typeof(ShizukuRootNode).Assembly.GetName().Name + ":" + typeof(ShizukuRootNode).FullName,
                    ["guid"] = "root-guid",
                    ["position"] = new JArray(10, 20, 200, 100)
                },
                new JObject
                {
                    ["op"] = "create_node",
                    ["type"] = typeof(ShizukuLogNode).Assembly.GetName().Name + ":" + typeof(ShizukuLogNode).FullName,
                    ["guid"] = "log-guid",
                    ["position"] = new JArray(300, 20, 200, 100)
                },
                new JObject
                {
                    ["op"] = "connect_control",
                    ["sourceGuid"] = "root-guid",
                    ["port"] = "next",
                    ["targetGuid"] = "log-guid"
                }
            };
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LegacyReadValidateAndPreviewPreserveSource(bool dirty)
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            AssetDatabase.CreateAsset(graph, GraphPath);
            AssetDatabase.SaveAssets();
            if (dirty) { graph.GUID = "unsaved"; EditorUtility.SetDirty(graph); }
            var json = EditorJsonUtility.ToJson(graph);
            var bytes = File.ReadAllBytes(GraphPath);
            Assert.That(graph.SchemaVersion, Is.Zero);
            var read = ShizukuMcpGraphService.ReadGraph(new JObject { ["assetPath"] = GraphPath });
            ShizukuMcpGraphService.ValidateGraph(new JObject { ["assetPath"] = GraphPath });
            var preview = ShizukuMcpGraphService.ApplyGraph(new JObject
            {
                ["assetPath"] = GraphPath, ["expectedRevision"] = read["revision"],
                ["dryRun"] = true, ["operations"] = CreateGraphOperations()
            });
            Assert.That(preview.Value<bool>("applied"), Is.False);
            Assert.That(preview.Value<bool?>("dryRun"), dirty ? Is.Null : Is.True);
            Assert.That(EditorJsonUtility.ToJson(graph), Is.EqualTo(json));
            Assert.That(File.ReadAllBytes(GraphPath), Is.EqualTo(bytes));
            Assert.That(EditorUtility.IsDirty(graph), Is.EqualTo(dirty));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ApplyRequiresRevisionAndRejectsDirtyCurrentGraph(bool dryRun)
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            AssetDatabase.CreateAsset(graph, GraphPath);
            ShizukuGraphMigrationService.EnsureCurrent(graph);
            var request = new JObject { ["assetPath"] = GraphPath, ["dryRun"] = dryRun, ["operations"] = CreateGraphOperations() };
            var bytes = File.ReadAllBytes(GraphPath);
            Assert.That(ShizukuMcpGraphService.ApplyGraph(request).Value<string>("code"), Is.EqualTo("revision_required"));
            request["expectedRevision"] = "stale";
            Assert.That(ShizukuMcpGraphService.ApplyGraph(request).Value<bool>("conflict"), Is.True);
            request["expectedRevision"] = ShizukuMcpGraphService.ReadGraph(request)["revision"];
            graph.GUID = "designer-unsaved";
            EditorUtility.SetDirty(graph);
            var json = EditorJsonUtility.ToJson(graph);
            Assert.That(ShizukuMcpGraphService.ApplyGraph(request).Value<string>("code"), Is.EqualTo("dirty_graph"));
            Assert.That(EditorJsonUtility.ToJson(graph), Is.EqualTo(json));
            Assert.That(EditorUtility.IsDirty(graph), Is.True);
            Assert.That(File.ReadAllBytes(GraphPath), Is.EqualTo(bytes));
        }

        [Test]
        public void LegacyCommitMigratesWithUndoAndProtectsWholeAssetRevision()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            AssetDatabase.CreateAsset(graph, GraphPath);
            var request = new JObject { ["assetPath"] = GraphPath, ["dryRun"] = false, ["operations"] = CreateGraphOperations() };
            request["expectedRevision"] = ShizukuMcpGraphService.ReadGraph(request)["revision"];
            Assert.That(ShizukuMcpGraphService.ApplyGraph(request).Value<bool>("applied"), Is.True);
            Assert.That(graph.SchemaVersion, Is.EqualTo(ShizukuGraphBase.CurrentSchemaVersion));
            Assert.That(EditorUtility.IsDirty(graph), Is.False);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(graph.SchemaVersion, Is.Zero);
            Assert.That(graph.Nodes, Is.Empty);
            Undo.PerformRedo();
            AssetDatabase.SaveAssetIfDirty(graph);
            request["expectedRevision"] = ShizukuMcpGraphService.ReadGraph(request)["revision"];
            graph.Methods.Add(new ShizukuMethod("changed-other-method"));
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssetIfDirty(graph);
            Assert.That(ShizukuMcpGraphService.ApplyGraph(request).Value<bool>("conflict"), Is.True);
            Undo.ClearUndo(graph);
        }

        [TestCase("operation", false)]
        [TestCase("validation", false)]
        [TestCase("save", false)]
        [TestCase("save", true)]
        [TestCase("silent-save", false)]
        public void ExecutorFailureRestoresMemoryDiskAndDirty(string phase, bool dirty)
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            AssetDatabase.CreateAsset(graph, GraphPath);
            if (dirty) { graph.GUID = "unsaved"; EditorUtility.SetDirty(graph); }
            var json = EditorJsonUtility.ToJson(graph);
            var bytes = File.ReadAllBytes(GraphPath);
            var options = new GraphEditExecutionOptions
            {
                SaveAsset = true,
                Prepare = g => ShizukuGraphMigrationService.EnsureCurrent(g, false),
                Validate = _ => { if (phase == "validation") throw new InvalidOperationException("validation"); },
                Save = g =>
                {
                    if (phase == "silent-save") return;
                    AssetDatabase.SaveAssetIfDirty(g);
                    throw new IOException("save failed after writing");
                }
            };
            var operations = new System.Collections.Generic.List<GraphEditOperation>
            { new CreateNodeOperation(new ShizukuLogNode { GUID = "same" }, default) };
            if (phase == "operation") operations.Add(new CreateNodeOperation(new ShizukuLogNode { GUID = "same" }, default));
            Assert.Catch(() => UnityGraphEditExecutor.Execute(graph, "", operations, "failure", options));
            Assert.That(EditorJsonUtility.ToJson(graph), Is.EqualTo(json));
            Assert.That(File.ReadAllBytes(GraphPath), Is.EqualTo(bytes));
            Assert.That(EditorUtility.IsDirty(graph), Is.EqualTo(dirty));
            Undo.ClearUndo(graph);
            if (!dirty)
            {
                AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var reloaded = AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(GraphPath);
                Assert.That(reloaded.Nodes, Is.Empty);
                Assert.That(reloaded.SchemaVersion, Is.Zero);
            }
        }

        [Test]
        public void InvalidPreviewDoesNotMigrateOrSaveSource()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            graph.RootNodeGUID = "missing-root";
            AssetDatabase.CreateAsset(graph, GraphPath);
            var json = EditorJsonUtility.ToJson(graph);
            var bytes = File.ReadAllBytes(GraphPath);
            var request = new JObject { ["assetPath"] = GraphPath, ["dryRun"] = false, ["operations"] = new JArray() };
            request["expectedRevision"] = ShizukuMcpGraphService.ReadGraph(request)["revision"];
            var result = ShizukuMcpGraphService.ApplyGraph(request);
            Assert.That(result.Value<bool>("applied"), Is.False);
            Assert.That(result["validation"].Value<bool>("valid"), Is.False);
            Assert.That(EditorJsonUtility.ToJson(graph), Is.EqualTo(json));
            Assert.That(File.ReadAllBytes(GraphPath), Is.EqualTo(bytes));
            Assert.That(EditorUtility.IsDirty(graph), Is.False);
        }

        [Test]
        public void RefreshFailureReportsCommittedWarningAndRetainsUndo()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            AssetDatabase.CreateAsset(graph, GraphPath);
            var warning = UnityGraphEditExecutor.Execute(graph, "", new GraphEditOperation[]
            { new CreateNodeOperation(new ShizukuLogNode { GUID = "committed" }, default) }, "commit",
                new GraphEditExecutionOptions
                {
                    SaveAsset = true, RefreshOpenGraph = true,
                    Refresh = _ => throw new InvalidOperationException("refresh failed")
                });
            Assert.That(warning, Does.Contain("committed"));
            Assert.That(graph.Nodes, Has.Count.EqualTo(1));
            Assert.That(EditorUtility.IsDirty(graph), Is.False);
            Assert.That(File.ReadAllText(GraphPath), Does.Contain("committed"));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(graph.Nodes, Is.Empty);
            Undo.ClearUndo(graph);
        }
    }
}
