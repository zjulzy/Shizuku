using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Shizuku.Core.Editor
{
    static class ShizukuSettingsProvider
    {
        [SettingsProvider]
        static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Shizuku", SettingsScope.Project)
            {
                label = "Shizuku",
                guiHandler = DrawSettings,
                keywords = new HashSet<string>
                {
                    "Shizuku",
                    "Module",
                    "Tag",
                    "Odin",
                    "Scripting Define Symbols"
                }
            };
        }

        static void DrawSettings(string searchContext)
        {
            var target = ShizukuDefineInstaller.CurrentBuildTarget;
            var tagEnabled = ShizukuDefineInstaller.IsDefineEnabled(ShizukuDefineInstaller.TagDefine);
            var odinAvailable = ShizukuDefineInstaller.IsOdinInspectorAvailable();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Modules", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active Build Target", target.TargetName);
            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(!odinAvailable && !tagEnabled))
            {
                var nextTagEnabled = EditorGUILayout.ToggleLeft(
                    new GUIContent(
                        "Enable Tag Module",
                        "Adds or removes SHIZUKU_TAG for the active build target."),
                    tagEnabled);

                if (nextTagEnabled != tagEnabled)
                {
                    ShizukuDefineInstaller.SetDefineEnabled(
                        ShizukuDefineInstaller.TagDefine,
                        nextTagEnabled);
                    GUIUtility.ExitGUI();
                }
            }

            if (!odinAvailable)
            {
                EditorGUILayout.HelpBox(
                    "Install Odin Inspector before enabling the Tag module. " +
                    "Shizuku expects Sirenix.OdinInspector.Attributes.dll to be available.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "The Tag module is controlled by SHIZUKU_TAG for the active build target.",
                    MessageType.Info);
            }
        }
    }
}
