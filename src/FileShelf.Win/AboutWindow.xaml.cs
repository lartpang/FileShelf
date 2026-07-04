using System.Reflection;
using System.Windows;
using FileShelf.Win.Models;
using FileShelf.Win.Services;

namespace FileShelf.Win;

public partial class AboutWindow : Window
{
    private readonly AppSettings _settings;

    public AboutWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        ApplyText();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ApplyText()
    {
        var language = _settings.LanguageCode;
        Title = UiText.Get(language, "AboutTitle");
        TitleTextBlock.Text = "FileShelf";
        SubtitleTextBlock.Text = UiText.Get(language, "AboutSubtitle");
        VersionLabelTextBlock.Text = UiText.Get(language, "Version");
        RuntimeLabelTextBlock.Text = UiText.Get(language, "Runtime");
        ModeLabelTextBlock.Text = UiText.Get(language, "Mode");
        DataLabelTextBlock.Text = UiText.Get(language, "Data");
        LogLabelTextBlock.Text = UiText.Get(language, "Log");
        DevelopmentLabelTextBlock.Text = UiText.Get(language, "Development");
        ScopeLabelTextBlock.Text = UiText.Get(language, "Scope");
        CloseButton.Content = UiText.Get(language, "Close");

        VersionTextBlock.Text = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        RuntimeTextBlock.Text = $".NET {Environment.Version} / {Environment.OSVersion.VersionString}";
        ModeTextBlock.Text = UiText.Get(language, "PortableTitle");
        DataTextBlock.Text = PortablePaths.ToDisplayPath(PortablePaths.DataDirectory);
        DataTextBlock.ToolTip = DataTextBlock.Text;
        LogTextBlock.Text = PortablePaths.ToDisplayPath(PortablePaths.LogPath);
        LogTextBlock.ToolTip = LogTextBlock.Text;
        DevelopmentTextBlock.Text = UiText.Get(language, "DevelopmentValue");
        ScopeTextBlock.Text = UiText.Get(language, "ScopeValue");
        ScopeTextBlock.ToolTip = ScopeTextBlock.Text;
    }
}
