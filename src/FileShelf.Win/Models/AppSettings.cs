namespace FileShelf.Win.Models;

public sealed class AppSettings
{
    public string LanguageCode { get; set; } = "en";

    public bool StartWithWindows { get; set; }

    public string DataDirectoryPath { get; set; } = string.Empty;

    public string LogFilePath { get; set; } = string.Empty;

    public AppSettings Clone()
    {
        return new AppSettings
        {
            LanguageCode = LanguageCode,
            StartWithWindows = StartWithWindows,
            DataDirectoryPath = DataDirectoryPath,
            LogFilePath = LogFilePath
        };
    }

    public void CopyFrom(AppSettings source)
    {
        LanguageCode = source.LanguageCode;
        StartWithWindows = source.StartWithWindows;
        DataDirectoryPath = source.DataDirectoryPath;
        LogFilePath = source.LogFilePath;
    }
}
