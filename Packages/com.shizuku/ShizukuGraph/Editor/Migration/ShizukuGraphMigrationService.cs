using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Shizuku.Graph.Editor
{
    public enum GraphAssetCompatibilityStatus
    {
        Ready,
        UpgradeRequired,
        Migrated,
        MissingManagedReferenceTypes,
        NewerSchemaVersion,
        MigrationFailed
    }

    public sealed class MissingManagedReferenceDiagnostic
    {
        internal MissingManagedReferenceDiagnostic(ManagedReferenceMissingType missingType)
        {
            ReferenceId = missingType.referenceId;
            AssemblyName = missingType.assemblyName ?? string.Empty;
            NamespaceName = missingType.namespaceName ?? string.Empty;
            ClassName = missingType.className ?? string.Empty;
            SerializedData = missingType.serializedData ?? string.Empty;
        }

        public long ReferenceId { get; }
        public string AssemblyName { get; }
        public string NamespaceName { get; }
        public string ClassName { get; }
        public string SerializedData { get; }

        public string QualifiedTypeName =>
            $"{(string.IsNullOrEmpty(NamespaceName) ? string.Empty : NamespaceName + ".")}{ClassName}, {AssemblyName}";
    }

    public sealed class GraphAssetCompatibilityReport
    {
        internal GraphAssetCompatibilityReport(
            ShizukuGraphBase graph,
            GraphAssetCompatibilityStatus status,
            int sourceVersion,
            string message,
            IReadOnlyList<MissingManagedReferenceDiagnostic> missingTypes = null)
        {
            Graph = graph;
            Status = status;
            SourceVersion = sourceVersion;
            Message = message ?? string.Empty;
            MissingTypes = missingTypes ?? Array.Empty<MissingManagedReferenceDiagnostic>();
        }

        public ShizukuGraphBase Graph { get; }
        public GraphAssetCompatibilityStatus Status { get; }
        public int SourceVersion { get; }
        public int TargetVersion => ShizukuGraphBase.CurrentSchemaVersion;
        public string Message { get; }
        public IReadOnlyList<MissingManagedReferenceDiagnostic> MissingTypes { get; }
        public bool CanOpen => Status == GraphAssetCompatibilityStatus.Ready ||
                               Status == GraphAssetCompatibilityStatus.Migrated;

        public string BuildDetailedMessage()
        {
            var builder = new StringBuilder(Message);
            foreach (var missingType in MissingTypes)
            {
                builder.AppendLine()
                    .Append("- ")
                    .Append(missingType.QualifiedTypeName)
                    .Append(" (referenceId: ")
                    .Append(missingType.ReferenceId)
                    .Append(')');

                if (!string.IsNullOrWhiteSpace(missingType.SerializedData))
                {
                    builder.AppendLine()
                        .Append("  serializedData: ")
                        .Append(missingType.SerializedData);
                }
            }

            return builder.ToString();
        }
    }

    public sealed class GraphAssetCompatibilityException : InvalidOperationException
    {
        internal GraphAssetCompatibilityException(GraphAssetCompatibilityReport report)
            : base(report?.BuildDetailedMessage())
        {
            Report = report;
        }

        public GraphAssetCompatibilityReport Report { get; }
    }

    internal interface IShizukuGraphMigration
    {
        int SourceVersion { get; }
        int TargetVersion { get; }
        void Migrate(ShizukuGraphBase graph);
    }

    /// <summary>
    /// 图资产格式迁移的唯一入口。迁移只负责 Shizuku 图容器格式，
    /// 业务节点自身的字段/类型兼容仍由 FormerlySerializedAs、MovedFrom 或业务迁移代码负责。
    /// </summary>
    internal static class ShizukuGraphMigrationService
    {
        private static readonly IReadOnlyDictionary<int, IShizukuGraphMigration> Migrations =
            new IShizukuGraphMigration[]
            {
                new UnversionedGraphToV1Migration()
            }.ToDictionary(migration => migration.SourceVersion);

        internal static GraphAssetCompatibilityReport Inspect(ShizukuGraphBase graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            var missingTypes = SerializationUtility.GetManagedReferencesWithMissingTypes(graph)
                .Select(missingType => new MissingManagedReferenceDiagnostic(missingType))
                .ToArray();
            if (missingTypes.Length > 0)
            {
                return new GraphAssetCompatibilityReport(
                    graph,
                    GraphAssetCompatibilityStatus.MissingManagedReferenceTypes,
                    graph.SchemaVersion,
                    "图资产包含无法反序列化的类型。迁移和编辑已停止，原始序列化数据未被清理。" +
                    "请恢复程序集、使用 MovedFrom，或提供明确的节点替换迁移。",
                    missingTypes);
            }

            if (graph.SchemaVersion > ShizukuGraphBase.CurrentSchemaVersion)
            {
                return new GraphAssetCompatibilityReport(
                    graph,
                    GraphAssetCompatibilityStatus.NewerSchemaVersion,
                    graph.SchemaVersion,
                    $"图资产版本 {graph.SchemaVersion} 高于当前框架支持的版本 " +
                    $"{ShizukuGraphBase.CurrentSchemaVersion}，已拒绝使用旧框架打开。");
            }

            if (graph.SchemaVersion < 0)
            {
                return new GraphAssetCompatibilityReport(
                    graph,
                    GraphAssetCompatibilityStatus.MigrationFailed,
                    graph.SchemaVersion,
                    $"图资产包含非法的 SchemaVersion：{graph.SchemaVersion}。");
            }

            return graph.SchemaVersion < ShizukuGraphBase.CurrentSchemaVersion
                ? new GraphAssetCompatibilityReport(
                    graph,
                    GraphAssetCompatibilityStatus.UpgradeRequired,
                    graph.SchemaVersion,
                    $"图资产需要从版本 {graph.SchemaVersion} 升级到 " +
                    $"{ShizukuGraphBase.CurrentSchemaVersion}。")
                : new GraphAssetCompatibilityReport(
                    graph,
                    GraphAssetCompatibilityStatus.Ready,
                    graph.SchemaVersion,
                    $"图资产 SchemaVersion {graph.SchemaVersion} 已是当前版本。");
        }

        internal static GraphAssetCompatibilityReport EnsureCurrent(ShizukuGraphBase graph)
        {
            var inspection = Inspect(graph);
            if (inspection.Status != GraphAssetCompatibilityStatus.UpgradeRequired)
                return inspection;

            var sourceVersion = graph.SchemaVersion;
            var backup = EditorJsonUtility.ToJson(graph);
            var wasDirty = EditorUtility.IsDirty(graph);
            var assetPath = AssetDatabase.GetAssetPath(graph);
            if (wasDirty && !string.IsNullOrEmpty(assetPath))
            {
                return new GraphAssetCompatibilityReport(
                    graph,
                    GraphAssetCompatibilityStatus.MigrationFailed,
                    sourceVersion,
                    "图资产存在尚未保存的修改，自动迁移已停止。请先保存或还原资产后重试。");
            }

            var absoluteAssetPath = string.IsNullOrEmpty(assetPath)
                ? string.Empty
                : Path.GetFullPath(assetPath);
            var diskBackup = string.IsNullOrEmpty(absoluteAssetPath)
                ? null
                : File.ReadAllBytes(absoluteAssetPath);
            var savedToDisk = false;

            try
            {
                var version = sourceVersion;
                while (version < ShizukuGraphBase.CurrentSchemaVersion)
                {
                    if (!Migrations.TryGetValue(version, out var migration) ||
                        migration.TargetVersion <= version)
                    {
                        throw new InvalidOperationException(
                            $"没有可用的 Shizuku 图迁移步骤：{version} -> " +
                            $"{ShizukuGraphBase.CurrentSchemaVersion}。");
                    }

                    migration.Migrate(graph);
                    version = migration.TargetVersion;
                    SetSchemaVersion(graph, version);
                }

                var postMigrationInspection = Inspect(graph);
                if (postMigrationInspection.Status != GraphAssetCompatibilityStatus.Ready)
                    throw new InvalidOperationException(postMigrationInspection.BuildDetailedMessage());

                EditorUtility.SetDirty(graph);
                var migratedGraph = graph;
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.SaveAssetIfDirty(graph);
                    savedToDisk = true;
                    AssetDatabase.ImportAsset(
                        assetPath,
                        ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    migratedGraph = AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(assetPath);
                    if (migratedGraph == null ||
                        migratedGraph.SchemaVersion != ShizukuGraphBase.CurrentSchemaVersion)
                    {
                        throw new InvalidOperationException("图资产保存并重新导入后未保持目标 SchemaVersion。");
                    }
                }

                Debug.Log(
                    $"[ShizukuGraph] 已将图资产 '{graph.name}' 从 SchemaVersion {sourceVersion} " +
                    $"迁移到 {ShizukuGraphBase.CurrentSchemaVersion}。",
                    migratedGraph);

                return new GraphAssetCompatibilityReport(
                    migratedGraph,
                    GraphAssetCompatibilityStatus.Migrated,
                    sourceVersion,
                    $"图资产已从 SchemaVersion {sourceVersion} 自动迁移到 " +
                    $"{ShizukuGraphBase.CurrentSchemaVersion}，并完成重新导入。");
            }
            catch (Exception exception)
            {
                if (savedToDisk && diskBackup != null)
                {
                    File.WriteAllBytes(absoluteAssetPath, diskBackup);
                    AssetDatabase.ImportAsset(
                        assetPath,
                        ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }

                EditorJsonUtility.FromJsonOverwrite(backup, graph);
                if (!wasDirty)
                    EditorUtility.ClearDirty(graph);

                return new GraphAssetCompatibilityReport(
                    graph,
                    GraphAssetCompatibilityStatus.MigrationFailed,
                    sourceVersion,
                    $"图资产从 SchemaVersion {sourceVersion} 迁移失败，内存数据已回滚且没有保存：" +
                    exception.Message);
            }
        }

        private static void SetSchemaVersion(ShizukuGraphBase graph, int version)
        {
            var serializedGraph = new SerializedObject(graph);
            serializedGraph.Update();
            var versionProperty = serializedGraph.FindProperty("_schemaVersion")
                ?? throw new InvalidOperationException("找不到图资产的 _schemaVersion 序列化字段。");
            versionProperty.intValue = version;
            serializedGraph.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class UnversionedGraphToV1Migration : IShizukuGraphMigration
        {
            public int SourceVersion => 0;
            public int TargetVersion => 1;

            public void Migrate(ShizukuGraphBase graph)
            {
                // v0.3.0 - v0.8.0 没有独立版本字段，其现有容器结构即为 v1 基线。
                // 该迁移只建立显式版本，不改写节点、边、变量或方法数据。
            }
        }
    }
}
