using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Shizuku.Graph.Editor.Mcp
{
    [InitializeOnLoad]
    internal static class ShizukuMcpBridge
    {
        private const string ProtocolVersion = "1";
        private static readonly ConcurrentQueue<PendingRequest> PendingRequests = new();
        private static readonly string ProjectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static readonly string EnabledKey = "Shizuku.Mcp.Enabled." + ProjectPath.ToLowerInvariant();

        private static TcpListener _listener;
        private static CancellationTokenSource _cancellation;
        private static string _token;
        private static int _port;

        static ShizukuMcpBridge()
        {
            EditorApplication.update += PumpRequests;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.quitting += Stop;
            EditorApplication.delayCall += EnsureState;
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EditorApplication.delayCall += EnsureState;
        }

        internal static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set
            {
                EditorPrefs.SetBool(EnabledKey, value);
                EnsureState();
            }
        }

        internal static bool IsRunning => _listener != null;
        internal static int Port => _port;
        internal static string DiscoveryPath => Path.Combine(ProjectPath, "Library", "ShizukuMcp", "bridge.json");

        internal static void EnsureState()
        {
            if (Enabled)
                Start();
            else
                Stop();
        }

        private static void Start()
        {
            if (_listener != null || EditorApplication.isCompiling)
                return;

            try
            {
                _token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                _cancellation = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                WriteDiscoveryFile();
                _ = AcceptLoopAsync(_listener, _cancellation.Token);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Shizuku MCP] Failed to start Unity bridge: {exception.Message}");
                Stop();
            }
        }

        internal static void Stop()
        {
            var listener = _listener;
            _listener = null;
            _port = 0;

            try
            {
                _cancellation?.Cancel();
                listener?.Stop();
            }
            catch (Exception)
            {
                // Listener shutdown intentionally interrupts pending accepts.
            }
            finally
            {
                _cancellation?.Dispose();
                _cancellation = null;
                _token = null;
            }

            while (PendingRequests.TryDequeue(out var request))
                request.Completion.TrySetResult(CreateError(request.Id, "Unity bridge stopped before the request completed."));

            TryDeleteDiscoveryFile();
        }

        private static async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client, cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    if (!cancellationToken.IsCancellationRequested)
                        Debug.LogWarning($"[Shizuku MCP] Bridge accept failed: {exception.Message}");
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            {
                try
                {
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true);
                    using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true)
                    {
                        AutoFlush = true,
                        NewLine = "\n"
                    };

                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                        return;

                    var requestJson = JObject.Parse(line);
                    var id = requestJson.Value<string>("id") ?? Guid.NewGuid().ToString("N");
                    if (!string.Equals(requestJson.Value<string>("token"), _token, StringComparison.Ordinal))
                    {
                        await writer.WriteLineAsync(CreateError(id, "Unauthorized bridge request.").ToString(Formatting.None));
                        return;
                    }

                    var completion = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
                    PendingRequests.Enqueue(new PendingRequest(id, requestJson, completion));

                    using (cancellationToken.Register(() => completion.TrySetCanceled()))
                    {
                        var response = await completion.Task;
                        await writer.WriteLineAsync(response.ToString(Formatting.None));
                    }
                }
                catch (OperationCanceledException)
                {
                    // Domain reload or Editor shutdown.
                }
                catch (Exception exception)
                {
                    if (!cancellationToken.IsCancellationRequested)
                        Debug.LogWarning($"[Shizuku MCP] Bridge request failed: {exception.Message}");
                }
            }
        }

        private static void PumpRequests()
        {
            if (_listener == null)
                return;

            var processed = 0;
            while (processed++ < 16 && PendingRequests.TryDequeue(out var request))
            {
                try
                {
                    var operation = request.Json.Value<string>("operation");
                    var payload = request.Json["payload"] as JObject ?? new JObject();
                    var data = ShizukuMcpGraphService.Handle(operation, payload);
                    request.Completion.TrySetResult(new JObject
                    {
                        ["id"] = request.Id,
                        ["success"] = true,
                        ["data"] = data ?? JValue.CreateNull()
                    });
                }
                catch (Exception exception)
                {
                    request.Completion.TrySetResult(CreateError(request.Id, exception.Message));
                    Debug.LogException(exception);
                }
            }
        }

        private static JObject CreateError(string id, string error)
        {
            return new JObject
            {
                ["id"] = id,
                ["success"] = false,
                ["error"] = error
            };
        }

        private static void WriteDiscoveryFile()
        {
            var path = DiscoveryPath;
            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            var json = new JObject
            {
                ["version"] = ProtocolVersion,
                ["projectPath"] = ProjectPath,
                ["processId"] = System.Diagnostics.Process.GetCurrentProcess().Id,
                ["port"] = _port,
                ["token"] = _token
            }.ToString(Formatting.Indented);

            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
            if (File.Exists(path))
                File.Replace(temporaryPath, path, null);
            else
                File.Move(temporaryPath, path);
        }

        private static void TryDeleteDiscoveryFile()
        {
            try
            {
                if (File.Exists(DiscoveryPath))
                    File.Delete(DiscoveryPath);
            }
            catch (IOException)
            {
                // A concurrently starting bridge will replace stale discovery data.
            }
        }

        private sealed class PendingRequest
        {
            public PendingRequest(string id, JObject json, TaskCompletionSource<JObject> completion)
            {
                Id = id;
                Json = json;
                Completion = completion;
            }

            public string Id { get; }
            public JObject Json { get; }
            public TaskCompletionSource<JObject> Completion { get; }
        }
    }
}
