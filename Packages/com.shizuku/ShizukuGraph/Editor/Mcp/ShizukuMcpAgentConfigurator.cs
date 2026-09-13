using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Shizuku.Graph.Editor.Mcp
{
    internal static class ShizukuMcpAgentConfigurator
    {
        private static readonly string ProjectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        internal static string ServerName => "shizuku-" + ComputeProjectId(ProjectPath);

        internal static IReadOnlyList<AgentStatus> GetStatuses()
        {
            return new[]
            {
                new AgentStatus("Codex", ShizukuMcpServerInstaller.FindCommand("codex") != null),
                new AgentStatus("Claude Code", ShizukuMcpServerInstaller.FindCommand("claude") != null),
                new AgentStatus("Cursor", ShizukuMcpServerInstaller.FindCommand("cursor") != null)
            };
        }

        internal static IReadOnlyList<ConfigurationResult> ConfigureDetectedAgents()
        {
            if (!ShizukuMcpServerInstaller.IsBuilt)
                return new[] { ConfigurationResult.Fail("Server", "Build the MCP server before configuring agents.") };

            var results = new List<ConfigurationResult>();
            if (ShizukuMcpServerInstaller.FindCommand("codex") != null)
                results.Add(ConfigureCodex());
            if (ShizukuMcpServerInstaller.FindCommand("claude") != null)
                results.Add(ConfigureClaudeCode());
            if (ShizukuMcpServerInstaller.FindCommand("cursor") != null)
                results.Add(ConfigureCursor());
            if (results.Count == 0)
                results.Add(ConfigurationResult.Fail("Agents", "No supported Agent command was found on PATH."));
            return results;
        }

        internal static ConfigurationResult ConfigureCodex()
        {
            try
            {
                var get = Run("codex", new[] { "mcp", "get", ServerName, "--json" });
                if (get.ExitCode == 0)
                {
                    var remove = Run("codex", new[] { "mcp", "remove", ServerName });
                    if (remove.ExitCode != 0)
                        return ConfigurationResult.Fail("Codex", remove.Output);
                }

                var add = Run("codex", new[]
                {
                    "mcp", "add", ServerName, "--",
                    ShizukuMcpServerInstaller.ExecutablePath, "--project", ProjectPath
                });
                return add.ExitCode == 0
                    ? ConfigurationResult.Ok("Codex", "Configured via codex mcp add.")
                    : ConfigurationResult.Fail("Codex", add.Output);
            }
            catch (Exception exception)
            {
                return ConfigurationResult.Fail("Codex", exception.Message);
            }
        }

        internal static ConfigurationResult ConfigureClaudeCode()
        {
            try
            {
                Run("claude", new[] { "mcp", "remove", "--scope", "local", ServerName });
                var add = Run("claude", new[]
                {
                    "mcp", "add", "--scope", "local", ServerName, "--",
                    ShizukuMcpServerInstaller.ExecutablePath, "--project", ProjectPath
                });
                return add.ExitCode == 0
                    ? ConfigurationResult.Ok("Claude Code", "Configured as a local project MCP server.")
                    : ConfigurationResult.Fail("Claude Code", add.Output);
            }
            catch (Exception exception)
            {
                return ConfigurationResult.Fail("Claude Code", exception.Message);
            }
        }

        internal static ConfigurationResult ConfigureCursor()
        {
            try
            {
                var configPath = Path.Combine(ProjectPath, ".cursor", "mcp.json");
                MergeCursorConfiguration(configPath, ServerName, ShizukuMcpServerInstaller.ExecutablePath, ProjectPath, true);
                return ConfigurationResult.Ok("Cursor", "Merged into .cursor/mcp.json. Reload Cursor to pick up the change.");
            }
            catch (Exception exception)
            {
                return ConfigurationResult.Fail("Cursor", exception.Message);
            }
        }

        internal static void MergeCursorConfiguration(
            string configPath,
            string serverName,
            string executablePath,
            string projectPath,
            bool createBackup)
        {
            var root = File.Exists(configPath)
                ? JObject.Parse(File.ReadAllText(configPath, Encoding.UTF8))
                : new JObject();
            var servers = root["mcpServers"] as JObject;
            if (servers == null)
            {
                servers = new JObject();
                root["mcpServers"] = servers;
            }
            servers[serverName] = CreateServerEntry(executablePath, projectPath);

            var directory = Path.GetDirectoryName(configPath);
            Directory.CreateDirectory(directory);
            if (createBackup && File.Exists(configPath))
            {
                var backup = configPath + ".bak." + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Copy(configPath, backup, false);
            }

            File.WriteAllText(configPath, root.ToString(Formatting.Indented), new UTF8Encoding(false));
        }

        internal static string GetGenericConfiguration()
        {
            return new JObject
            {
                ["mcpServers"] = new JObject
                {
                    [ServerName] = CreateServerEntry(ShizukuMcpServerInstaller.ExecutablePath, ProjectPath)
                }
            }.ToString(Formatting.Indented);
        }

        private static JObject CreateServerEntry(string executablePath, string projectPath)
        {
            return new JObject
            {
                ["command"] = executablePath,
                ["args"] = new JArray("--project", projectPath)
            };
        }

        private static ShizukuMcpServerInstaller.ProcessResult Run(string command, string[] arguments)
        {
            return ShizukuMcpServerInstaller.RunProcess(command, arguments, ProjectPath, TimeSpan.FromSeconds(30));
        }

        private static string ComputeProjectId(string path)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(path.ToLowerInvariant()));
            return string.Concat(bytes.Take(4).Select(value => value.ToString("x2")));
        }

        internal readonly struct AgentStatus
        {
            public AgentStatus(string name, bool detected)
            {
                Name = name;
                Detected = detected;
            }

            public string Name { get; }
            public bool Detected { get; }
        }

        internal readonly struct ConfigurationResult
        {
            private ConfigurationResult(string agent, bool success, string message)
            {
                Agent = agent;
                Success = success;
                Message = message;
            }

            public string Agent { get; }
            public bool Success { get; }
            public string Message { get; }
            public static ConfigurationResult Ok(string agent, string message) => new(agent, true, message);
            public static ConfigurationResult Fail(string agent, string message) => new(agent, false, message);
        }
    }
}
