using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shizuku.Graph.Editor
{
    /// <summary>
    /// 借助 Unity 的 SerializeReference 管线保存多态节点快照。
    /// </summary>
    internal sealed class ShizukuNodeClipboardContainer : ScriptableObject
    {
        [SerializeReference]
        public List<ShizukuNodeBase> Nodes = new List<ShizukuNodeBase>();
    }

    [Serializable]
    internal sealed class ShizukuNodeClipboardEnvelope
    {
        public int Version;
        public string SourceGraphId;
        public int NodeCount;
        public string SerializedNodes;
    }

    public partial class ShizukuGraphView
    {
        private const string ClipboardHeader = "SHIZUKU_GRAPH_NODES\n";
        private const int ClipboardVersion = 1;
        private const float ConsecutivePasteOffset = 20f;

        private string _lastPastedData;
        private Vector2 _lastPasteMousePosition;
        private int _consecutivePasteCount;

        private void ConfigureClipboard()
        {
            serializeGraphElements = SerializeSelectedNodes;
            canPasteSerializedData = CanPasteNodes;
            unserializeAndPaste = PasteSerializedNodes;

            // Ctrl+V 没有位置参数，持续记录鼠标在 GraphView 内容坐标中的位置。
            RegisterCallback<MouseMoveEvent>(evt =>
            {
                _localMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            });
        }

        /// <summary>
        /// 结构节点不是可复用的数据节点，复制会破坏图或函数的唯一入口/返回关系。
        /// </summary>
        internal static bool IsClipboardCopyableNode(ShizukuNodeBase node)
        {
            if (node == null) return false;
            if (node is ShizukuRootNode) return false;
            if (node is BlueprintReturnNode) return false;
            if (node is MethodReturnNode) return false;
            return true;
        }

        private string SerializeSelectedNodes(IEnumerable<GraphElement> elements)
        {
            if (_runtimeGraph == null || elements == null)
                return string.Empty;

            var selectedNodes = elements
                .OfType<ShizukuNodeView>()
                .Select(view => view.RuntimeNode)
                .Where(IsClipboardCopyableNode)
                .Distinct()
                .OrderBy(node => CurrentNodes.IndexOf(node))
                .ToList();

            if (selectedNodes.Count == 0)
                return string.Empty;

            var container = ScriptableObject.CreateInstance<ShizukuNodeClipboardContainer>();
            container.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                container.Nodes.AddRange(selectedNodes);
                var envelope = new ShizukuNodeClipboardEnvelope
                {
                    Version = ClipboardVersion,
                    SourceGraphId = GetClipboardGraphId(),
                    NodeCount = selectedNodes.Count,
                    SerializedNodes = EditorJsonUtility.ToJson(container)
                };

                _lastPastedData = null;
                _consecutivePasteCount = 0;
                return ClipboardHeader + JsonUtility.ToJson(envelope);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ShizukuGraph] 复制节点失败: {exception.Message}");
                return string.Empty;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
        }

        private bool CanPasteNodes(string data)
        {
            return _runtimeGraph != null
                && TryReadClipboardEnvelope(data, out var envelope)
                && envelope.Version == ClipboardVersion
                && envelope.NodeCount > 0
                && !string.IsNullOrEmpty(envelope.SerializedNodes)
                && string.Equals(envelope.SourceGraphId, GetClipboardGraphId(), StringComparison.Ordinal);
        }

        private void PasteSerializedNodes(string operationName, string data)
        {
            if (!CanPasteNodes(data) || !TryReadClipboardEnvelope(data, out var envelope))
                return;

            var container = ScriptableObject.CreateInstance<ShizukuNodeClipboardContainer>();
            container.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                EditorJsonUtility.FromJsonOverwrite(envelope.SerializedNodes, container);
                var nodes = container.Nodes
                    .Where(IsClipboardCopyableNode)
                    .ToList();

                if (nodes.Count == 0)
                    return;

                var sourceOrigin = new Vector2(
                    nodes.Min(node => node.PositionAndSize.x),
                    nodes.Min(node => node.PositionAndSize.y));
                var pasteOrigin = GetPasteOrigin(operationName, data, sourceOrigin);

                ClearSelection();

                foreach (var node in nodes)
                {
                    var relativePosition = new Vector2(
                        node.PositionAndSize.x - sourceOrigin.x,
                        node.PositionAndSize.y - sourceOrigin.y);
                    AddPastedNode(node, pasteOrigin + relativePosition);
                }

                // 节点现已由图数据持有，避免临时容器继续保留它们。
                container.Nodes.Clear();
                EditorUtility.SetDirty(_runtimeGraph);
                OnGraphChanged?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ShizukuGraph] 粘贴节点失败: {exception.Message}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
        }

        private void AddPastedNode(ShizukuNodeBase node, Vector2 position)
        {
            node.GUID = Guid.NewGuid().ToString();
            ClearNodeConnections(node);

            var width = node.PositionAndSize.z > 0f ? node.PositionAndSize.z : 200f;
            var height = node.PositionAndSize.w > 0f ? node.PositionAndSize.w : 100f;
            node.PositionAndSize = new float4(position.x, position.y, width, height);

            CurrentNodes.Add(node);

            var nodeView = new ShizukuNodeView(node, _runtimeGraph);
            nodeView.InitPort();
            nodeView.SetPosition(new Rect(position, new Vector2(width, height)));
            _guidToNodeViewMap[node.GUID] = nodeView;
            AddElement(nodeView);
            AddToSelection(nodeView);
        }

        private Vector2 GetPasteOrigin(string operationName, string data, Vector2 sourceOrigin)
        {
            if (!string.IsNullOrEmpty(operationName)
                && operationName.IndexOf("Duplicate", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return sourceOrigin + Vector2.one * ConsecutivePasteOffset;
            }

            var samePasteLocation = string.Equals(_lastPastedData, data, StringComparison.Ordinal)
                && Vector2.SqrMagnitude(_lastPasteMousePosition - _localMousePosition) < 0.01f;
            _consecutivePasteCount = samePasteLocation ? _consecutivePasteCount + 1 : 0;
            _lastPastedData = data;
            _lastPasteMousePosition = _localMousePosition;

            return _localMousePosition + Vector2.one * (ConsecutivePasteOffset * _consecutivePasteCount);
        }

        private string GetClipboardGraphId()
        {
            if (_runtimeGraph == null)
                return string.Empty;

            var assetPath = AssetDatabase.GetAssetPath(_runtimeGraph);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                if (!string.IsNullOrEmpty(assetGuid))
                    return "asset:" + assetGuid;
            }

            return "instance:" + _runtimeGraph.GetInstanceID();
        }

        private static bool TryReadClipboardEnvelope(string data, out ShizukuNodeClipboardEnvelope envelope)
        {
            envelope = null;
            if (string.IsNullOrEmpty(data) || !data.StartsWith(ClipboardHeader, StringComparison.Ordinal))
                return false;

            try
            {
                envelope = JsonUtility.FromJson<ShizukuNodeClipboardEnvelope>(
                    data.Substring(ClipboardHeader.Length));
                return envelope != null;
            }
            catch
            {
                return false;
            }
        }

        private static void ClearNodeConnections(ShizukuNodeBase node)
        {
            node.SelfOutputPorts.Clear();
            node.SelfInputPorts.Clear();
            node.DependentNodes.Clear();

            if (node is ShizukuNormalNode normalNode)
                normalNode.ChainPorts?.Clear();

            ClearConnectionsRecursive(node, new HashSet<object>(ReferenceComparer.Instance));
        }

        private static void ClearConnectionsRecursive(object value, HashSet<object> visited)
        {
            if (value == null || value is string || value is UnityEngine.Object || value is Delegate)
                return;

            var valueType = value.GetType();
            if (valueType.IsPrimitive || valueType.IsEnum || valueType.IsValueType)
                return;

            if (!visited.Add(value))
                return;

            if (value is ChainPort chainPort)
            {
                chainPort.NextNodeGuid = null;
                return;
            }

            if (value is ParameterEdgePort parameterPort)
            {
                parameterPort.InputEdgeGUID = null;
                parameterPort.SameTypeConnectedPort = null;
                parameterPort.DifferentTypeConnectedPort = null;
                return;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                    ClearConnectionsRecursive(item, visited);
                return;
            }

            for (var type = valueType; type != null && type != typeof(object); type = type.BaseType)
            {
                var fields = type.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (var field in fields)
                {
                    if (field.IsStatic || field.IsNotSerialized)
                        continue;

                    ClearConnectionsRecursive(field.GetValue(value), visited);
                }
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
