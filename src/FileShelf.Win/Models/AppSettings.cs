namespace FileShelf.Win.Models;

public sealed class AppSettings
{
    public string DesignProfile { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = "en";

    public bool EnableDragTrigger { get; set; }

    public string TriggerMode { get; set; } = string.Empty;

    public double ShelfWidth { get; set; } = 335;

    public double ShelfHeight { get; set; } = 540;

    public string ShelfDockMode { get; set; } = "RightCenter";

    public string DataDirectoryPath { get; set; } = string.Empty;

    public string LogFilePath { get; set; } = string.Empty;
}
