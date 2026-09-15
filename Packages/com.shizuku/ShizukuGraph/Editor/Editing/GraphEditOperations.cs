using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEditor;

namespace Shizuku.Graph.Editor
{
    /// <summary>
    /// 一次性的图编辑意图。Operation 不保存历史，也不实现 Undo / Redo。
    /// </summary>
    internal abstract class GraphEditOperation
    {
    }

    internal sealed class CreateNodeOperation : GraphEditOperation
    {
        internal CreateNodeOperation(
            ShizukuNodeBase node,
            float4 positionAndSize,
            JObject fields = null,
            bool assignRootIfEmpty = true)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            PositionAndSize = positionAndSize;
            Fields = fields;
            AssignRootIfEmpty = assignRootIfEmpty;
        }

        internal ShizukuNodeBase Node { get; }
        internal float4 PositionAndSize { get; }
        internal JObject Fields { get; }
        internal bool AssignRootIfEmpty { get; }
    }

    internal sealed class DeleteNodesOperation : GraphEditOperation
    {
        internal DeleteNodesOperation(IEnumerable<string> nodeGuids)
        {
            NodeGuids = (nodeGuids ?? throw new ArgumentNullException(nameof(nodeGuids)))
                .Where(guid => !string.IsNullOrWhiteSpace(guid))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        internal IReadOnlyList<string> NodeGuids { get; }
    }

    internal sealed class MoveNodeOperation : GraphEditOperation
    {
        internal MoveNodeOperation(string nodeGuid, float4 positionAndSize)
        {
            NodeGuid = nodeGuid;
            PositionAndSize = positionAndSize;
        }

        internal string NodeGuid { get; }
        internal float4 PositionAndSize { get; }
    }

    internal sealed class SetNodeFieldsOperation : GraphEditOperation
    {
        internal SetNodeFieldsOperation(string nodeGuid, JObject fields)
        {
            NodeGuid = nodeGuid;
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        internal string NodeGuid { get; }
        internal JObject Fields { get; }
    }

    internal sealed class ConnectControlOperation : GraphEditOperation
    {
        internal ConnectControlOperation(string sourceGuid, string portName, string targetGuid)
        {
            SourceGuid = sourceGuid;
            PortName = portName;
            TargetGuid = targetGuid;
        }

        internal string SourceGuid { get; }
        internal string PortName { get; }
        internal string TargetGuid { get; }
    }

    internal sealed class DisconnectControlOperation : GraphEditOperation
    {
        internal DisconnectControlOperation(string sourceGuid, string portName, string targetGuid = null)
        {
            SourceGuid = sourceGuid;
            PortName = portName;
            TargetGuid = targetGuid;
        }

        internal string SourceGuid { get; }
        internal string PortName { get; }
        internal string TargetGuid { get; }
    }

    internal sealed class ConnectParameterOperation : GraphEditOperation
    {
        internal ConnectParameterOperation(
            string sourceGuid,
            string outputPortName,
            string targetGuid,
            string inputPortName,
            string edgeGuid = null)
        {
            SourceGuid = sourceGuid;
            OutputPortName = outputPortName;
            TargetGuid = targetGuid;
            InputPortName = inputPortName;
            EdgeGuid = string.IsNullOrWhiteSpace(edgeGuid) ? Guid.NewGuid().ToString() : edgeGuid;
        }

        internal string SourceGuid { get; }
        internal string OutputPortName { get; }
        internal string TargetGuid { get; }
        internal string InputPortName { get; }
        internal string EdgeGuid { get; }
    }

    internal sealed class DisconnectParameterOperation : GraphEditOperation
    {
        internal DisconnectParameterOperation(
            string edgeGuid = null,
            string sourceGuid = null,
            string outputPortName = null,
            string targetGuid = null,
            string inputPortName = null)
        {
            EdgeGuid = edgeGuid;
            SourceGuid = sourceGuid;
            OutputPortName = outputPortName;
            TargetGuid = targetGuid;
            InputPortName = inputPortName;
        }

        internal string EdgeGuid { get; }
        internal string SourceGuid { get; }
        internal string OutputPortName { get; }
        internal string TargetGuid { get; }
        internal string InputPortName { get; }
    }

    internal sealed class AddGroupOperation : GraphEditOperation
    {
        internal AddGroupOperation(GroupData group)
        {
            Group = group ?? throw new ArgumentNullException(nameof(group));
        }

        internal GroupData Group { get; }
    }

    internal sealed class DeleteGroupsOperation : GraphEditOperation
    {
        internal DeleteGroupsOperation(IEnumerable<string> groupGuids)
        {
            GroupGuids = (groupGuids ?? throw new ArgumentNullException(nameof(groupGuids)))
                .Where(guid => !string.IsNullOrWhiteSpace(guid))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        internal IReadOnlyList<string> GroupGuids { get; }
    }

    internal sealed class UpdateGroupOperation : GraphEditOperation
    {
        internal UpdateGroupOperation(string groupGuid, string title, float4 positionAndSize)
        {
            GroupGuid = groupGuid;
            Title = title;
            PositionAndSize = positionAndSize;
        }

        internal string GroupGuid { get; }
        internal string Title { get; }
        internal float4 PositionAndSize { get; }
    }

    internal sealed class ClearGraphContextOperation : GraphEditOperation
    {
    }

    internal sealed class GraphEditContext
    {
        private GraphEditContext(ShizukuGraphBase graph, ShizukuMethod method)
        {
            Graph = graph;
            Method = method;
            NodeContext = method ?? (INodeContext)graph;
            Nodes = method?.Nodes ?? graph.Nodes;
            Edges = method?.Edges ?? graph.Edges;
            Groups = method?.Groups ?? graph.Groups;

            foreach (var node in Nodes.Where(node => node != null && !string.IsNullOrEmpty(node.GUID)))
                NodeContext.Guid2NodeMap[node.GUID] = node;
            foreach (var edge in Edges.Where(edge => edge != null && !string.IsNullOrEmpty(edge.GUID)))
                NodeContext.Guid2EdgeMap[edge.GUID] = edge;
        }

        internal ShizukuGraphBase Graph { get; }
        internal ShizukuMethod Method { get; }
        internal INodeContext NodeContext { get; }
        internal List<ShizukuNodeBase> Nodes { get; }
        internal List<ParameterEdge> Edges { get; }
        internal List<GroupData> Groups { get; }

        internal static GraphEditContext Create(ShizukuGraphBase graph, string methodGuid)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            if (string.IsNullOrWhiteSpace(methodGuid))
                return new GraphEditContext(graph, null);

            var method = graph.Methods.FirstOrDefault(candidate =>
                candidate != null && candidate.GUID == methodGuid)
                ?? throw new ArgumentException($"Shizuku method does not exist: {methodGuid}");
            return new GraphEditContext(graph, method);
        }

        internal ShizukuNodeBase RequireNode(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                throw new ArgumentException("Node GUID is required.");
            return Nodes.FirstOrDefault(node => node != null && node.GUID == guid)
                ?? throw new ArgumentException($"Node does not exist in the selected graph context: {guid}");
        }
    }

    /// <summary>
    /// 无状态的图数据修改器。它不记录 Undo、不保存资产，也不刷新 GraphView。
    /// </summary>
    internal static class GraphEditService
    {
        internal static void Apply(
            ShizukuGraphBase graph,
            string methodGuid,
            IEnumerable<GraphEditOperation> operations)
        {
            var context = GraphEditContext.Create(graph, methodGuid);
            foreach (var operation in operations ?? throw new ArgumentNullException(nameof(operations)))
            {
                if (operation == null)
                    throw new ArgumentException("Graph edit operation cannot be null.");

                switch (operation)
                {
                    case CreateNodeOperation create:
                        ApplyCreateNode(context, create);
                        break;
                    case DeleteNodesOperation delete:
                        ApplyDeleteNodes(context, delete);
                        break;
                    case MoveNodeOperation move:
                        ApplyMoveNode(context, move);
                        break;
                    case SetNodeFieldsOperation setFields:
                        ApplySetNodeFields(context, setFields);
                        break;
                    case ConnectControlOperation connectControl:
                        ApplyConnectControl(context, connectControl);
                        break;
                    case DisconnectControlOperation disconnectControl:
                        ApplyDisconnectControl(context, disconnectControl);
                        break;
                    case ConnectParameterOperation connectParameter:
                        ApplyConnectParameter(context, connectParameter);
                        break;
                    case DisconnectParameterOperation disconnectParameter:
                        ApplyDisconnectParameter(context, disconnectParameter);
                        break;
                    case AddGroupOperation addGroup:
                        ApplyAddGroup(context, addGroup);
                        break;
                    case DeleteGroupsOperation deleteGroups:
                        ApplyDeleteGroups(context, deleteGroups);
                        break;
                    case UpdateGroupOperation updateGroup:
                        ApplyUpdateGroup(context, updateGroup);
                        break;
                    case ClearGraphContextOperation:
                        ApplyClearContext(context);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported graph edit operation: {operation.GetType().Name}");
                }
            }

            RebuildContextConnections(context);
        }

        private static void ApplyCreateNode(GraphEditContext context, CreateNodeOperation operation)
        {
            var node = operation.Node;
            if (string.IsNullOrWhiteSpace(node.GUID))
                throw new InvalidOperationException("Created node requires a GUID.");
            if (context.Nodes.Any(existing => existing != null && existing.GUID == node.GUID))
                throw new InvalidOperationException($"Node GUID already exists: {node.GUID}");
            if (context.Method != null && node is ShizukuLatentNode)
                throw new InvalidOperationException("Shizuku methods do not support latent nodes.");

            node.PositionAndSize = operation.PositionAndSize;
            context.Nodes.Add(node);
            context.NodeContext.Guid2NodeMap[node.GUID] = node;

            if (operation.Fields != null)
                OverwriteNodeFields(node, context.NodeContext, operation.Fields);

            if (context.Method == null && operation.AssignRootIfEmpty &&
                node.GetType() == typeof(ShizukuRootNode) &&
                string.IsNullOrEmpty(context.Graph.RootNodeGUID))
            {
                context.Graph.RootNodeGUID = node.GUID;
            }
        }

        private static void ApplyDeleteNodes(GraphEditContext context, DeleteNodesOperation operation)
        {
            if (operation.NodeGuids.Count == 0)
                return;

            var guids = new HashSet<string>(operation.NodeGuids, StringComparer.Ordinal);
            foreach (var guid in guids)
                context.RequireNode(guid);

            context.Nodes.RemoveAll(node => node != null && guids.Contains(node.GUID));
            foreach (var guid in guids)
                context.NodeContext.Guid2NodeMap.Remove(guid);

            var removedEdges = context.Edges
                .Where(edge => edge == null || guids.Contains(edge.OutputNodeGuid) || guids.Contains(edge.InputNodeGuid))
                .ToArray();
            context.Edges.RemoveAll(edge => edge == null || guids.Contains(edge.OutputNodeGuid) || guids.Contains(edge.InputNodeGuid));
            foreach (var edge in removedEdges.Where(edge => edge != null))
                context.NodeContext.Guid2EdgeMap.Remove(edge.GUID);

            foreach (var normalNode in context.Nodes.OfType<ShizukuNormalNode>())
            {
                foreach (var port in GraphEditReflection.GetChainPorts(normalNode))
                {
                    if (guids.Contains(port.NextNodeGuid))
                        port.NextNodeGuid = null;
                }
            }

            if (context.Method == null && guids.Contains(context.Graph.RootNodeGUID))
                context.Graph.RootNodeGUID = null;
            if (context.Method != null)
            {
                if (guids.Contains(context.Method.EntryNodeGUID))
                    context.Method.EntryNodeGUID = null;
                if (guids.Contains(context.Method.ReturnNodeGUID))
                    context.Method.ReturnNodeGUID = null;
            }
        }

        private static void ApplyMoveNode(GraphEditContext context, MoveNodeOperation operation)
        {
            context.RequireNode(operation.NodeGuid).PositionAndSize = operation.PositionAndSize;
        }

        private static void ApplySetNodeFields(GraphEditContext context, SetNodeFieldsOperation operation)
        {
            OverwriteNodeFields(context.RequireNode(operation.NodeGuid), context.NodeContext, operation.Fields);
        }

        private static void ApplyConnectControl(GraphEditContext context, ConnectControlOperation operation)
        {
            var source = context.RequireNode(operation.SourceGuid) as ShizukuNormalNode
                ?? throw new InvalidOperationException("Control-flow source must be a ShizukuNormalNode.");
            var target = context.RequireNode(operation.TargetGuid);
            if (!target.SupportControlInput)
                throw new InvalidOperationException($"Node {target.GUID} does not accept control input.");

            var port = GraphEditReflection.GetChainPorts(source)
                .FirstOrDefault(candidate => candidate.Name == operation.PortName)
                ?? throw new InvalidOperationException(
                    $"Control output port does not exist: {source.GUID}.{operation.PortName}");
            port.NextNodeGuid = target.GUID;
        }

        private static void ApplyDisconnectControl(GraphEditContext context, DisconnectControlOperation operation)
        {
            var source = context.RequireNode(operation.SourceGuid) as ShizukuNormalNode
                ?? throw new InvalidOperationException("Control-flow source must be a ShizukuNormalNode.");
            var port = GraphEditReflection.GetChainPorts(source)
                .FirstOrDefault(candidate => candidate.Name == operation.PortName)
                ?? throw new InvalidOperationException(
                    $"Control output port does not exist: {source.GUID}.{operation.PortName}");

            if (string.IsNullOrEmpty(operation.TargetGuid) || port.NextNodeGuid == operation.TargetGuid)
                port.NextNodeGuid = null;
        }

        private static void ApplyConnectParameter(GraphEditContext context, ConnectParameterOperation operation)
        {
            var source = context.RequireNode(operation.SourceGuid);
            var target = context.RequireNode(operation.TargetGuid);
            var output = GraphEditReflection.GetParameterPorts(source)
                .FirstOrDefault(port => port.IsOut && port.Name == operation.OutputPortName)
                ?? throw new InvalidOperationException(
                    $"Parameter output port does not exist: {source.GUID}.{operation.OutputPortName}");
            var input = GraphEditReflection.GetParameterPorts(target)
                .FirstOrDefault(port => !port.IsOut && port.Name == operation.InputPortName)
                ?? throw new InvalidOperationException(
                    $"Parameter input port does not exist: {target.GUID}.{operation.InputPortName}");
            if (GraphEditReflection.GetPortValueType(output) != GraphEditReflection.GetPortValueType(input))
                throw new InvalidOperationException(
                    "Parameter connections require equal port types. Create an explicit converter node for conversions.");

            var replaced = context.Edges
                .Where(edge => edge != null && edge.InputNodeGuid == target.GUID && edge.InputPortName == operation.InputPortName)
                .ToArray();
            context.Edges.RemoveAll(edge => edge != null &&
                edge.InputNodeGuid == target.GUID && edge.InputPortName == operation.InputPortName);
            foreach (var edge in replaced)
                context.NodeContext.Guid2EdgeMap.Remove(edge.GUID);

            var created = new ParameterEdge(
                source.GUID,
                operation.OutputPortName,
                target.GUID,
                operation.InputPortName)
            {
                GUID = operation.EdgeGuid
            };
            context.Edges.Add(created);
            context.NodeContext.Guid2EdgeMap[created.GUID] = created;
        }

        private static void ApplyDisconnectParameter(GraphEditContext context, DisconnectParameterOperation operation)
        {
            ParameterEdge[] removed;
            if (!string.IsNullOrWhiteSpace(operation.EdgeGuid))
            {
                removed = context.Edges
                    .Where(edge => edge != null && edge.GUID == operation.EdgeGuid)
                    .ToArray();
                context.Edges.RemoveAll(edge => edge != null && edge.GUID == operation.EdgeGuid);
            }
            else
            {
                Require(operation.SourceGuid, "sourceGuid");
                Require(operation.OutputPortName, "outputPort");
                Require(operation.TargetGuid, "targetGuid");
                Require(operation.InputPortName, "inputPort");
                removed = context.Edges.Where(edge => edge != null &&
                    edge.OutputNodeGuid == operation.SourceGuid && edge.OutputPortName == operation.OutputPortName &&
                    edge.InputNodeGuid == operation.TargetGuid && edge.InputPortName == operation.InputPortName).ToArray();
                context.Edges.RemoveAll(edge => edge != null &&
                    edge.OutputNodeGuid == operation.SourceGuid && edge.OutputPortName == operation.OutputPortName &&
                    edge.InputNodeGuid == operation.TargetGuid && edge.InputPortName == operation.InputPortName);
            }

            foreach (var edge in removed)
                context.NodeContext.Guid2EdgeMap.Remove(edge.GUID);
        }

        private static void ApplyAddGroup(GraphEditContext context, AddGroupOperation operation)
        {
            if (string.IsNullOrWhiteSpace(operation.Group.GUID))
                throw new InvalidOperationException("Created group requires a GUID.");
            if (context.Groups.Any(group => group != null && group.GUID == operation.Group.GUID))
                throw new InvalidOperationException($"Group GUID already exists: {operation.Group.GUID}");
            context.Groups.Add(operation.Group);
        }

        private static void ApplyDeleteGroups(GraphEditContext context, DeleteGroupsOperation operation)
        {
            var guids = new HashSet<string>(operation.GroupGuids, StringComparer.Ordinal);
            context.Groups.RemoveAll(group => group != null && guids.Contains(group.GUID));
        }

        private static void ApplyUpdateGroup(GraphEditContext context, UpdateGroupOperation operation)
        {
            var group = context.Groups.FirstOrDefault(candidate =>
                candidate != null && candidate.GUID == operation.GroupGuid)
                ?? throw new ArgumentException($"Group does not exist in the selected graph context: {operation.GroupGuid}");
            group.Title = operation.Title;
            group.PositionAndSize = operation.PositionAndSize;
        }

        private static void ApplyClearContext(GraphEditContext context)
        {
            context.Nodes.Clear();
            context.Edges.Clear();
            context.Groups.Clear();
            context.NodeContext.Guid2NodeMap.Clear();
            context.NodeContext.Guid2EdgeMap.Clear();
            if (context.Method == null)
                context.Graph.RootNodeGUID = null;
            else
            {
                context.Method.EntryNodeGUID = null;
                context.Method.ReturnNodeGUID = null;
            }
        }

        private static void OverwriteNodeFields(
            ShizukuNodeBase node,
            INodeContext context,
            JObject fields)
        {
            var serialized = JObject.Parse(EditorJsonUtility.ToJson(node));
            foreach (var property in fields.Properties())
                serialized[property.Name] = property.Value.DeepClone();

            EditorJsonUtility.FromJsonOverwrite(serialized.ToString(Formatting.None), node);
            foreach (var property in fields.Properties())
            {
                if (node is INodeSerializedFieldChangeHandler handler)
                    handler.OnSerializedFieldChanged(property.Name, context);
            }
        }

        private static void RebuildContextConnections(GraphEditContext context)
        {
            foreach (var node in context.Nodes.Where(node => node != null))
            {
                context.NodeContext.Guid2NodeMap[node.GUID] = node;
                node.Init(context.NodeContext);
            }

            var liveEdgeGuids = new HashSet<string>(context.Edges
                .Where(edge => edge != null)
                .Select(edge => edge.GUID), StringComparer.Ordinal);
            foreach (var staleGuid in context.NodeContext.Guid2EdgeMap.Keys
                         .Where(guid => !liveEdgeGuids.Contains(guid)).ToArray())
            {
                context.NodeContext.Guid2EdgeMap.Remove(staleGuid);
            }

            foreach (var edge in context.Edges.Where(edge => edge != null))
            {
                context.NodeContext.Guid2EdgeMap[edge.GUID] = edge;
                edge.ConnectPorts(context.NodeContext);
            }
        }

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{name} is required.");
            return value;
        }
    }

    internal static class GraphEditReflection
    {
        internal static IEnumerable<ChainPort> GetChainPorts(ShizukuNormalNode node)
        {
            return GetInstanceFields(node.GetType())
                .Where(field => typeof(ChainPort).IsAssignableFrom(field.FieldType))
                .Select(field => field.GetValue(node) as ChainPort)
                .Where(port => port != null);
        }

        internal static IEnumerable<ParameterEdgePort> GetParameterPorts(ShizukuNodeBase node)
        {
            var ports = GetInstanceFields(node.GetType())
                .Where(field => typeof(ParameterEdgePort).IsAssignableFrom(field.FieldType))
                .Select(field => field.GetValue(node) as ParameterEdgePort)
                .Where(port => port != null)
                .ToList();

            if (node is IDynamicParameterPortProvider provider && provider.DynamicParameterPorts != null)
            {
                ports.AddRange(provider.DynamicParameterPorts
                    .Select(descriptor => descriptor.Port)
                    .Where(port => port != null));
            }

            return ports.Distinct();
        }

        internal static Type GetPortValueType(ParameterEdgePort port)
        {
            for (var type = port.GetType(); type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ParameterEdgePort<>))
                    return type.GetGenericArguments()[0];
            }

            return null;
        }

        private static IEnumerable<FieldInfo> GetInstanceFields(Type type)
        {
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (var field in current.GetFields(
                             BindingFlags.Instance | BindingFlags.Public |
                             BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    yield return field;
                }
            }
        }
    }
}
