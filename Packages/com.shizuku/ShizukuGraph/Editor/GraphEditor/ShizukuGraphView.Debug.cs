using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ShizukuGraphView 的调试功能部分
/// 负责：断点标记可视化、节点执行高亮、右键菜单断点操作、调试状态刷新
/// </summary>
namespace Shizuku.Graph.Editor
{
    using Shizuku.Graph;
    using Shizuku.Core;
    public partial class ShizukuGraphView
    {
        #region Debug 可视化

        /// <summary>
        /// 当前断点暂停高亮的节点视图
        /// </summary>
        private ShizukuNodeView _pausedHighlightNode;

        /// <summary>
        /// 上一帧执行过的节点高亮列表
        /// </summary>
        private readonly List<ShizukuNodeView> _executedHighlightNodes = new List<ShizukuNodeView>();

        /// <summary>
        /// 切换指定节点的断点状态，并刷新视觉标记
        /// </summary>
        public void ToggleBreakpoint(string nodeGuid)
        {
            bool hasBreakpoint = ShizukuDebugger.ToggleBreakpoint(nodeGuid);

            if (_guidToNodeViewMap.TryGetValue(nodeGuid, out var nodeView))
            {
                SetBreakpointVisual(nodeView, hasBreakpoint);
            }
        }

        /// <summary>
        /// 设置节点的断点视觉标记
        /// </summary>
        private void SetBreakpointVisual(ShizukuNodeView nodeView, bool hasBreakpoint)
        {
            const string breakpointBadgeName = "breakpoint-badge";
            var existing = nodeView.Q(breakpointBadgeName);

            if (hasBreakpoint)
            {
                if (existing == null)
                {
                    var badge = new Label("●")
                    {
                        name = breakpointBadgeName,
                        style =
                        {
                            color = new Color(1f, 0.2f, 0.2f, 1f),
                            fontSize = 18,
                            position = Position.Absolute,
                            left = -6,
                            top = 4,
                            unityFontStyleAndWeight = FontStyle.Bold
                        },
                        pickingMode = PickingMode.Ignore
                    };
                    nodeView.Add(badge);
                }
            }
            else
            {
                existing?.RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// 高亮断点暂停的节点（黄色边框）
        /// </summary>
        private void HighlightPausedNode(string nodeGuid)
        {
            ClearPausedHighlight();

            if (string.IsNullOrEmpty(nodeGuid)) return;
            if (!_guidToNodeViewMap.TryGetValue(nodeGuid, out var nodeView)) return;

            _pausedHighlightNode = nodeView;
            nodeView.AddToClassList("debug-paused");
        }

        /// <summary>
        /// 清除暂停高亮
        /// </summary>
        private void ClearPausedHighlight()
        {
            if (_pausedHighlightNode != null)
            {
                _pausedHighlightNode.RemoveFromClassList("debug-paused");
                _pausedHighlightNode = null;
            }
        }

        /// <summary>
        /// 高亮本帧执行过的节点（绿色闪烁）
        /// </summary>
        private void HighlightExecutedNodes(IReadOnlyList<string> executedGuids)
        {
            ClearExecutedHighlights();

            foreach (var guid in executedGuids)
            {
                if (_guidToNodeViewMap.TryGetValue(guid, out var nodeView))
                {
                    nodeView.AddToClassList("debug-executed");
                    _executedHighlightNodes.Add(nodeView);
                }
            }
        }

        /// <summary>
        /// 清除执行高亮
        /// </summary>
        private void ClearExecutedHighlights()
        {
            foreach (var nodeView in _executedHighlightNodes)
            {
                nodeView.RemoveFromClassList("debug-executed");
            }
            _executedHighlightNodes.Clear();
        }

        /// <summary>
        /// 刷新所有断点视觉标记（例如加载图时）
        /// </summary>
        public void RefreshAllBreakpointVisuals()
        {
            foreach (var kvp in _guidToNodeViewMap)
            {
                bool hasBp = ShizukuDebugger.HasBreakpoint(kvp.Key);
                SetBreakpointVisual(kvp.Value, hasBp);
            }
        }

        /// <summary>
        /// 由 EditorUpdate 定期调用，刷新调试可视化状态
        /// </summary>
        public void RefreshDebugVisuals()
        {
            if (!ShizukuDebugger.Enabled)
            {
                ClearPausedHighlight();
                ClearExecutedHighlights();
                return;
            }

            // 高亮执行过的节点
            HighlightExecutedNodes(ShizukuDebugger.ExecutedNodesLastFrame);

            // 如果暂停中，高亮当前暂停节点
            if (ShizukuDebugger.IsPaused && ShizukuDebugger.CurrentSnapshot != null)
            {
                HighlightPausedNode(ShizukuDebugger.CurrentSnapshot.PausedAtNodeGuid);
            }
            else
            {
                ClearPausedHighlight();
            }
        }

        /// <summary>
        /// 清除所有调试可视化（停止调试时调用）
        /// </summary>
        public void ClearAllDebugVisuals()
        {
            ClearPausedHighlight();
            ClearExecutedHighlights();
        }

        /// <summary>
        /// 聚焦到指定 GUID 的节点
        /// </summary>
        public void FocusOnNode(string nodeGuid)
        {
            if (string.IsNullOrEmpty(nodeGuid)) return;
            if (!_guidToNodeViewMap.TryGetValue(nodeGuid, out var nodeView)) return;

            ClearSelection();
            AddToSelection(nodeView);
            FrameSelection();
        }

        #endregion

        #region 右键菜单 - 断点操作

        /// <summary>
        /// 在 BuildContextualMenu 中追加调试相关菜单项
        /// 需要在主 ShizukuGraphView.cs 的 BuildContextualMenu 中调用
        /// </summary>
        public void BuildDebugContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // 查找是否右键点击在某个 RunnableNode 上
            var selectedNodeViews = selection.OfType<ShizukuNodeView>().ToList();
            if (selectedNodeViews.Count == 1)
            {
                var nodeView = selectedNodeViews[0];
                if (nodeView.RuntimeNode is ShizukuRunnableNode runnable)
                {
                    evt.menu.AppendSeparator();
                    bool hasBp = ShizukuDebugger.HasBreakpoint(runnable.GUID);
                    string label = hasBp ? "移除断点" : "设置断点";
                    evt.menu.AppendAction($"调试/{label}", _ =>
                    {
                        ToggleBreakpoint(runnable.GUID);
                    });
                }
            }
        }

        #endregion
    }


}
