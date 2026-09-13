using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shizuku.Mcp.Server;

public sealed record BridgeOptions(string ProjectPath)
{
    public string DiscoveryPath => Path.Combine(ProjectPath, "Library", "ShizukuMcp", "bridge.json");
}

public sealed class BridgeClient(BridgeOptions options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> SendAsync(string operation, object? payload, CancellationToken cancellationToken)
    {
        var discovery = await ReadDiscoveryAsync(cancellationToken);
        using var client = new TcpClient();
        using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connectTimeout.CancelAfter(TimeSpan.FromSeconds(5));
        await client.ConnectAsync("127.0.0.1", discovery.Port, connectTimeout.Token);

        await using var stream = client.GetStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };
        using var reader = new StreamReader(stream, Encoding.UTF8, false, leaveOpen: true);

        var request = new BridgeRequest(
            Guid.NewGuid().ToString("N"),
            discovery.Token,
            operation,
            payload);
        await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));

        using var responseTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        responseTimeout.CancelAfter(TimeSpan.FromSeconds(30));
        var responseLine = await reader.ReadLineAsync(responseTimeout.Token);
        if (string.IsNullOrWhiteSpace(responseLine))
            throw new IOException("Shizuku Unity bridge closed the connection without a response.");

        var response = JsonSerializer.Deserialize<BridgeResponse>(responseLine, JsonOptions)
            ?? throw new IOException("Shizuku Unity bridge returned an invalid response.");
        if (!response.Success)
            throw new InvalidOperationException(response.Error ?? "Shizuku Unity bridge request failed.");

        return response.Data.ValueKind == JsonValueKind.Undefined
            ? "null"
            : response.Data.GetRawText();
    }

    private async Task<BridgeDiscovery> ReadDiscoveryAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(options.DiscoveryPath))
        {
            throw new InvalidOperationException(
                $"Shizuku MCP bridge is not running for '{options.ProjectPath}'. Open the Unity project and enable Project Settings > Shizuku > MCP.");
        }

        await using var stream = File.Open(options.DiscoveryPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return await JsonSerializer.DeserializeAsync<BridgeDiscovery>(stream, JsonOptions, cancellationToken)
            ?? throw new IOException($"Invalid Shizuku MCP discovery file: {options.DiscoveryPath}");
    }

    private sealed record BridgeRequest(string Id, string Token, string Operation, object? Payload);

    private sealed class BridgeResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("data")]
        public JsonElement Data { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }

    private sealed class BridgeDiscovery
    {
        [JsonPropertyName("port")]
        public int Port { get; init; }

        [JsonPropertyName("token")]
        public string Token { get; init; } = string.Empty;
    }
}
