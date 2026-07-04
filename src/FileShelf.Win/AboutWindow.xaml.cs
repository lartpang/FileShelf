using System.Windows;
using System.Windows.Media;
using FileShelf.Win.Models;
using FileShelf.Win.Services;

namespace FileShelf.Win;

public partial class AboutWindow : Window
{
    private readonly AppInfoService _appInfoService = new();
    private readonly AppSettings _settings;

    public AboutWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        ApplyText();
        Loaded += AboutWindow_Loaded;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OpenReleaseButton_Click(object sender, RoutedEventArgs e)
    {
        _appInfoService.OpenLatestReleasePage();
    }

    private void ApplyText()
    {
        var language = _settings.LanguageCode;
        Title = UiText.Get(language, "AboutTitle");
        TitleTextBlock.Text = "FileShelf";
        SubtitleTextBlock.Text = UiText.Get(language, "AboutSubtitle");
        VersionLabelTextBlock.Text = UiText.Get(language, "Version");
        RuntimeLabelTextBlock.Text = UiText.Get(language, "Runtime");
        UpdateLabelTextBlock.Text = UiText.Get(language, "Update");
        ModeLabelTextBlock.Text = UiText.Get(language, "Mode");
        DataLabelTextBlock.Text = UiText.Get(language, "Data");
        LogLabelTextBlock.Text = UiText.Get(language, "Log");
        DevelopmentLabelTextBlock.Text = UiText.Get(language, "Development");
        ScopeLabelTextBlock.Text = UiText.Get(language, "Scope");
        OpenReleaseButton.Content = UiText.Get(language, "OpenRelease");
        CloseButton.Content = UiText.Get(language, "Close");

        VersionTextBlock.Text = _appInfoService.DisplayVersion;
        RuntimeTextBlock.Text = $".NET {Environment.Version} / {Environment.OSVersion.VersionString}";
        UpdateTextBlock.Text = string.IsNullOrWhiteSpace(_appInfoService.Current.LatestReleaseApiUrl)
            ? UiText.Get(language, "UpdateNotConfigured")
            : UiText.Get(language, "UpdateChecking");
        ModeTextBlock.Text = UiText.Get(language, "PortableTitle");
        DataTextBlock.Text = PortablePaths.ToDisplayPath(PortablePaths.DataDirectory);
        DataTextBlock.ToolTip = DataTextBlock.Text;
        LogTextBlock.Text = PortablePaths.ToDisplayPath(PortablePaths.LogPath);
        LogTextBlock.ToolTip = LogTextBlock.Text;
        DevelopmentTextBlock.Text = UiText.Get(language, "DevelopmentValue");
        ScopeTextBlock.Text = UiText.Get(language, "ScopeValue");
        ScopeTextBlock.ToolTip = ScopeTextBlock.Text;
    }

    private async void AboutWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_appInfoService.Current.LatestReleaseApiUrl))
        {
            return;
        }

        var language = _settings.LanguageCode;
        try
        {
            var result = await _appInfoService.CheckForUpdateAsync();
            if (!result.IsConfigured)
            {
                UpdateTextBlock.Text = UiText.Get(language, "UpdateNotConfigured");
                return;
            }

            if (result.IsFailed)
            {
                UpdateTextBlock.Text = UiText.Get(language, "UpdateCheckFailed");
                return;
            }

            if (result.IsAvailable)
            {
                UpdateTextBlock.Text = UiText.FormatVersion(language, "UpdateAvailable", result.LatestVersion);
                UpdateTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x16, 0x73, 0x4A));
                OpenReleaseButton.Visibility = Visibility.Visible;
                return;
            }

            UpdateTextBlock.Text = UiText.FormatVersion(language, "UpdateCurrent", result.LatestVersion);
        }
        catch
        {
            UpdateTextBlock.Text = UiText.Get(language, "UpdateCheckFailed");
        }
    }
}
