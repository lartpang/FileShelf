using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using FileShelf.Win.Models;

namespace FileShelf.Win.Services;

public sealed class AppInfoService
{
    private const string AppInfoFileName = "FileShelf.app.json";
    private static readonly object UpdateCheckLock = new();
    private static readonly Dictionary<string, Lazy<Task<UpdateCheckResult>>> UpdateCheckTasks = new(StringComparer.OrdinalIgnoreCase);
    private readonly AppInfo _appInfo;

    public AppInfoService()
    {
        _appInfo = LoadAppInfo();
    }

    public AppInfo Current => _appInfo;

    public string DisplayVersion => !string.IsNullOrWhiteSpace(_appInfo.Version)
        ? _appInfo.Version
        : GetAssemblyVersion();

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_appInfo.LatestReleaseApiUrl))
        {
            return UpdateCheckResult.Unavailable;
        }

        var cacheKey = $"{_appInfo.LatestReleaseApiUrl}|{DisplayVersion}";
        Lazy<Task<UpdateCheckResult>> updateCheck;
        lock (UpdateCheckLock)
        {
            if (!UpdateCheckTasks.TryGetValue(cacheKey, out updateCheck!))
            {
                updateCheck = new Lazy<Task<UpdateCheckResult>>(
                    CheckForUpdateCoreAsync,
                    LazyThreadSafetyMode.ExecutionAndPublication);
                UpdateCheckTasks[cacheKey] = updateCheck;
            }
        }

        return await updateCheck.Value.WaitAsync(cancellationToken);
    }

    private async Task<UpdateCheckResult> CheckForUpdateCoreAsync()
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(6)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FileShelf");
        try
        {
            using var response = await client.GetAsync(_appInfo.LatestReleaseApiUrl);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            var root = document.RootElement;
            var latestTag = GetJsonString(root, "tag_name");
            var latestVersion = NormalizeVersion(latestTag);
            var releaseUrl = GetJsonString(root, "html_url");

            if (string.IsNullOrWhiteSpace(latestVersion))
            {
                return UpdateCheckResult.Unavailable;
            }

            var currentVersion = NormalizeVersion(DisplayVersion);
            var isNewer = TryCompareVersions(latestVersion, currentVersion, out var comparison)
                ? comparison > 0
                : !string.Equals(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase);

            return isNewer
                ? UpdateCheckResult.Available(latestVersion, releaseUrl)
                : UpdateCheckResult.Current(latestVersion, releaseUrl);
        }
        catch
        {
            return UpdateCheckResult.Failed;
        }
    }

    public void OpenLatestReleasePage()
    {
        var url = string.IsNullOrWhiteSpace(_appInfo.LatestReleasePageUrl)
            ? _appInfo.LatestReleaseApiUrl
            : _appInfo.LatestReleasePageUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    private static AppInfo LoadAppInfo()
    {
        var appInfoPath = Path.Combine(AppContext.BaseDirectory, AppInfoFileName);
        if (!File.Exists(appInfoPath))
        {
            return new AppInfo();
        }

        try
        {
            var json = File.ReadAllText(appInfoPath);
            return JsonSerializer.Deserialize<AppInfo>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AppInfo();
        }
        catch
        {
            return new AppInfo();
        }
    }

    private static string GetAssemblyVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return !string.IsNullOrWhiteSpace(informationalVersion)
            ? informationalVersion
            : assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    private static string GetJsonString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string NormalizeVersion(string version)
    {
        var normalized = version.Trim();
        return normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? normalized[1..]
            : normalized;
    }

    private static bool TryCompareVersions(string left, string right, out int comparison)
    {
        comparison = 0;
        var leftCore = left.Split('-', '+')[0];
        var rightCore = right.Split('-', '+')[0];
        if (!Version.TryParse(leftCore, out var leftVersion) || !Version.TryParse(rightCore, out var rightVersion))
        {
            return false;
        }

        comparison = leftVersion.CompareTo(rightVersion);
        return true;
    }
}

public sealed record UpdateCheckResult(bool IsAvailable, bool IsConfigured, bool IsFailed, string LatestVersion, string ReleaseUrl)
{
    public static UpdateCheckResult Unavailable { get; } = new(false, false, false, string.Empty, string.Empty);

    public static UpdateCheckResult Failed { get; } = new(false, true, true, string.Empty, string.Empty);

    public static UpdateCheckResult Current(string latestVersion, string releaseUrl)
    {
        return new UpdateCheckResult(false, true, false, latestVersion, releaseUrl);
    }

    public static UpdateCheckResult Available(string latestVersion, string releaseUrl)
    {
        return new UpdateCheckResult(true, true, false, latestVersion, releaseUrl);
    }
}
