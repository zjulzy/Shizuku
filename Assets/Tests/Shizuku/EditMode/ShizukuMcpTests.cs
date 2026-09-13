using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shizuku.Graph;
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
    }
}
