using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using FileShelf.Win.Services;

namespace FileShelf.Win.Models;

public sealed class ShelfItem : INotifyPropertyChanged
{
    private bool _exists;
    private bool _hasMissingPaths;
    private bool _isPinned;
    private string _languageCode = UiText.English;
    private string _name = string.Empty;

    public required IReadOnlyList<string> FilePaths { get; init; }

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

    public DateTime AddedAt { get; init; }

    public string Badge
    {
        get
        {
            if (IsGroup)
            {
                return FilePaths.Count.ToString();
            }

            if (IsDirectory)
            {
                return "DIR";
            }

            return string.IsNullOrWhiteSpace(Extension)
                ? "FILE"
                : Extension.TrimStart('.').ToUpperInvariant();
        }
    }

    public string PathSummary => IsGroup
        ? UiText.FormatGroupSummary(_languageCode, FilePaths.Count)
        : Path;

    public string FullPathToolTip => string.Join(Environment.NewLine, FilePaths);

    public Visibility MissingVisibility => HasMissingPaths ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PinVisibility => IsPinned ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IconVisibility => IconImage is null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility BadgeVisibility => IconImage is null ? Visibility.Visible : Visibility.Collapsed;

    public string MissingText => UiText.Get(_languageCode, IsGroup ? "SomeFilesMissing" : "FileNotFound");

    public string RevealToolTip => UiText.Get(_languageCode, "RevealInExplorer");

    public string RemoveToolTip => UiText.Get(_languageCode, "Remove");

    public string PinToolTip => UiText.Get(_languageCode, IsPinned ? "Unpin" : "Pin");

    public event PropertyChangedEventHandler? PropertyChanged;

    public static ShelfItem FromPath(string path, bool isPinned = false, DateTime? addedAt = null)
    {
        var isDirectory = Directory.Exists(path);
        var name = System.IO.Path.GetFileName(path.TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar));

        var exists = File.Exists(path) || Directory.Exists(path);
        return new ShelfItem
        {
            FilePaths = new[] { path },
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
            return FromPath(filePaths[0], isPinned, addedAt);
        }

        var existingPaths = filePaths
            .Select(path => File.Exists(path) || Directory.Exists(path))
            .ToArray();
        var item = new ShelfItem
        {
            FilePaths = filePaths,
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
        var existingPaths = FilePaths
            .Select(path => File.Exists(path) || Directory.Exists(path))
            .ToArray();
        Exists = existingPaths.Any(exists => exists);
        HasMissingPaths = existingPaths.Any(exists => !exists);
    }

    public void ApplyLanguage(string languageCode)
    {
        _languageCode = languageCode;
        if (IsGroup)
        {
            Name = UiText.FormatGroupName(_languageCode, FilePaths.Count);
        }

        OnPropertyChanged(nameof(MissingText));
        OnPropertyChanged(nameof(PathSummary));
        OnPropertyChanged(nameof(RevealToolTip));
        OnPropertyChanged(nameof(RemoveToolTip));
        OnPropertyChanged(nameof(PinToolTip));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
