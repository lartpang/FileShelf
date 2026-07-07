using System.IO;
using System.Text;

namespace FileShelf.Win.Models;

public sealed class PathVersion
{
    public string Path { get; set; } = string.Empty;

    public bool Exists { get; set; }

    public bool IsDirectory { get; set; }

    public long Length { get; set; }

    public long LastWriteTimeUtcTicks { get; set; }

    public long ChildCount { get; set; }

    public string Fingerprint { get; set; } = string.Empty;

    public static PathVersion Capture(string path)
    {
        if (File.Exists(path))
        {
            var file = new FileInfo(path);
            return new PathVersion
            {
                Path = path,
                Exists = true,
                IsDirectory = false,
                Length = file.Length,
                LastWriteTimeUtcTicks = file.LastWriteTimeUtc.Ticks,
                ChildCount = 0,
                Fingerprint = $"file|{file.Length}|{file.LastWriteTimeUtc.Ticks}"
            };
        }

        if (Directory.Exists(path))
        {
            return CaptureDirectory(path);
        }

        return new PathVersion
        {
            Path = path,
            Exists = false,
            Fingerprint = "missing"
        };
    }

    public bool MatchesCurrent()
    {
        var current = Capture(Path);
        return Exists == current.Exists
            && IsDirectory == current.IsDirectory
            && Length == current.Length
            && LastWriteTimeUtcTicks == current.LastWriteTimeUtcTicks
            && ChildCount == current.ChildCount
            && string.Equals(Fingerprint, current.Fingerprint, StringComparison.Ordinal);
    }

    private static PathVersion CaptureDirectory(string path)
    {
        var directory = new DirectoryInfo(path);
        var hash = FnvOffsetBasis;
        var childCount = 0L;
        var totalLength = 0L;
        var latestTicks = directory.LastWriteTimeUtc.Ticks;
        var pendingDirectories = new Stack<DirectoryInfo>();
        pendingDirectories.Push(directory);

        while (pendingDirectories.Count > 0)
        {
            var currentDirectory = pendingDirectories.Pop();
            FileSystemInfo[] entries;
            try
            {
                entries = currentDirectory
                    .EnumerateFileSystemInfos()
                    .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                hash = AddEntryHash(hash, path, currentDirectory.FullName, "inaccessible", 0, currentDirectory.LastWriteTimeUtc.Ticks);
                continue;
            }

            foreach (var entry in entries)
            {
                var isReparsePoint = entry.Attributes.HasFlag(FileAttributes.ReparsePoint);
                if (entry is DirectoryInfo childDirectory)
                {
                    childCount++;
                    latestTicks = Math.Max(latestTicks, childDirectory.LastWriteTimeUtc.Ticks);
                    hash = AddEntryHash(hash, path, childDirectory.FullName, isReparsePoint ? "dir-link" : "dir", 0, childDirectory.LastWriteTimeUtc.Ticks);
                    if (!isReparsePoint)
                    {
                        pendingDirectories.Push(childDirectory);
                    }

                    continue;
                }

                if (entry is not FileInfo file || !file.Exists)
                {
                    continue;
                }

                childCount++;
                totalLength += file.Length;
                latestTicks = Math.Max(latestTicks, file.LastWriteTimeUtc.Ticks);
                hash = AddEntryHash(hash, path, file.FullName, isReparsePoint ? "file-link" : "file", file.Length, file.LastWriteTimeUtc.Ticks);
            }
        }

        return new PathVersion
        {
            Path = path,
            Exists = true,
            IsDirectory = true,
            Length = totalLength,
            LastWriteTimeUtcTicks = latestTicks,
            ChildCount = childCount,
            Fingerprint = $"dir|{childCount}|{totalLength}|{latestTicks}|{hash:X16}"
        };
    }

    private const ulong FnvOffsetBasis = 14695981039346656037;
    private const ulong FnvPrime = 1099511628211;

    private static ulong AddEntryHash(ulong hash, string rootPath, string entryPath, string kind, long length, long lastWriteTicks)
    {
        var relativePath = System.IO.Path.GetRelativePath(rootPath, entryPath);
        hash = AddHash(hash, relativePath);
        hash = AddHash(hash, kind);
        hash = AddHash(hash, length.ToString());
        hash = AddHash(hash, lastWriteTicks.ToString());
        return hash;
    }

    private static ulong AddHash(ulong hash, string value)
    {
        foreach (var character in Encoding.UTF8.GetBytes(value))
        {
            hash ^= character;
            hash *= FnvPrime;
        }

        hash ^= 0xFF;
        hash *= FnvPrime;
        return hash;
    }
}
