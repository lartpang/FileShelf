using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using FileShelf.Win.Services;

namespace FileShelf.Win.Models;

public sealed class ShelfItem : INotifyPropertyChanged
{
    private bool _hasChangedPaths;
    private bool _exists;
    private bool _hasMissingPaths;
    private bool _isPinned;
    private string _languageCode = UiText.English;
    private string _name = string.Empty;

    public required IReadOnlyList<string> FilePaths { get; init; }

    public required IReadOnlyList<PathVersion> PathVersions { get; init; }

    public string Path => FilePaths[0];

    public string Name
    {
        get => _name;
        private set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    public string Extension { get; init; } = string.Empty;

    public ImageSource? IconImage { get; init; }

    public bool IsDirectory { get; init; }

    public bool IsGroup => FilePaths.Count > 1;

    public bool ContainsDirectories => PathVersions.Any(version => version.IsDirectory);

    public bool ContainsFiles => PathVersions.Any(version => version.Exists && !version.IsDirectory)
        || (!ContainsDirectories && PathVersions.Any(version => !version.Exists));

    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (_isPinned == value)
            {
                return;
            }

            _isPinned = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PinVisibility));
            OnPropertyChanged(nameof(PinToolTip));
        }
    }

    public bool Exists
    {
        get => _exists;
        private set
        {
            if (_exists == value)
            {
                return;
            }

            _exists = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MissingVisibility));
        }
    }

    public bool HasMissingPaths
    {
        get => _hasMissingPaths;
        private set
        {
            if (_hasMissingPaths == value)
            {
                return;
            }

            _hasMissingPaths = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MissingVisibility));
        }
    }

    public bool HasChangedPaths
    {
        get => _hasChangedPaths;
        private set
        {
            if (_hasChangedPaths == value)
            {
                return;
            }

            _hasChangedPaths = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(VersionVisibility));
            OnPropertyChanged(nameof(VersionText));
        }
    }

    public DateTime AddedAt { get; init; }

    public string Badge
    {
        get
        {
            if (IsGroup)
            {
                if (ContainsDirectories && !ContainsFiles)
                {
                    return $"{FilePaths.Count}D";
                }

                if (!ContainsDirectories && ContainsFiles)
                {
                    return $"{FilePaths.Count}F";
                }

                return $"{FilePaths.Count}M";
            }

            if (IsDirectory)
            {
                return "D";
            }

            return "F";
        }
    }

    public string PathSummary => IsGroup
        ? UiText.FormatGroupSummary(_languageCode, FilePaths.Count)
        : Path;

    public string FullPathToolTip => string.Join(Environment.NewLine, FilePaths);

    public Visibility MissingVisibility => HasMissingPaths ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PinVisibility => IsPinned ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IconVisibility => Visibility.Collapsed;

    public Visibility BadgeVisibility => Visibility.Visible;

    public string MissingText => UiText.Get(_languageCode, IsGroup ? "SomeFilesMissing" : "FileNotFound");

    public string VersionText => UiText.Get(_languageCode, "ShelfItemChanged");

    public Visibility VersionVisibility => HasChangedPaths ? Visibility.Visible : Visibility.Collapsed;

    public string RevealToolTip => UiText.Get(_languageCode, "RevealInExplorer");

    public string RemoveToolTip => UiText.Get(_languageCode, "Remove");

    public string PinToolTip => UiText.Get(_languageCode, IsPinned ? "Unpin" : "Pin");

    public event PropertyChangedEventHandler? PropertyChanged;

    public static ShelfItem FromPath(string path, bool isPinned = false, DateTime? addedAt = null, IReadOnlyList<PathVersion>? pathVersions = null)
    {
        var versions = pathVersions?.Count > 0 ? pathVersions.ToArray() : new[] { PathVersion.Capture(path) };
        var isDirectory = Directory.Exists(path);
        var name = System.IO.Path.GetFileName(path.TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar));

        var exists = File.Exists(path) || Directory.Exists(path);
        return new ShelfItem
        {
            FilePaths = new[] { path },
            PathVersions = versions,
            Name = string.IsNullOrWhiteSpace(name) ? path : name,
            Extension = isDirectory ? string.Empty : System.IO.Path.GetExtension(path),
            IconImage = ShellIconService.GetSmallIcon(path, isDirectory),
            IsDirectory = isDirectory,
            Exists = exists,
            HasMissingPaths = !exists,
            IsPinned = isPinned,
            AddedAt = addedAt ?? DateTime.Now
        };
    }

    public static ShelfItem FromPaths(IEnumerable<string> paths, bool isPinned = false, DateTime? addedAt = null)
    {
        return FromPaths(paths, isPinned, addedAt, null);
    }

    public static ShelfItem FromPaths(IEnumerable<string> paths, bool isPinned, DateTime? addedAt, IReadOnlyList<PathVersion>? pathVersions)
    {
        var filePaths = paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (filePaths.Length == 0)
        {
            throw new ArgumentException("At least one path is required.", nameof(paths));
        }

        if (filePaths.Length == 1)
        {
            return FromPath(filePaths[0], isPinned, addedAt, pathVersions);
        }

        var versions = pathVersions?.Count == filePaths.Length
            ? pathVersions.ToArray()
            : filePaths.Select(PathVersion.Capture).ToArray();
        var existingPaths = filePaths
            .Select(path => File.Exists(path) || Directory.Exists(path))
            .ToArray();
        var item = new ShelfItem
        {
            FilePaths = filePaths,
            PathVersions = versions,
            Name = UiText.FormatGroupName(UiText.English, filePaths.Length),
            Extension = string.Empty,
            IsDirectory = false,
            Exists = existingPaths.Any(exists => exists),
            HasMissingPaths = existingPaths.Any(exists => !exists),
            IsPinned = isPinned,
            AddedAt = addedAt ?? DateTime.Now
        };
        return item;
    }

    public bool ContainsPath(string path)
    {
        return FilePaths.Any(itemPath => string.Equals(itemPath, path, StringComparison.OrdinalIgnoreCase));
    }

    public void RefreshExists()
    {
        RefreshVersionState();
    }

    public void RefreshVersionState()
    {
        var currentVersions = FilePaths.Select(PathVersion.Capture).ToArray();
        var existingPaths = currentVersions.Select(version => version.Exists).ToArray();
        Exists = existingPaths.Any(exists => exists);
        HasMissingPaths = existingPaths.Any(exists => !exists);
        HasChangedPaths = PathVersions.Count != currentVersions.Length
            || PathVersions.Zip(currentVersions).Any(pair => !VersionsMatch(pair.First, pair.Second));
    }

    public void ApplyLanguage(string languageCode)
    {
        _languageCode = languageCode;
        if (IsGroup)
        {
            Name = UiText.FormatGroupName(_languageCode, FilePaths.Count);
        }

        OnPropertyChanged(nameof(MissingText));
        OnPropertyChanged(nameof(VersionText));
        OnPropertyChanged(nameof(PathSummary));
        OnPropertyChanged(nameof(RevealToolTip));
        OnPropertyChanged(nameof(RemoveToolTip));
        OnPropertyChanged(nameof(PinToolTip));
    }

    private static bool VersionsMatch(PathVersion stored, PathVersion current)
    {
        return string.Equals(stored.Path, current.Path, StringComparison.OrdinalIgnoreCase)
            && stored.Exists == current.Exists
            && stored.IsDirectory == current.IsDirectory
            && stored.Length == current.Length
            && stored.LastWriteTimeUtcTicks == current.LastWriteTimeUtcTicks
            && stored.ChildCount == current.ChildCount
            && string.Equals(stored.Fingerprint, current.Fingerprint, StringComparison.Ordinal);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
