using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Shizuku.Mcp.Server;

[McpServerToolType]
public static class ShizukuGraphTools
{
    [McpServerTool, Description("List all Shizuku graph and blueprint assets in the open Unity project.")]
    public static Task<string> shizuku_graph_list(
        BridgeClient bridge,
        CancellationToken cancellationToken) =>
        bridge.SendAsync("graph_list", new { }, cancellationToken);

    [McpServerTool, Description("List node types that can be created in Shizuku graphs, including stable type IDs and menu paths.")]
    public static Task<string> shizuku_node_catalog(
        BridgeClient bridge,
        CancellationToken cancellationToken) =>
        bridge.SendAsync("node_catalog", new { }, cancellationToken);

    [McpServerTool, Description("Read a Shizuku graph as semantic JSON. methodGuid is optional and selects a method subgraph.")]
    public static Task<string> shizuku_graph_read(
        BridgeClient bridge,
        [Description("Unity asset path, for example Assets/MyGraph.asset.")] string assetPath,
        [Description("Optional Shizuku method GUID. Leave empty for the main graph.")] string methodGuid = "",
        CancellationToken cancellationToken = default) =>
        bridge.SendAsync("graph_read", new { assetPath, methodGuid }, cancellationToken);

    [McpServerTool, Description("Validate a Shizuku graph and return structural issues without modifying the asset.")]
    public static Task<string> shizuku_graph_validate(
        BridgeClient bridge,
        [Description("Unity asset path, for example Assets/MyGraph.asset.")] string assetPath,
        [Description("Optional Shizuku method GUID. Leave empty for the main graph.")] string methodGuid = "",
        CancellationToken cancellationToken = default) =>
        bridge.SendAsync("graph_validate", new { assetPath, methodGuid }, cancellationToken);

    [McpServerTool, Description("Apply transactional Shizuku graph operations. Use dryRun=true first, then pass the returned revision as expectedRevision when committing.")]
    public static Task<string> shizuku_graph_apply(
        BridgeClient bridge,
        [Description("Unity asset path, for example Assets/MyGraph.asset.")] string assetPath,
        [Description("JSON array of operations: create_node, delete_node, set_node_fields, connect_control, disconnect_control, connect_parameter, disconnect_parameter.")] string operationsJson,
        [Description("Revision returned by shizuku_graph_read or a prior dry run. Empty disables optimistic concurrency checking.")] string expectedRevision = "",
        [Description("When true, validates the edit on a clone and does not save the asset.")] bool dryRun = true,
        [Description("Optional Shizuku method GUID. Leave empty for the main graph.")] string methodGuid = "",
        CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(operationsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("operationsJson must be a JSON array.", nameof(operationsJson));

        return bridge.SendAsync(
            "graph_apply",
            new { assetPath, methodGuid, expectedRevision, dryRun, operations = document.RootElement.Clone() },
            cancellationToken);
    }
}
