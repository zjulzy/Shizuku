using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Shizuku.Graph.Editor
{
    internal enum GeneratorOutputPathKind
    {
        Blueprint,
        FunctionNode,
        VariableNode,
        PortType
    }

    [FilePath("ProjectSettings/ShizukuGeneratorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class ShizukuGeneratorSettings : ScriptableSingleton<ShizukuGeneratorSettings>
    {
        internal const string DefaultBlueprintOutputPath = "Assets/Scripts/Graph/Blueprint/Generated";
        internal const string DefaultFunctionNodeOutputPath = "Assets/Scripts/Node/DerivedNodes/Generated";
        internal const string DefaultVariableNodeOutputPath = "Assets/Scripts/Node/VariableNodes/Generated";
        internal const string DefaultPortTypeOutputPath = "Assets/Scripts/Node/Generated";

        [SerializeField] private string _blueprintOutputPath = DefaultBlueprintOutputPath;
        [SerializeField] private string _functionNodeOutputPath = DefaultFunctionNodeOutputPath;
        [SerializeField] private string _variableNodeOutputPath = DefaultVariableNodeOutputPath;
        [SerializeField] private string _portTypeOutputPath = DefaultPortTypeOutputPath;

        internal string GetOutputPath(GeneratorOutputPathKind kind)
        {
            var storedPath = GetStoredPath(kind);
            if (ShizukuGeneratorPathUtility.TryNormalizeAssetFolderPath(storedPath, out var normalized, out _))
                return normalized;

            return GetDefaultOutputPath(kind);
        }

        internal bool TrySetOutputPath(
            GeneratorOutputPathKind kind,
            string path,
            out string normalized,
            out string error)
        {
            if (!ShizukuGeneratorPathUtility.TryNormalizeAssetFolderPath(path, out normalized, out error))
                return false;

            SetStoredPath(kind, normalized);
            Save(true);
            return true;
        }

        internal void ResetOutputPath(GeneratorOutputPathKind kind)
        {
            SetStoredPath(kind, GetDefaultOutputPath(kind));
            Save(true);
        }

        internal static string GetDefaultOutputPath(GeneratorOutputPathKind kind)
        {
            switch (kind)
            {
                case GeneratorOutputPathKind.Blueprint:
                    return DefaultBlueprintOutputPath;
                case GeneratorOutputPathKind.FunctionNode:
                    return DefaultFunctionNodeOutputPath;
                case GeneratorOutputPathKind.VariableNode:
                    return DefaultVariableNodeOutputPath;
                case GeneratorOutputPathKind.PortType:
                    return DefaultPortTypeOutputPath;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private string GetStoredPath(GeneratorOutputPathKind kind)
        {
            switch (kind)
            {
                case GeneratorOutputPathKind.Blueprint:
                    return _blueprintOutputPath;
                case GeneratorOutputPathKind.FunctionNode:
                    return _functionNodeOutputPath;
                case GeneratorOutputPathKind.VariableNode:
                    return _variableNodeOutputPath;
                case GeneratorOutputPathKind.PortType:
                    return _portTypeOutputPath;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private void SetStoredPath(GeneratorOutputPathKind kind, string value)
        {
            switch (kind)
            {
                case GeneratorOutputPathKind.Blueprint:
                    _blueprintOutputPath = value;
                    break;
                case GeneratorOutputPathKind.FunctionNode:
                    _functionNodeOutputPath = value;
                    break;
                case GeneratorOutputPathKind.VariableNode:
                    _variableNodeOutputPath = value;
                    break;
                case GeneratorOutputPathKind.PortType:
                    _portTypeOutputPath = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }

    internal static class ShizukuGeneratorPathUtility
    {
        internal static bool TryNormalizeAssetFolderPath(
            string rawPath,
            out string normalized,
            out string error)
        {
            return TryNormalizeAssetFolderPath(rawPath, Application.dataPath, out normalized, out error);
        }

        internal static bool TryNormalizeAssetFolderPath(
            string rawPath,
            string projectAssetsPath,
            out string normalized,
            out string error)
        {
            normalized = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawPath))
            {
                error = "Output path cannot be empty.";
                return false;
            }

            try
            {
                var assetsRoot = Path.GetFullPath(projectAssetsPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var projectRoot = Directory.GetParent(assetsRoot)?.FullName;
                if (string.IsNullOrEmpty(projectRoot))
                {
                    error = "Cannot resolve the Unity project root.";
                    return false;
                }

                var platformPath = rawPath.Trim()
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
                var absolutePath = Path.IsPathRooted(platformPath)
                    ? Path.GetFullPath(platformPath)
                    : Path.GetFullPath(Path.Combine(projectRoot, platformPath));
                absolutePath = absolutePath.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

                var isAssetsRoot = string.Equals(
                    absolutePath,
                    assetsRoot,
                    StringComparison.OrdinalIgnoreCase);
                var isAssetsChild = absolutePath.StartsWith(
                    assetsRoot + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase);

                if (!isAssetsRoot && !isAssetsChild)
                {
                    error = "Output path must be inside this project's Assets folder.";
                    return false;
                }

                normalized = isAssetsRoot
                    ? "Assets"
                    : "Assets/" + absolutePath.Substring(assetsRoot.Length + 1)
                        .Replace('\\', '/')
                        .TrimEnd('/');
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException)
            {
                error = $"Invalid output path: {exception.Message}";
                return false;
            }
        }

        internal static string ResolveOutputFilePath(string fileName, string outputDirectory)
        {
            var searchName = Path.GetFileNameWithoutExtension(fileName)
                .Replace(".Generated", string.Empty);
            var existingPaths = AssetDatabase.FindAssets($"{searchName} t:Script", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath);

            return ResolveOutputFilePath(fileName, outputDirectory, existingPaths);
        }

        internal static string ResolveOutputFilePath(
            string fileName,
            string outputDirectory,
            IEnumerable<string> existingAssetPaths)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
                throw new ArgumentException("A plain file name is required.", nameof(fileName));

            var matches = (existingAssetPaths ?? Enumerable.Empty<string>())
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(NormalizeAssetPath)
                .Where(path => path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(path, "Assets", StringComparison.OrdinalIgnoreCase))
                .Where(path => string.Equals(
                    Path.GetFileName(path),
                    fileName,
                    StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (matches.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Multiple generated files named '{fileName}' were found. " +
                    "Remove the duplicate before regenerating.");
            }

            return matches.Count == 1
                ? matches[0]
                : CombineAssetPath(outputDirectory, fileName);
        }

        internal static string FindExistingGeneratedFilePath(string fileName)
        {
            var resolved = ResolveOutputFilePath(
                fileName,
                ShizukuGeneratorSettings.DefaultFunctionNodeOutputPath);
            return AssetDatabase.LoadAssetAtPath<MonoScript>(resolved) != null ? resolved : null;
        }

        internal static string CombineAssetPath(string directory, string fileName)
        {
            return NormalizeAssetPath(Path.Combine(directory, fileName));
        }

        internal static string GetAbsolutePath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return string.IsNullOrEmpty(projectRoot)
                ? Application.dataPath
                : Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        internal static void EnsureOutputDirectoryExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private static string NormalizeAssetPath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
