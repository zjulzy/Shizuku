using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shizuku.Graph.Editor
{
    public partial class ShizukuGraphView
    {
        private Label _feedback;
        internal List<ParameterEdge> AuthoringEdges => CurrentEdges;
        internal bool RebuildingView => _isRebuildingView;

        internal void OpenAuthoringSearch(Vector2 graphPosition, Vector2 screenPosition, Port source = null) =>
            NodeAuthoringSearchWindow.Open(this, graphPosition, screenPosition, _runtimeGraph, source);

        internal Vector2 PanelToScreen(Vector2 position)
        {
            var window = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w => w.rootVisualElement.panel == panel);
            return window != null ? window.position.position + window.rootVisualElement.WorldToLocal(position) : GUIUtility.GUIToScreenPoint(position);
        }

        private void SetupAuthoring()
        {
            RegisterCallback<AttachToPanelEvent>(_ => { Undo.undoRedoPerformed -= OnAuthoringUndo; Undo.undoRedoPerformed += OnAuthoringUndo; });
            RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= OnAuthoringUndo);
            _feedback = new Label { name = "graph-feedback", pickingMode = PickingMode.Ignore };
            _feedback.style.position = Position.Absolute;
            _feedback.style.left = 12; _feedback.style.bottom = 12;
            _feedback.style.paddingLeft = _feedback.style.paddingRight = 10;
            _feedback.style.paddingTop = _feedback.style.paddingBottom = 6;
            _feedback.style.backgroundColor = new Color(.13f, .16f, .2f, .96f);
            _feedback.style.color = new Color(1f, .82f, .4f);
            _feedback.style.display = DisplayStyle.None;
            Add(_feedback);
        }

        private void OnAuthoringUndo()
        {
            if (!_runtimeGraph || Application.isPlaying) return;
            var methodGuid = _currentMethod?.GUID;
            var selectedGuid = selection.OfType<ShizukuNodeView>().FirstOrDefault()?.RuntimeNode.GUID;
            _runtimeGraph.Init();
            _currentMethod = methodGuid == null ? null : _runtimeGraph.Methods.FirstOrDefault(m => m.GUID == methodGuid);
            LoadCurrentContext();
            OnEditingContextChanged?.Invoke(_currentMethod);
            if (selectedGuid != null && _guidToNodeViewMap.TryGetValue(selectedGuid, out var selected))
            { AddToSelection(selected); OnNodeSelected?.Invoke(selected.RuntimeNode); }
            else OnNodeSelected?.Invoke(null);
            OnGraphChanged?.Invoke();
        }

        internal void ShowAuthoringFeedback(string message)
        {
            _feedback.text = message;
            _feedback.style.display = DisplayStyle.Flex;
            _feedback.BringToFront();
            schedule.Execute(() => { if (_feedback.text == message) _feedback.style.display = DisplayStyle.None; }).ExecuteLater(6500);
        }

        internal void FocusAuthoringNode(string guid)
        {
            if (!_guidToNodeViewMap.TryGetValue(guid, out var node)) return;
            ClearSelection(); AddToSelection(node); FrameSelection(); OnNodeSelected?.Invoke(node.RuntimeNode);
        }

        internal void RefreshAuthoringSummaries()
        {
            foreach (var node in nodes.OfType<ShizukuNodeView>()) node.RefreshAuthoringSummary();
        }

        internal void UpdateAuthoringGroup(
            GroupData group,
            string title,
            Unity.Mathematics.float4 positionAndSize)
        {
            if (!_runtimeGraph || _isRebuildingView || group == null)
                return;
            ExecuteGraphEdits(
                "编辑分组",
                new GraphEditOperation[]
                {
                    new UpdateGroupOperation(group.GUID, title, positionAndSize)
                });
        }

        public void ValidateAuthoringGraph()
        {
            if (!_runtimeGraph) return;
            foreach (var node in nodes.OfType<ShizukuNodeView>()) node.RefreshAuthoringSummary();
            var problems = CurrentNodes.SelectMany(n => NodeAuthoringUtility.Validate(n, CurrentEdges).Select(text => (n.GUID, text))).ToList();
            var dangling = CurrentEdges.Any(e => !CurrentNodes.Any(n => n.GUID == e.InputNodeGuid) || !CurrentNodes.Any(n => n.GUID == e.OutputNodeGuid));
            if (problems.Count > 0)
            { FocusAuthoringNode(problems[0].GUID); ShowAuthoringFeedback($"发现 {problems.Count} 项配置问题：{problems[0].text}"); }
            else ShowAuthoringFeedback(dangling ? "存在指向已删除节点的连线，请检查图数据。" : "当前图的必填项和数值范围检查通过（不代替运行验证）。");
        }

        internal void ConnectAuthoringPorts(Port first, Port second)
        {
            var input = first.direction == Direction.Input ? first : second;
            var output = first.direction == Direction.Output ? first : second;
            if (!GetCompatiblePorts(output, null).Contains(input))
            { ShowAuthoringFeedback("端口方向或类型不兼容，未创建连线。"); return; }
            var edge = new Edge { input = input, output = output };
            if (this.WouldCreateCycle(edge)) { ShowAuthoringFeedback("这条线会形成循环，未创建连线。"); return; }
            var replace = new List<Edge>();
            if (input.capacity == Port.Capacity.Single) replace.AddRange(input.connections);
            if (output.capacity == Port.Capacity.Single) replace.AddRange(output.connections);
            var change = OnGraphViewChanged(new GraphViewChange
            {
                edgesToCreate = new List<Edge> { edge },
                elementsToRemove = replace.Distinct().Cast<GraphElement>().ToList()
            });
            if (change.elementsToRemove?.Count > 0)
            {
                _isRebuildingView = true;
                try
                {
                    DeleteElements(change.elementsToRemove);
                }
                finally
                {
                    _isRebuildingView = false;
                }
            }
            foreach (var created in change.edgesToCreate)
            {
                if (!created.input.connections.Contains(created)) created.input.Connect(created);
                if (!created.output.connections.Contains(created)) created.output.Connect(created);
                if (created.parent == null) AddElement(created);
            }
        }
    }
}
