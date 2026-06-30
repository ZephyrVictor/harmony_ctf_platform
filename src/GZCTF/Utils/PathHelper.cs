namespace GZCTF.Utils;

internal enum DirType : byte
{
    Logs,
    Uploads,
    Capture
}

internal static class PathHelper
{
    internal const string Base = "files";

    internal static readonly string Logs = GetDir(DirType.Logs);
    internal static readonly string Uploads = GetDir(DirType.Uploads);
    internal static readonly string Capture = GetDir(DirType.Capture);

    /// <summary>
    /// URL path prefix for sub-path deployment (e.g. "/harmonyctf").
    /// Set from the "PathBase" config at startup. Empty/null for root deployment.
    /// </summary>
    internal static string? UrlPrefix { get; private set; }

    /// <summary>
    /// Initialize UrlPrefix from configuration. Call once at startup.
    /// </summary>
    internal static void InitializeUrlPrefix(IConfiguration configuration)
    {
        UrlPrefix = string.IsNullOrWhiteSpace(configuration["PathBase"])
            ? null
            : configuration["PathBase"]!.TrimEnd('/');
    }

    /// <summary>
    /// Prepend the configured UrlPrefix to a path that starts with "/".
    /// For root deployment (UrlPrefix is null/empty), returns the path unchanged.
    /// </summary>
    internal static string PrefixUrl(string path)
    {
        if (string.IsNullOrEmpty(UrlPrefix))
            return path;
        return $"{UrlPrefix}{path}";
    }

    internal static bool AllowBaseCreate(IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return true;

        var know = Environment.GetEnvironmentVariable("YES_I_KNOW_FILES_ARE_NOT_PERSISTED_GO_AHEAD_PLEASE");
        return know is not null;
    }

    internal static async Task EnsureDirsAsync(IHostEnvironment environment)
    {
        if (!Directory.Exists(Base))
        {
            if (AllowBaseCreate(environment))
                Directory.CreateDirectory(Base);
            else
                ExitWithFatalMessage(
                    StaticLocalizer[nameof(Resources.Program.Init_NoFilesDir), Path.GetFullPath(Base)]);
        }

        await using (var versionFile = File.Open(Path.Combine(Base, "version.txt"), FileMode.Create))
        await using (var writer = new StreamWriter(versionFile))
        {
            await writer.WriteLineAsync(typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");
        }

        // only create logs directory
        var path = Path.Combine(Base, GetDir(DirType.Logs));
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    /// <summary>
    /// 获取文件夹路径
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static string GetDir(DirType type) => type.ToString().ToLower();
}
