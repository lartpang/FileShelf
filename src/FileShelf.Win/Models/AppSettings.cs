namespace FileShelf.Win.Models;

public sealed class AppSettings
{
    public string DesignProfile { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = "en";

    public bool EnableDragTrigger { get; set; }

    public string TriggerMode { get; set; } = string.Empty;

    public string ShelfDockMode { get; set; } = "RightCenter";

    public bool StartWithWindows { get; set; }

    public string DataDirectoryPath { get; set; } = string.Empty;

    public string LogFilePath { get; set; } = string.Empty;
}
