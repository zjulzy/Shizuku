using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Shizuku.Graph.Editor.Mcp
{
    internal static class ShizukuMcpServerInstaller
    {
        private static readonly string ProjectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        internal static bool IsBuilding { get; private set; }
        internal static string LastStatus { get; private set; } = "Not built in this Editor session.";
        internal static event Action StatusChanged;

        internal static string RuntimeIdentifier
        {
            get
            {
                return Application.platform switch
                {
                    RuntimePlatform.WindowsEditor => "win-x64",
                    RuntimePlatform.OSXEditor => SystemInfo.processorType.IndexOf("ARM", StringComparison.OrdinalIgnoreCase) >= 0
                        ? "osx-arm64"
                        : "osx-x64",
                    RuntimePlatform.LinuxEditor => "linux-x64",
                    _ => throw new PlatformNotSupportedException($"Unsupported Unity Editor platform: {Application.platform}")
                };
            }
        }

        internal static string ServerRoot => Path.Combine(ProjectPath, "Library", "ShizukuMcp", "Server", RuntimeIdentifier);
        internal static string ExecutablePath => Path.Combine(
            ServerRoot,
            Application.platform == RuntimePlatform.WindowsEditor ? "Shizuku.Mcp.Server.exe" : "Shizuku.Mcp.Server");
        internal static bool IsBuilt => File.Exists(ExecutablePath);

        internal static async void Build(Action<bool, string> completed = null)
        {
            if (IsBuilding)
                return;

            IsBuilding = true;
            LastStatus = "Building self-contained MCP server...";
            StatusChanged?.Invoke();

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ShizukuMcpServerInstaller).Assembly);
            var source = package == null
                ? null
                : Path.Combine(package.resolvedPath, "Tools~", "Shizuku.Mcp.Server");
            var staging = Path.Combine(ProjectPath, "Library", "ShizukuMcp", "ServerSource");
            var output = ServerRoot;
            var rid = RuntimeIdentifier;

            var result = await Task.Run(() => BuildWorker(source, staging, output, rid));
            IsBuilding = false;
            LastStatus = result.Message;
            StatusChanged?.Invoke();
            completed?.Invoke(result.Success, result.Message);
        }

        private static BuildResult BuildWorker(string source, string staging, string output, string rid)
        {
            try
            {
                if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
                    return BuildResult.Fail("MCP server source was not found in the Shizuku package.");
                if (FindCommand("dotnet") == null)
                    return BuildResult.Fail(".NET SDK was not found on PATH. Install .NET 8 SDK or newer, then retry.");

                RecreateDirectory(staging);
                CopySource(source, staging);
                RecreateDirectory(output);

                var projectFile = Path.Combine(staging, "Shizuku.Mcp.Server.csproj");
                var arguments = new[]
                {
                    "publish", projectFile,
                    "-c", "Release",
                    "-r", rid,
                    "--self-contained", "true",
                    "-o", output,
                    "-p:PublishSingleFile=true",
                    "-p:DebugType=None",
                    "-p:DebugSymbols=false"
                };
                var process = RunProcess("dotnet", arguments, ProjectPath, TimeSpan.FromMinutes(5));
                if (process.ExitCode != 0)
                    return BuildResult.Fail("dotnet publish failed:\n" + Tail(process.Output, 3000));

                var executable = Path.Combine(output, rid.StartsWith("win-", StringComparison.Ordinal)
                    ? "Shizuku.Mcp.Server.exe"
                    : "Shizuku.Mcp.Server");
                return File.Exists(executable)
                    ? BuildResult.Ok("MCP server built successfully: " + executable)
                    : BuildResult.Fail("dotnet publish completed but the MCP executable was not found.");
            }
            catch (Exception exception)
            {
                return BuildResult.Fail(exception.Message);
            }
        }

        private static void CopySource(string source, string destination)
        {
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);

            foreach (var directory in Directory.GetDirectories(source))
            {
                var name = Path.GetFileName(directory);
                if (string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var target = Path.Combine(destination, name);
                Directory.CreateDirectory(target);
                CopySource(directory, target);
            }
        }

        private static void RecreateDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        internal static string FindCommand(string command)
        {
            var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var extensions = Application.platform == RuntimePlatform.WindowsEditor
                ? new[] { ".exe", ".cmd", ".bat", string.Empty }
                : new[] { string.Empty };

            foreach (var directory in pathValue.Split(Path.PathSeparator).Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                foreach (var extension in extensions)
                {
                    var candidate = Path.Combine(directory.Trim(), command + extension);
                    if (File.Exists(candidate))
                        return candidate;
                }
            }

            return null;
        }

        internal static ProcessResult RunProcess(string command, string[] arguments, string workingDirectory, TimeSpan timeout)
        {
            var resolved = FindCommand(command) ?? command;
            var isCommandScript = Application.platform == RuntimePlatform.WindowsEditor &&
                (resolved.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                 resolved.EndsWith(".bat", StringComparison.OrdinalIgnoreCase));

            var executable = isCommandScript ? "cmd.exe" : resolved;
            var argumentText = string.Join(" ", arguments.Select(QuoteArgument));
            if (isCommandScript)
                argumentText = "/d /s /c \"\"" + resolved + "\" " + argumentText + "\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = argumentText,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start process: {command}");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                try { process.Kill(); } catch { }
                throw new TimeoutException($"Process timed out: {command}");
            }

            Task.WaitAll(outputTask, errorTask);
            return new ProcessResult(process.ExitCode, outputTask.Result + errorTask.Result);
        }

        private static string QuoteArgument(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";
            if (value.All(character => !char.IsWhiteSpace(character) && character != '"'))
                return value;
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static string Tail(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text.Substring(text.Length - maxLength);
        }

        internal readonly struct ProcessResult
        {
            public ProcessResult(int exitCode, string output)
            {
                ExitCode = exitCode;
                Output = output;
            }

            public int ExitCode { get; }
            public string Output { get; }
        }

        private readonly struct BuildResult
        {
            private BuildResult(bool success, string message)
            {
                Success = success;
                Message = message;
            }

            public bool Success { get; }
            public string Message { get; }
            public static BuildResult Ok(string message) => new(true, message);
            public static BuildResult Fail(string message) => new(false, message);
        }
    }
}
