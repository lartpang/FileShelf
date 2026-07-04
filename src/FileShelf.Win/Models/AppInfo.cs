namespace FileShelf.Win.Models;

public sealed class AppInfo
{
    public string Version { get; set; } = string.Empty;

    public string Tag { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public string LatestReleaseApiUrl { get; set; } = string.Empty;

    public string LatestReleasePageUrl { get; set; } = string.Empty;
}
