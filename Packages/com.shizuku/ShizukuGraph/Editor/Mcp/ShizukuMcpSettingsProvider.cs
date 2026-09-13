using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Shizuku.Graph.Editor.Mcp
{
    internal static class ShizukuMcpSettingsProvider
    {
        private static string _configurationStatus;
        private static MessageType _configurationMessageType = MessageType.Info;

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Shizuku/MCP", SettingsScope.Project)
            {
                label = "MCP",
                guiHandler = DrawSettings,
                keywords = new HashSet<string> { "Shizuku", "MCP", "Codex", "Claude", "Cursor", "Agent", "AI" }
            };
        }

        private static void DrawSettings(string searchContext)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shizuku MCP", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Runs a loopback-only Unity bridge and a project-scoped stdio MCP server. " +
                "Build only generates the executable; it does not start a persistent server. " +
                "A configured Agent starts the server on demand, while the Unity bridge follows the toggle below.",
                MessageType.Info);

            var enabled = EditorGUILayout.ToggleLeft("Enable Unity Editor bridge", ShizukuMcpBridge.Enabled);
            if (enabled != ShizukuMcpBridge.Enabled)
                ShizukuMcpBridge.Enabled = enabled;

            EditorGUILayout.LabelField("Bridge", ShizukuMcpBridge.IsRunning ? $"Running on 127.0.0.1:{ShizukuMcpBridge.Port}" : "Stopped");
            EditorGUILayout.LabelField("Server executable", ShizukuMcpServerInstaller.IsBuilt ? ShizukuMcpServerInstaller.ExecutablePath : "Not built");
            EditorGUILayout.LabelField("Server name", ShizukuMcpAgentConfigurator.ServerName);

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(ShizukuMcpServerInstaller.IsBuilding))
            {
                if (GUILayout.Button(ShizukuMcpServerInstaller.IsBuilding ? "Building..." : "Build MCP Server Executable (One-time Setup)"))
                    ShizukuMcpServerInstaller.Build(OnBuildCompleted);

                if (GUILayout.Button("Build Executable and Configure Detected Agents"))
                {
                    ShizukuMcpServerInstaller.Build((success, message) =>
                    {
                        if (success)
                            ConfigureAgents();
                        else
                            SetStatus(message, MessageType.Error);
                    });
                }
            }

            using (new EditorGUI.DisabledScope(!ShizukuMcpServerInstaller.IsBuilt || ShizukuMcpServerInstaller.IsBuilding))
            {
                if (GUILayout.Button("Configure Detected Agents"))
                    ConfigureAgents();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Supported Agents", EditorStyles.boldLabel);
            foreach (var status in ShizukuMcpAgentConfigurator.GetStatuses())
                EditorGUILayout.LabelField(status.Name, status.Detected ? "Detected" : "Not found on PATH");

            if (!string.IsNullOrEmpty(_configurationStatus))
                EditorGUILayout.HelpBox(_configurationStatus, _configurationMessageType);
            else if (!string.IsNullOrEmpty(ShizukuMcpServerInstaller.LastStatus))
                EditorGUILayout.HelpBox(ShizukuMcpServerInstaller.LastStatus, MessageType.None);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Other MCP Clients", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Automatic configuration currently supports Codex, Claude Code, and Cursor only. " +
                "For another stdio-compatible Agent, copy this configuration and adapt its config-file wrapper if needed.",
                MessageType.Info);
            using (new EditorGUI.DisabledScope(!ShizukuMcpServerInstaller.IsBuilt))
            {
                if (GUILayout.Button("Copy Generic stdio Configuration"))
                {
                    EditorGUIUtility.systemCopyBuffer = ShizukuMcpAgentConfigurator.GetGenericConfiguration();
                    SetStatus("Generic stdio MCP configuration copied to the clipboard.", MessageType.Info);
                }
            }
        }

        private static void OnBuildCompleted(bool success, string message)
        {
            SetStatus(message, success ? MessageType.Info : MessageType.Error);
        }

        private static void ConfigureAgents()
        {
            var results = ShizukuMcpAgentConfigurator.ConfigureDetectedAgents();
            var success = results.All(result => result.Success);
            var message = string.Join("\n", results.Select(result =>
                $"{(result.Success ? "OK" : "FAILED")} {result.Agent}: {result.Message}"));
            SetStatus(message, success ? MessageType.Info : MessageType.Error);
        }

        private static void SetStatus(string message, MessageType type)
        {
            _configurationStatus = message;
            _configurationMessageType = type;
            SettingsService.OpenProjectSettings("Project/Shizuku/MCP");
        }
    }
}
