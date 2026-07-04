using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using FileShelf.Win.Models;

namespace FileShelf.Win.Services;

public sealed class ShelfStateService
{
    private readonly LoggerService _logger;

    public ShelfStateService(LoggerService logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<ShelfItem> Load()
    {
        var statePath = PortablePaths.ShelfStatePath;
        if (!File.Exists(statePath))
        {
            return Array.Empty<ShelfItem>();
        }

        try
        {
            var json = File.ReadAllText(statePath);
            var records = JsonSerializer.Deserialize<ShelfRecord[]>(json) ?? Array.Empty<ShelfRecord>();
            return records
                .Select(CreateShelfItem)
                .Where(item => item is not null)
                .Select(item => item!)
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.Error("Shelf state load failed; empty shelf restored", ex);
            BackupBrokenState(statePath);
            return Array.Empty<ShelfItem>();
        }
    }

    public void Save(ObservableCollection<ShelfItem> items)
    {
        try
        {
            Directory.CreateDirectory(PortablePaths.DataDirectory);
            var records = items
                .Select(item => new ShelfRecord
                {
                    Path = item.Path,
                    Paths = item.FilePaths.ToArray(),
                    IsPinned = item.IsPinned,
                    AddedAt = item.AddedAt
                })
                .ToArray();
            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(PortablePaths.ShelfStatePath, json);
        }
        catch (Exception ex)
        {
            _logger.Error("Shelf state save failed", ex);
        }
    }

    private static ShelfItem? CreateShelfItem(ShelfRecord record)
    {
        if (record.Paths is { Length: > 0 })
        {
            return ShelfItem.FromPaths(record.Paths, record.IsPinned, record.AddedAt);
        }

        return string.IsNullOrWhiteSpace(record.Path)
            ? null
            : ShelfItem.FromPath(record.Path, record.IsPinned, record.AddedAt);
    }

    private void BackupBrokenState(string statePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(statePath);
            if (directory is null)
            {
                return;
            }

            var brokenPath = Path.Combine(directory, $"shelf.broken.{DateTime.Now:yyyyMMddHHmmss}.json");
            File.Move(statePath, brokenPath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.Error("Broken shelf state backup failed", ex);
        }
    }

    private sealed class ShelfRecord
    {
        public string? Path { get; set; }

        public string[]? Paths { get; set; }

        public bool IsPinned { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
