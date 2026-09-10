using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shizuku.Graph.Editor
{
    internal static class ShizukuGeneratorPathPanel
    {
        internal static VisualElement Create(params GeneratorOutputPathKind[] kinds)
        {
            var panel = new VisualElement
            {
                name = "generator-output-paths",
                style =
                {
                    paddingTop = 6,
                    paddingBottom = 6,
                    paddingLeft = 10,
                    paddingRight = 10,
                    backgroundColor = new Color(0.17f, 0.17f, 0.17f, 1f)
                }
            };

            panel.Add(new Label("Output Paths")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 3
                }
            });

            foreach (var kind in kinds)
                panel.Add(CreatePathRow(kind));

            panel.Add(new Label("Changing a path affects new files only. Existing generated files are regenerated in place.")
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.6f, 0.6f, 0.6f),
                    marginTop = 3
                }
            });

            return panel;
        }

        private static VisualElement CreatePathRow(GeneratorOutputPathKind kind)
        {
            var settings = ShizukuGeneratorSettings.instance;
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginTop = 2,
                    marginBottom = 2
                }
            };

            var label = new Label(GetLabel(kind))
            {
                style =
                {
                    width = 120,
                    minWidth = 120
                }
            };

            var pathField = new TextField
            {
                name = GetElementName(kind),
                value = settings.GetOutputPath(kind),
                isDelayed = true,
                tooltip = "Project-relative folder under Assets/."
            };
            pathField.style.flexGrow = 1;

            void ApplyPath(string path)
            {
                if (settings.TrySetOutputPath(kind, path, out var normalized, out var error))
                {
                    pathField.SetValueWithoutNotify(normalized);
                    return;
                }

                EditorUtility.DisplayDialog("Invalid Output Path", error, "OK");
                pathField.SetValueWithoutNotify(settings.GetOutputPath(kind));
            }

            pathField.RegisterValueChangedCallback(evt => ApplyPath(evt.newValue));

            var browseButton = new Button(() =>
            {
                var currentPath = ShizukuGeneratorPathUtility.GetAbsolutePath(settings.GetOutputPath(kind));
                if (!Directory.Exists(currentPath))
                    currentPath = Application.dataPath;

                var selected = EditorUtility.OpenFolderPanel("Select Generator Output Folder", currentPath, string.Empty);
                if (!string.IsNullOrEmpty(selected))
                    ApplyPath(selected);
            })
            {
                text = "Browse",
                tooltip = "Select a folder inside this project's Assets folder."
            };
            browseButton.style.width = 64;
            browseButton.style.marginLeft = 4;

            var resetButton = new Button(() =>
            {
                settings.ResetOutputPath(kind);
                pathField.SetValueWithoutNotify(settings.GetOutputPath(kind));
            })
            {
                text = "Reset",
                tooltip = "Restore the default output path."
            };
            resetButton.style.width = 52;
            resetButton.style.marginLeft = 4;

            row.Add(label);
            row.Add(pathField);
            row.Add(browseButton);
            row.Add(resetButton);
            return row;
        }

        private static string GetLabel(GeneratorOutputPathKind kind)
        {
            switch (kind)
            {
                case GeneratorOutputPathKind.Blueprint:
                    return "Blueprint";
                case GeneratorOutputPathKind.FunctionNode:
                    return "Function Nodes";
                case GeneratorOutputPathKind.VariableNode:
                    return "Variable Nodes";
                case GeneratorOutputPathKind.PortType:
                    return "Port Types";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private static string GetElementName(GeneratorOutputPathKind kind)
        {
            return "generator-output-path-" + kind.ToString().ToLowerInvariant();
        }
    }
}
