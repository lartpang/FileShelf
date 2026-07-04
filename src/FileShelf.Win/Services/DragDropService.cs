using System.IO;
using System.Windows;

namespace FileShelf.Win.Services;

public sealed class DragDropService
{
    public IReadOnlyList<string> ExtractFilePaths(System.Windows.IDataObject data)
    {
        if (!data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            return Array.Empty<string>();
        }

        if (data.GetData(System.Windows.DataFormats.FileDrop) is not string[] paths)
        {
            return Array.Empty<string>();
        }

        return paths
            .Where(path => File.Exists(path) || Directory.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public System.Windows.DataObject CreateFileDropData(IEnumerable<string> paths)
    {
        var existingPaths = paths
            .Where(path => File.Exists(path) || Directory.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new System.Windows.DataObject(System.Windows.DataFormats.FileDrop, existingPaths);
    }
}
