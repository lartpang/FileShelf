using System.IO;
using FileShelf.Win.Models;

namespace FileShelf.Win.Services;

public static class PortablePaths
{
    public static string DefaultDataDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "FileShelfData");

    public static string DefaultLogPath => Path.Combine(DefaultDataDirectory, "logs", "fileshelf.log");

    public static string DataDirectory { get; private set; } = DefaultDataDirectory;

    public static string LogPath { get; private set; } = DefaultLogPath;

    public static string SettingsPath => Path.Combine(DefaultDataDirectory, "settings.json");

    public static string ShelfStatePath => Path.Combine(DataDirectory, "shelf.json");

    public static string LogDirectory => Path.GetDirectoryName(LogPath) ?? DefaultDataDirectory;

    public static void Configure(AppSettings settings)
    {
        DataDirectory = ResolvePath(settings.DataDirectoryPath, DefaultDataDirectory);
        LogPath = ResolvePath(settings.LogFilePath, Path.Combine(DataDirectory, "logs", "fileshelf.log"));
    }

    public static string ToDisplayPath(string path)
    {
        return Path.GetFullPath(path);
    }

    private static string ResolvePath(string configuredPath, string fallback)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return fallback;
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
    }
}
