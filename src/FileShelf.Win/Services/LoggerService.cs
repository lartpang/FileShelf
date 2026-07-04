using System.IO;

namespace FileShelf.Win.Services;

public sealed class LoggerService
{
    private const long MaxLogBytes = 1024 * 1024;
    private readonly object _syncRoot = new();
    private bool _enabled = true;

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warning(string message)
    {
        Write("WARN", message);
    }

    public void Error(string message, Exception? exception = null)
    {
        var text = exception is null ? message : $"{message}: {exception.GetType().Name}: {exception.Message}";
        Write("ERROR", text);
    }

    private void Write(string level, string message)
    {
        lock (_syncRoot)
        {
            if (!_enabled)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(PortablePaths.LogDirectory);
                RotateIfNeeded();
                File.AppendAllText(
                    PortablePaths.LogPath,
                    $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}");
            }
            catch
            {
                _enabled = false;
            }
        }
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(PortablePaths.LogPath))
        {
            return;
        }

        var file = new FileInfo(PortablePaths.LogPath);
        if (file.Length <= MaxLogBytes)
        {
            return;
        }

        var archivePath = Path.Combine(PortablePaths.LogDirectory, "fileshelf.previous.log");
        File.Copy(PortablePaths.LogPath, archivePath, overwrite: true);
        File.WriteAllText(PortablePaths.LogPath, string.Empty);
    }
}
