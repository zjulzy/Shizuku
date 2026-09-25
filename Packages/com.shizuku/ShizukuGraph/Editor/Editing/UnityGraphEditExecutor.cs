using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Shizuku.Graph.Editor
{
    internal sealed class GraphEditExecutionOptions
    {
        internal bool RecordUndo { get; set; } = true;
        internal bool SaveAsset { get; set; }
        internal bool RefreshOpenGraph { get; set; }
        internal Action<ShizukuGraphBase> Validate { get; set; }
        internal Action<ShizukuGraphBase> Prepare { get; set; }
        internal Action<ShizukuGraphBase> Save { get; set; } = AssetDatabase.SaveAssetIfDirty;
        internal Action<ShizukuGraphBase> Refresh { get; set; } = ShizukuGraphWindow.RefreshOpenGraph;
    }

    /// <summary>
    /// GraphEditOperation 与 Unity Editor 状态之间的唯一适配层。
    /// Unity Undo 是唯一历史栈；Operation 只描述并执行一次向前修改。
    /// </summary>
    internal static class UnityGraphEditExecutor
    {
        internal static string Execute(
            ShizukuGraphBase graph,
            string methodGuid,
            IEnumerable<GraphEditOperation> operations,
            string undoName,
            GraphEditExecutionOptions options = null)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            options ??= new GraphEditExecutionOptions();
            var operationList = new List<GraphEditOperation>(
                operations ?? throw new ArgumentNullException(nameof(operations)));
            if (operationList.Count == 0 && options.Prepare == null)
                return null;

            var backup = EditorJsonUtility.ToJson(graph);
            var wasDirty = EditorUtility.IsDirty(graph);
            var assetPath = options.SaveAsset ? AssetDatabase.GetAssetPath(graph) : string.Empty;
            var diskBackup = string.IsNullOrEmpty(assetPath) ? null : File.ReadAllBytes(assetPath);
            var saveAttempted = false;

            var shouldRecordUndo = options.RecordUndo && !Application.isPlaying;
            var undoGroup = -1;
            if (shouldRecordUndo)
            {
                Undo.IncrementCurrentGroup();
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(undoName);
                Undo.RegisterCompleteObjectUndo(graph, undoName);
            }

            try
            {
                options.Prepare?.Invoke(graph);
                GraphEditService.Apply(graph, methodGuid, operationList);
                options.Validate?.Invoke(graph);
                EditorUtility.SetDirty(graph);
                if (options.SaveAsset)
                {
                    saveAttempted = true;
                    options.Save(graph);
                    if (EditorUtility.IsDirty(graph))
                        throw new IOException("Graph save did not complete; the asset is still dirty.");
                }
            }
            catch
            {
                if (undoGroup >= 0)
                    Undo.RevertAllDownToGroup(undoGroup);
                try
                {
                    if (saveAttempted && diskBackup != null)
                        File.WriteAllBytes(assetPath, diskBackup);
                }
                finally
                {
                    EditorJsonUtility.FromJsonOverwrite(backup, graph);
                    try { graph.Init(); }
                    finally
                    {
                        // Init may synchronize ports. Preserve the exact serialized pre-transaction state.
                        EditorJsonUtility.FromJsonOverwrite(backup, graph);
                        if (wasDirty) EditorUtility.SetDirty(graph);
                        else EditorUtility.ClearDirty(graph);
                    }
                }
                throw;
            }
            finally
            {
                if (undoGroup >= 0)
                    Undo.CollapseUndoOperations(undoGroup);
            }

            // The transaction has committed. A view failure must not masquerade as an apply failure.
            if (options.RefreshOpenGraph)
            {
                try { options.Refresh(graph); }
                catch (Exception exception)
                {
                    return "Graph edits committed, but refreshing the editor view failed: " + exception.Message;
                }
            }
            return null;
        }

        internal static void RecordSideEffects(ShizukuGraphBase graph, string undoName)
        {
            if (graph != null && !Application.isPlaying)
                Undo.RegisterCompleteObjectUndo(graph, undoName);
        }
    }
}
