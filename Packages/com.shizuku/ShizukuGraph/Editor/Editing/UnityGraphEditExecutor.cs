using System;
using System.Collections.Generic;
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
    }

    /// <summary>
    /// GraphEditOperation 与 Unity Editor 状态之间的唯一适配层。
    /// Unity Undo 是唯一历史栈；Operation 只描述并执行一次向前修改。
    /// </summary>
    internal static class UnityGraphEditExecutor
    {
        internal static void Execute(
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
            if (operationList.Count == 0)
                return;

            var shouldRecordUndo = options.RecordUndo && !Application.isPlaying;
            var undoGroup = -1;
            if (shouldRecordUndo)
            {
                Undo.IncrementCurrentGroup();
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(undoName);
                Undo.RegisterCompleteObjectUndo(graph, undoName);
            }

            var backup = EditorJsonUtility.ToJson(graph);
            try
            {
                GraphEditService.Apply(graph, methodGuid, operationList);
                options.Validate?.Invoke(graph);
                EditorUtility.SetDirty(graph);
                if (options.SaveAsset)
                    AssetDatabase.SaveAssetIfDirty(graph);
                if (options.RefreshOpenGraph)
                    ShizukuGraphWindow.RefreshOpenGraph(graph);
            }
            catch
            {
                EditorJsonUtility.FromJsonOverwrite(backup, graph);
                graph.Init();
                EditorUtility.SetDirty(graph);
                throw;
            }
            finally
            {
                if (undoGroup >= 0)
                    Undo.CollapseUndoOperations(undoGroup);
            }
        }

        internal static void RecordSideEffects(ShizukuGraphBase graph, string undoName)
        {
            if (graph != null && !Application.isPlaying)
                Undo.RegisterCompleteObjectUndo(graph, undoName);
        }
    }
}
