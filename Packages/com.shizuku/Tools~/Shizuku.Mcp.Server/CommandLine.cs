namespace Shizuku.Mcp.Server;

internal static class CommandLine
{
    public static string GetRequiredProjectPath(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], "--project", StringComparison.OrdinalIgnoreCase))
                continue;

            var path = Path.GetFullPath(args[i + 1]);
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Unity project does not exist: {path}");

            return path;
        }

        throw new ArgumentException("Missing required argument: --project <Unity project path>");
    }
}
