using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shizuku.Graph;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Shizuku.Graph.Editor.Mcp
{
    internal static class ShizukuMcpGraphService
    {
        internal static JToken Handle(string operation, JObject payload)
        {
            return operation switch
            {
                "graph_list" => ListGraphs(),
                "node_catalog" => GetNodeCatalog(),
                "graph_read" => ReadGraph(payload),
                "graph_validate" => ValidateGraph(payload),
                "graph_apply" => ApplyGraph(payload),
                _ => throw new ArgumentException($"Unknown Shizuku MCP operation: {operation}")
            };
        }

        internal static JArray ListGraphs()
        {
            var result = new JArray();
            foreach (var guid in AssetDatabase.FindAssets("t:ShizukuGraphBase"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var graph = AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(path);
                if (graph == null)
                    continue;

                result.Add(new JObject
                {
                    ["assetPath"] = path,
                    ["name"] = graph.name,
                    ["type"] = GetTypeId(graph.GetType()),
                    ["nodeCount"] = graph.Nodes.Count,
                    ["methodCount"] = graph.Methods.Count,
                    ["revision"] = ComputeRevision(BuildGraphDocument(graph, string.Empty))
                });
            }

            return result;
        }

        internal static JArray GetNodeCatalog()
        {
            var result = new JArray();
            foreach (var type in GetLoadableTypes()
                         .Where(type => type.IsClass && !type.IsAbstract && typeof(ShizukuNodeBase).IsAssignableFrom(type)))
            {
                var menu = type.GetCustomAttribute<NodeMenuItemAttribute>();
                if (menu == null || string.IsNullOrWhiteSpace(menu.MenuPath))
                    continue;

                result.Add(new JObject
                {
                    ["typeId"] = GetTypeId(type),
                    ["menuPath"] = menu.MenuPath,
                    ["displayName"] = menu.DisplayName,
                    ["description"] = menu.Description ?? string.Empty,
                    ["supportsControlInput"] = GetDefaultBool(type, node => node.SupportControlInput),
                    ["supportsControlOutput"] = GetDefaultBool(type, node => node.SupportControlOutput),
                    ["latent"] = typeof(ShizukuLatentNode).IsAssignableFrom(type)
                });
            }

            return new JArray(result.OrderBy(token => token.Value<string>("menuPath"), StringComparer.Ordinal));
        }

        internal static JObject ReadGraph(JObject payload)
        {
            var graph = LoadGraph(payload.Value<string>("assetPath"));
            var methodGuid = payload.Value<string>("methodGuid") ?? string.Empty;
            var document = BuildGraphDocument(graph, methodGuid);
            document["revision"] = ComputeRevision(document);
            return document;
        }

        internal static JObject ValidateGraph(JObject payload)
        {
            var graph = LoadGraph(payload.Value<string>("assetPath"));
            var methodGuid = payload.Value<string>("methodGuid") ?? string.Empty;
            return BuildValidationResult(graph, methodGuid);
        }

        internal static JObject ApplyGraph(JObject payload)
        {
            var assetPath = RequireString(payload, "assetPath");
            var methodGuid = payload.Value<string>("methodGuid") ?? string.Empty;
            var expectedRevision = payload.Value<string>("expectedRevision") ?? string.Empty;
            var dryRun = payload.Value<bool?>("dryRun") ?? true;
            var operations = payload["operations"] as JArray
                ?? throw new ArgumentException("graph_apply requires an operations array.");

            var graph = LoadGraph(assetPath);
            var currentDocument = BuildGraphDocument(graph, methodGuid);
            var currentRevision = ComputeRevision(currentDocument);
            if (!string.IsNullOrWhiteSpace(expectedRevision) &&
                !string.Equals(expectedRevision, currentRevision, StringComparison.Ordinal))
            {
                return new JObject
                {
                    ["applied"] = false,
                    ["conflict"] = true,
                    ["revision"] = currentRevision,
                    ["message"] = "Graph changed after the caller last read it. Read the graph again before applying edits."
                };
            }

            var normalizedOperations = (JArray)operations.DeepClone();
            NormalizeCreateNodeGuids(normalizedOperations);

            var preview = UnityEngine.Object.Instantiate(graph);
            preview.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                ApplyOperations(preview, methodGuid, normalizedOperations);
                SynchronizeDynamicPorts(preview, methodGuid);
                var validation = BuildValidationResult(preview, methodGuid);
                if (!validation.Value<bool>("valid"))
                {
                    return new JObject
                    {
                        ["applied"] = false,
                        ["dryRun"] = dryRun,
                        ["revision"] = currentRevision,
                        ["validation"] = validation
                    };
                }

                var previewDocument = BuildGraphDocument(preview, methodGuid);
                var previewRevision = ComputeRevision(previewDocument);
                if (dryRun)
                {
                    return new JObject
                    {
                        ["applied"] = false,
                        ["dryRun"] = true,
                        ["baseRevision"] = currentRevision,
                        ["resultRevision"] = previewRevision,
                        ["operations"] = normalizedOperations,
                        ["validation"] = validation
                    };
                }

                var backup = EditorJsonUtility.ToJson(graph);
                Undo.RecordObject(graph, "Apply Shizuku MCP graph edits");
                try
                {
                    ApplyOperations(graph, methodGuid, normalizedOperations);
                    SynchronizeDynamicPorts(graph, methodGuid);
                    var committedValidation = BuildValidationResult(graph, methodGuid);
                    if (!committedValidation.Value<bool>("valid"))
                        throw new InvalidOperationException("Committed graph failed validation; changes were rolled back.");

                    EditorUtility.SetDirty(graph);
                    AssetDatabase.SaveAssetIfDirty(graph);
                    ShizukuGraphWindow.RefreshOpenGraph(graph);

                    var resultDocument = BuildGraphDocument(graph, methodGuid);
                    return new JObject
                    {
                        ["applied"] = true,
                        ["dryRun"] = false,
                        ["baseRevision"] = currentRevision,
                        ["revision"] = ComputeRevision(resultDocument),
                        ["operations"] = normalizedOperations,
                        ["validation"] = committedValidation
                    };
                }
                catch
                {
                    EditorJsonUtility.FromJsonOverwrite(backup, graph);
                    EditorUtility.SetDirty(graph);
                    throw;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static void ApplyOperations(ShizukuGraphBase graph, string methodGuid, IEnumerable<JToken> operations)
        {
            var context = GetContext(graph, methodGuid);
            foreach (var token in operations)
            {
                if (token is not JObject operation)
                    throw new ArgumentException("Each graph operation must be a JSON object.");

                switch (RequireString(operation, "op"))
                {
                    case "create_node":
                        CreateNode(graph, context, operation);
                        break;
                    case "delete_node":
                        DeleteNode(graph, context, RequireString(operation, "guid"));
                        break;
                    case "set_node_fields":
                        SetNodeFields(context, operation);
                        break;
                    case "connect_control":
                        ConnectControl(context, operation);
                        break;
                    case "disconnect_control":
                        DisconnectControl(context, operation);
                        break;
                    case "connect_parameter":
                        ConnectParameter(context, operation);
                        break;
                    case "disconnect_parameter":
                        DisconnectParameter(context, operation);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported graph operation: {operation.Value<string>("op")}");
                }
            }
        }

        private static void CreateNode(ShizukuGraphBase graph, GraphContext context, JObject operation)
        {
            var type = ResolveNodeType(RequireString(operation, "type"));
            if (context.Method != null && typeof(ShizukuLatentNode).IsAssignableFrom(type))
                throw new InvalidOperationException("Shizuku methods do not support latent nodes.");

            var node = Activator.CreateInstance(type) as ShizukuNodeBase
                ?? throw new InvalidOperationException($"Could not create node type {type.FullName}.");
            node.GUID = RequireString(operation, "guid");
            if (context.Nodes.Any(existing => existing != null && existing.GUID == node.GUID))
                throw new InvalidOperationException($"Node GUID already exists: {node.GUID}");

            var position = operation["position"] as JArray;
            node.PositionAndSize = new float4(
                position?.ElementAtOrDefault(0)?.Value<float>() ?? 0f,
                position?.ElementAtOrDefault(1)?.Value<float>() ?? 0f,
                position?.ElementAtOrDefault(2)?.Value<float>() ?? 200f,
                position?.ElementAtOrDefault(3)?.Value<float>() ?? 100f);

            context.Nodes.Add(node);
            if (operation["fields"] is JObject fields)
                OverwriteNodeFields(node, context.NodeContext, fields);

            if (context.Method == null && type == typeof(ShizukuRootNode) && string.IsNullOrEmpty(graph.RootNodeGUID))
                graph.RootNodeGUID = node.GUID;
        }

        private static void DeleteNode(ShizukuGraphBase graph, GraphContext context, string guid)
        {
            var node = FindNode(context, guid);
            context.Nodes.Remove(node);
            context.Edges.RemoveAll(edge => edge == null || edge.OutputNodeGuid == guid || edge.InputNodeGuid == guid);

            foreach (var normalNode in context.Nodes.OfType<ShizukuNormalNode>())
            {
                foreach (var port in GetChainPorts(normalNode))
                {
                    if (port.NextNodeGuid == guid)
                        port.NextNodeGuid = null;
                }
            }

            if (context.Method == null && graph.RootNodeGUID == guid)
                graph.RootNodeGUID = null;
            if (context.Method != null)
            {
                if (context.Method.EntryNodeGUID == guid)
                    context.Method.EntryNodeGUID = null;
                if (context.Method.ReturnNodeGUID == guid)
                    context.Method.ReturnNodeGUID = null;
            }
        }

        private static void SetNodeFields(GraphContext context, JObject operation)
        {
            var node = FindNode(context, RequireString(operation, "guid"));
            var fields = operation["fields"] as JObject
                ?? throw new ArgumentException("set_node_fields requires a fields object.");
            OverwriteNodeFields(node, context.NodeContext, fields);
        }

        private static void OverwriteNodeFields(ShizukuNodeBase node, INodeContext context, JObject fields)
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

        private static void ConnectControl(GraphContext context, JObject operation)
        {
            var source = FindNode(context, RequireString(operation, "sourceGuid")) as ShizukuNormalNode
                ?? throw new InvalidOperationException("Control-flow source must be a ShizukuNormalNode.");
            var target = FindNode(context, RequireString(operation, "targetGuid"));
            if (!target.SupportControlInput)
                throw new InvalidOperationException($"Node {target.GUID} does not accept control input.");

            var portName = RequireString(operation, "port");
            var port = GetChainPorts(source).FirstOrDefault(candidate => candidate.Name == portName)
                ?? throw new InvalidOperationException($"Control output port does not exist: {source.GUID}.{portName}");
            port.NextNodeGuid = target.GUID;
        }

        private static void DisconnectControl(GraphContext context, JObject operation)
        {
            var source = FindNode(context, RequireString(operation, "sourceGuid")) as ShizukuNormalNode
                ?? throw new InvalidOperationException("Control-flow source must be a ShizukuNormalNode.");
            var portName = RequireString(operation, "port");
            var port = GetChainPorts(source).FirstOrDefault(candidate => candidate.Name == portName)
                ?? throw new InvalidOperationException($"Control output port does not exist: {source.GUID}.{portName}");
            var targetGuid = operation.Value<string>("targetGuid");
            if (string.IsNullOrEmpty(targetGuid) || port.NextNodeGuid == targetGuid)
                port.NextNodeGuid = null;
        }

        private static void ConnectParameter(GraphContext context, JObject operation)
        {
            var source = FindNode(context, RequireString(operation, "sourceGuid"));
            var target = FindNode(context, RequireString(operation, "targetGuid"));
            var outputName = RequireString(operation, "outputPort");
            var inputName = RequireString(operation, "inputPort");
            var output = GetParameterPorts(source).FirstOrDefault(port => port.IsOut && port.Name == outputName)
                ?? throw new InvalidOperationException($"Parameter output port does not exist: {source.GUID}.{outputName}");
            var input = GetParameterPorts(target).FirstOrDefault(port => !port.IsOut && port.Name == inputName)
                ?? throw new InvalidOperationException($"Parameter input port does not exist: {target.GUID}.{inputName}");
            if (GetPortValueType(output) != GetPortValueType(input))
                throw new InvalidOperationException("MCP parameter connections require equal port types. Create an explicit converter node for conversions.");

            context.Edges.RemoveAll(edge => edge != null && edge.InputNodeGuid == target.GUID && edge.InputPortName == inputName);
            context.Edges.Add(new ParameterEdge(source.GUID, outputName, target.GUID, inputName));
        }

        private static void DisconnectParameter(GraphContext context, JObject operation)
        {
            var edgeGuid = operation.Value<string>("edgeGuid");
            if (!string.IsNullOrEmpty(edgeGuid))
            {
                context.Edges.RemoveAll(edge => edge != null && edge.GUID == edgeGuid);
                return;
            }

            var sourceGuid = RequireString(operation, "sourceGuid");
            var targetGuid = RequireString(operation, "targetGuid");
            var outputPort = RequireString(operation, "outputPort");
            var inputPort = RequireString(operation, "inputPort");
            context.Edges.RemoveAll(edge => edge != null &&
                edge.OutputNodeGuid == sourceGuid && edge.OutputPortName == outputPort &&
                edge.InputNodeGuid == targetGuid && edge.InputPortName == inputPort);
        }

        private static JObject BuildGraphDocument(ShizukuGraphBase graph, string methodGuid)
        {
            var context = GetContext(graph, methodGuid);
            var nodes = new JArray(context.Nodes.Where(node => node != null).Select(BuildNodeDocument));
            var parameterEdges = new JArray(context.Edges.Where(edge => edge != null).Select(edge => new JObject
            {
                ["guid"] = edge.GUID,
                ["sourceGuid"] = edge.OutputNodeGuid,
                ["outputPort"] = edge.OutputPortName,
                ["targetGuid"] = edge.InputNodeGuid,
                ["inputPort"] = edge.InputPortName
            }));

            return new JObject
            {
                ["assetPath"] = AssetDatabase.GetAssetPath(graph),
                ["name"] = graph.name,
                ["graphType"] = GetTypeId(graph.GetType()),
                ["graphGuid"] = graph.GUID,
                ["methodGuid"] = methodGuid,
                ["rootNodeGuid"] = context.Method == null ? graph.RootNodeGUID : context.Method.EntryNodeGUID,
                ["nodes"] = nodes,
                ["parameterEdges"] = parameterEdges
            };
        }

        private static JObject BuildNodeDocument(ShizukuNodeBase node)
        {
            JToken fields;
            try
            {
                fields = JObject.Parse(EditorJsonUtility.ToJson(node));
            }
            catch (JsonException)
            {
                fields = new JObject();
            }

            var controlOutputs = new JArray();
            if (node is ShizukuNormalNode normalNode)
            {
                foreach (var port in GetChainPorts(normalNode))
                {
                    controlOutputs.Add(new JObject
                    {
                        ["name"] = port.Name,
                        ["targetGuid"] = port.NextNodeGuid
                    });
                }
            }

            var parameterPorts = new JArray(GetParameterPorts(node).Select(port => new JObject
            {
                ["name"] = port.Name,
                ["direction"] = port.IsOut ? "output" : "input",
                ["valueType"] = GetPortValueType(port)?.AssemblyQualifiedName ?? string.Empty
            }));

            return new JObject
            {
                ["guid"] = node.GUID,
                ["typeId"] = GetTypeId(node.GetType()),
                ["title"] = node.Title,
                ["position"] = new JArray(node.PositionAndSize.x, node.PositionAndSize.y, node.PositionAndSize.z, node.PositionAndSize.w),
                ["supportsControlInput"] = node.SupportControlInput,
                ["controlOutputs"] = controlOutputs,
                ["parameterPorts"] = parameterPorts,
                ["fields"] = fields
            };
        }

        private static JObject BuildValidationResult(ShizukuGraphBase graph, string methodGuid)
        {
            var context = GetContext(graph, methodGuid);
            var issues = new JArray();
            var validNodes = context.Nodes.Where(node => node != null).ToList();
            foreach (var duplicate in validNodes.GroupBy(node => node.GUID).Where(group => string.IsNullOrEmpty(group.Key) || group.Count() > 1))
                AddIssue(issues, "error", "duplicate_node_guid", $"Node GUID is empty or duplicated: '{duplicate.Key}'");

            var nodeByGuid = validNodes
                .Where(node => !string.IsNullOrEmpty(node.GUID))
                .GroupBy(node => node.GUID)
                .ToDictionary(group => group.Key, group => group.First());

            var rootGuid = context.Method == null ? graph.RootNodeGUID : context.Method.EntryNodeGUID;
            if (!string.IsNullOrEmpty(rootGuid) && !nodeByGuid.ContainsKey(rootGuid))
                AddIssue(issues, "error", "missing_root", $"Root or entry node does not exist: {rootGuid}");

            var adjacency = nodeByGuid.Keys.ToDictionary(guid => guid, _ => new List<string>());
            foreach (var normalNode in validNodes.OfType<ShizukuNormalNode>())
            {
                foreach (var port in GetChainPorts(normalNode))
                {
                    if (string.IsNullOrEmpty(port.NextNodeGuid))
                        continue;
                    if (!nodeByGuid.ContainsKey(port.NextNodeGuid))
                        AddIssue(issues, "error", "missing_control_target", $"{normalNode.GUID}.{port.Name} targets missing node {port.NextNodeGuid}.");
                    else
                        adjacency[normalNode.GUID].Add(port.NextNodeGuid);
                }
            }

            var occupiedInputs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in context.Edges)
            {
                if (edge == null)
                {
                    AddIssue(issues, "error", "null_parameter_edge", "Graph contains a null parameter edge.");
                    continue;
                }

                if (!nodeByGuid.TryGetValue(edge.OutputNodeGuid, out var source) || !nodeByGuid.TryGetValue(edge.InputNodeGuid, out var target))
                {
                    AddIssue(issues, "error", "missing_parameter_node", $"Parameter edge {edge.GUID} references a missing node.");
                    continue;
                }

                var output = GetParameterPorts(source).FirstOrDefault(port => port.IsOut && port.Name == edge.OutputPortName);
                var input = GetParameterPorts(target).FirstOrDefault(port => !port.IsOut && port.Name == edge.InputPortName);
                if (output == null || input == null)
                    AddIssue(issues, "error", "missing_parameter_port", $"Parameter edge {edge.GUID} references a missing port.");
                else if (GetPortValueType(output) != GetPortValueType(input))
                    AddIssue(issues, "error", "parameter_type_mismatch", $"Parameter edge {edge.GUID} connects incompatible types.");

                var inputKey = edge.InputNodeGuid + "\n" + edge.InputPortName;
                if (!occupiedInputs.Add(inputKey))
                    AddIssue(issues, "error", "multiple_input_edges", $"Input {edge.InputNodeGuid}.{edge.InputPortName} has multiple edges.");
                adjacency[source.GUID].Add(target.GUID);
            }

            if (HasCycle(adjacency))
                AddIssue(issues, "error", "cycle", "Graph contains a directed dependency or control-flow cycle.");

            var document = BuildGraphDocument(graph, methodGuid);
            return new JObject
            {
                ["valid"] = !issues.Any(issue => issue.Value<string>("severity") == "error"),
                ["revision"] = ComputeRevision(document),
                ["issues"] = issues
            };
        }

        private static GraphContext GetContext(ShizukuGraphBase graph, string methodGuid)
        {
            if (string.IsNullOrWhiteSpace(methodGuid))
                return new GraphContext(graph, null, graph.Nodes, graph.Edges);

            var method = graph.Methods.FirstOrDefault(candidate => candidate != null && candidate.GUID == methodGuid)
                ?? throw new ArgumentException($"Shizuku method does not exist: {methodGuid}");
            return new GraphContext(method, method, method.Nodes, method.Edges);
        }

        private static ShizukuGraphBase LoadGraph(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("assetPath must be a Unity project path below Assets/.");

            return AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(assetPath)
                ?? throw new ArgumentException($"Shizuku graph asset does not exist: {assetPath}");
        }

        private static ShizukuNodeBase FindNode(GraphContext context, string guid)
        {
            return context.Nodes.FirstOrDefault(node => node != null && node.GUID == guid)
                ?? throw new ArgumentException($"Node does not exist in the selected graph context: {guid}");
        }

        private static IEnumerable<ChainPort> GetChainPorts(ShizukuNormalNode node)
        {
            return GetInstanceFields(node.GetType())
                .Where(field => typeof(ChainPort).IsAssignableFrom(field.FieldType))
                .Select(field => field.GetValue(node) as ChainPort)
                .Where(port => port != null);
        }

        private static IEnumerable<ParameterEdgePort> GetParameterPorts(ShizukuNodeBase node)
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

        private static IEnumerable<FieldInfo> GetInstanceFields(Type type)
        {
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    yield return field;
            }
        }

        private static Type GetPortValueType(ParameterEdgePort port)
        {
            for (var type = port.GetType(); type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ParameterEdgePort<>))
                    return type.GetGenericArguments()[0];
            }

            return null;
        }

        private static void SynchronizeDynamicPorts(ShizukuGraphBase graph, string methodGuid)
        {
            var context = GetContext(graph, methodGuid);
            foreach (var provider in context.Nodes.OfType<IDynamicParameterPortProvider>())
                provider.SynchronizeDynamicParameterPorts(context.NodeContext);
        }

        private static Type ResolveNodeType(string typeId)
        {
            var separator = typeId.IndexOf(':');
            var assemblyName = separator > 0 ? typeId.Substring(0, separator) : null;
            var fullName = separator > 0 ? typeId.Substring(separator + 1) : typeId;
            var match = GetLoadableTypes().FirstOrDefault(type =>
                typeof(ShizukuNodeBase).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                (string.Equals(type.FullName, fullName, StringComparison.Ordinal) ||
                 string.Equals(type.GetCustomAttribute<NodeMenuItemAttribute>()?.MenuPath, typeId, StringComparison.Ordinal)) &&
                (assemblyName == null || string.Equals(type.Assembly.GetName().Name, assemblyName, StringComparison.Ordinal)));

            return match ?? throw new ArgumentException($"Unknown or non-creatable Shizuku node type: {typeId}");
        }

        private static IEnumerable<Type> GetLoadableTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types.Where(type => type != null).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                    yield return type;
            }
        }

        private static bool HasCycle(Dictionary<string, List<string>> adjacency)
        {
            var state = new Dictionary<string, byte>(StringComparer.Ordinal);
            foreach (var guid in adjacency.Keys)
            {
                if (Visit(guid))
                    return true;
            }
            return false;

            bool Visit(string guid)
            {
                if (state.TryGetValue(guid, out var value))
                    return value == 1;
                state[guid] = 1;
                foreach (var target in adjacency[guid])
                {
                    if (Visit(target))
                        return true;
                }
                state[guid] = 2;
                return false;
            }
        }

        private static void NormalizeCreateNodeGuids(JArray operations)
        {
            foreach (var operation in operations.OfType<JObject>())
            {
                if (operation.Value<string>("op") == "create_node" && string.IsNullOrWhiteSpace(operation.Value<string>("guid")))
                    operation["guid"] = Guid.NewGuid().ToString();
            }
        }

        private static string ComputeRevision(JObject document)
        {
            var clone = (JObject)document.DeepClone();
            clone.Remove("revision");
            clone.Remove("assetPath");
            clone.Remove("name");
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(clone.ToString(Formatting.None));
            return BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static string GetTypeId(Type type) => type.Assembly.GetName().Name + ":" + type.FullName;

        private static bool GetDefaultBool(Type type, Func<ShizukuNodeBase, bool> selector)
        {
            try
            {
                return selector((ShizukuNodeBase)Activator.CreateInstance(type));
            }
            catch
            {
                return false;
            }
        }

        private static string RequireString(JObject value, string propertyName)
        {
            var result = value.Value<string>(propertyName);
            if (string.IsNullOrWhiteSpace(result))
                throw new ArgumentException($"Missing required string property: {propertyName}");
            return result;
        }

        private static void AddIssue(JArray issues, string severity, string code, string message)
        {
            issues.Add(new JObject
            {
                ["severity"] = severity,
                ["code"] = code,
                ["message"] = message
            });
        }

        private sealed class GraphContext
        {
            public GraphContext(INodeContext nodeContext, ShizukuMethod method, List<ShizukuNodeBase> nodes, List<ParameterEdge> edges)
            {
                NodeContext = nodeContext;
                Method = method;
                Nodes = nodes;
                Edges = edges;
            }

            public INodeContext NodeContext { get; }
            public ShizukuMethod Method { get; }
            public List<ShizukuNodeBase> Nodes { get; }
            public List<ParameterEdge> Edges { get; }
        }
    }
}
