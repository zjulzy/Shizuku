using System.Collections.Generic;
using System.Reflection;

namespace Shizuku.Graph
{
    internal static class GraphVariableReferenceUtility
    {
        private static readonly Dictionary<System.Type, FieldInfo> VariableGuidFields = new();

        public static int CountReferences(List<ShizukuNodeBase> nodes, string variableGuid)
        {
            if (nodes == null || string.IsNullOrEmpty(variableGuid))
                return 0;

            var count = 0;
            foreach (var node in nodes)
            {
                if (TryGetReferencedVariableGuid(node, out var referencedGuid) &&
                    referencedGuid == variableGuid)
                {
                    count++;
                }
            }

            return count;
        }

        public static int RemoveReferences(
            List<ShizukuNodeBase> nodes,
            List<ParameterEdge> edges,
            Dictionary<string, ShizukuNodeBase> nodeMap,
            Dictionary<string, ParameterEdge> edgeMap,
            string variableGuid)
        {
            if (nodes == null || string.IsNullOrEmpty(variableGuid))
                return 0;

            var removedNodeGuids = new HashSet<string>();
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if (!TryGetReferencedVariableGuid(nodes[i], out var referencedGuid) ||
                    referencedGuid != variableGuid)
                {
                    continue;
                }

                removedNodeGuids.Add(nodes[i].GUID);
                nodes.RemoveAt(i);
            }

            if (removedNodeGuids.Count == 0)
                return 0;

            foreach (var nodeGuid in removedNodeGuids)
                nodeMap?.Remove(nodeGuid);

            if (edges != null)
            {
                for (int i = edges.Count - 1; i >= 0; i--)
                {
                    var edge = edges[i];
                    if (edge == null ||
                        (!removedNodeGuids.Contains(edge.OutputNodeGuid) &&
                         !removedNodeGuids.Contains(edge.InputNodeGuid)))
                    {
                        continue;
                    }

                    edgeMap?.Remove(edge.GUID);
                    edges.RemoveAt(i);
                }
            }

            foreach (var node in nodes)
            {
                if (node is ShizukuNormalNode normalNode)
                    normalNode.ClearControlFlowReferencesTo(removedNodeGuids);
            }

            return removedNodeGuids.Count;
        }

        private static bool TryGetReferencedVariableGuid(ShizukuNodeBase node, out string variableGuid)
        {
            variableGuid = null;
            if (node is not IVariableNode)
                return false;

            var nodeType = node.GetType();
            if (!VariableGuidFields.TryGetValue(nodeType, out var field))
            {
                field = nodeType.GetField(
                    "VariableGUID",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                VariableGuidFields[nodeType] = field;
            }

            if (field == null || field.FieldType != typeof(string))
                return false;

            variableGuid = field.GetValue(node) as string;
            return true;
        }
    }
}
