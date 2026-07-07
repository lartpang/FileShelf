using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileShelf.Win.Models;
using FileShelf.Win.Services;
using Forms = System.Windows.Forms;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace FileShelf.Win;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly StartupShortcutService _startupShortcutService;
    private AppSettings _draftSettings;
    private bool _isInitializing = true;
    private bool _isUpdatingStartupCheckBox;

    public SettingsWindow(
        AppSettings settings,
        SettingsService settingsService,
        StartupShortcutService startupShortcutService)
    {
        InitializeComponent();

        _settings = settings;
        _settingsService = settingsService;
        _startupShortcutService = startupShortcutService;
        _draftSettings = _settings.Clone();
        _draftSettings.StartWithWindows = _startupShortcutService.IsEnabled();

        RefreshControlsFromDraft();
        _isInitializing = false;
    }

    private string LanguageCode => _draftSettings.LanguageCode;

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || LanguageComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string languageCode)
        {
            return;
        }

        _draftSettings.LanguageCode = languageCode;
        ApplyLanguage();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateDraftSettings())
        {
            return;
        }

        var previousSettings = _settings.Clone();
        var previousStartup = _startupShortcutService.IsEnabled();
        try
        {
            _startupShortcutService.SetEnabled(_draftSettings.StartWithWindows, LanguageCode);
            _settings.CopyFrom(_draftSettings);
            PortablePaths.Configure(_settings);
            _settingsService.Save(_settings);
        }
        catch (StartupShortcutConflictException ex)
        {
            RestoreSettings(previousSettings, previousStartup);
            System.Windows.MessageBox.Show(
                $"{UiText.Get(LanguageCode, "StartupUpdateFailed")}\n\n{UiText.FormatPath(LanguageCode, "StartupShortcutConflict", ex.ShortcutPath)}",
                "FileShelf",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }
        catch (StartupExecutablePathUnavailableException)
        {
            RestoreSettings(previousSettings, previousStartup);
            System.Windows.MessageBox.Show(
                $"{UiText.Get(LanguageCode, "StartupUpdateFailed")}\n\n{UiText.Get(LanguageCode, "StartupExecutablePathUnavailable")}",
                "FileShelf",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }
        catch (Exception ex)
        {
            RestoreSettings(previousSettings, previousStartup);
            System.Windows.MessageBox.Show(
                $"{UiText.Get(LanguageCode, "SettingsSaveFailed")}\n\n{ex.Message}",
                "FileShelf",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void RestoreSettings(AppSettings previousSettings, bool previousStartup)
    {
        try
        {
            _startupShortcutService.SetEnabled(previousStartup, previousSettings.LanguageCode);
        }
        catch
        {
            // Best-effort rollback; the original failure is shown to the user.
        }

        _settings.CopyFrom(previousSettings);
        PortablePaths.Configure(_settings);
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        _draftSettings = SettingsService.CreateDefaultSettings();
        RefreshControlsFromDraft();
    }

    private void StartupCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || _isUpdatingStartupCheckBox)
        {
            return;
        }

        _draftSettings.StartWithWindows = StartupCheckBox.IsChecked == true;
    }

    private void DataPathTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        BeginPathEdit(DataPathTextBlock, DataPathTextBox);
    }

    private void LogPathTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        BeginPathEdit(LogPathTextBlock, LogPathTextBox);
    }

    private void DataPathTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitPathEdit(DataPathTextBlock, DataPathTextBox, isDataPath: true);
    }

    private void LogPathTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitPathEdit(LogPathTextBlock, LogPathTextBox, isDataPath: false);
    }

    private void DataBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = UiText.Get(LanguageCode, "BrowseDataPath"),
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(PortablePaths.GetDataDirectory(_draftSettings))
                ? PortablePaths.GetDataDirectory(_draftSettings)
                : AppContext.BaseDirectory
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        _draftSettings.DataDirectoryPath = dialog.SelectedPath;
        UpdatePathFields();
    }

    private void LogBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var currentLogPath = PortablePaths.GetLogPath(_draftSettings);
        var currentLogDirectory = Path.GetDirectoryName(currentLogPath);
        var dialog = new SaveFileDialog
        {
            Title = UiText.Get(LanguageCode, "BrowseLogPath"),
            Filter = UiText.Get(LanguageCode, "LogFileFilter"),
            FileName = Path.GetFileName(currentLogPath),
            InitialDirectory = !string.IsNullOrWhiteSpace(currentLogDirectory) && Directory.Exists(currentLogDirectory)
                ? currentLogDirectory
                : AppContext.BaseDirectory
        };

        if (dialog.ShowDialog(this) != true || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        _draftSettings.LogFilePath = dialog.FileName;
        UpdatePathFields();
    }

    private void BeginPathEdit(TextBlock display, WpfTextBox editor)
    {
        editor.Text = display.Text;
        display.Visibility = Visibility.Collapsed;
        editor.Visibility = Visibility.Visible;
        editor.Focus();
        editor.SelectAll();
    }

    private void CommitPathEdit(TextBlock display, WpfTextBox editor, bool isDataPath)
    {
        var path = editor.Text.Trim();
        if (isDataPath)
        {
            _draftSettings.DataDirectoryPath = path;
        }
        else
        {
            _draftSettings.LogFilePath = path;
        }

        UpdatePathFields();
        editor.Visibility = Visibility.Collapsed;
        display.Visibility = Visibility.Visible;
    }

    private bool ValidateDraftSettings()
    {
        try
        {
            var dataDirectory = PortablePaths.GetDataDirectory(_draftSettings);
            Directory.CreateDirectory(dataDirectory);
            EnsureDirectoryWritable(dataDirectory);

            var logPath = PortablePaths.GetLogPath(_draftSettings);
            if (Directory.Exists(logPath))
            {
                throw new IOException(UiText.Get(LanguageCode, "LogPathMustBeFile"));
            }

            var logDirectory = Path.GetDirectoryName(logPath);
            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                throw new IOException(UiText.Get(LanguageCode, "LogPathInvalid"));
            }

            Directory.CreateDirectory(logDirectory);
            EnsureDirectoryWritable(logDirectory);
            using var _ = new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            System.Windows.MessageBox.Show(
                $"{UiText.Get(LanguageCode, "SettingsPathValidationFailed")}\n\n{ex.Message}",
                "FileShelf",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private static void EnsureDirectoryWritable(string directory)
    {
        var testPath = Path.Combine(directory, $".fileshelf-write-test-{Guid.NewGuid():N}.tmp");
        using (File.Create(testPath))
        {
        }

        File.Delete(testPath);
    }

    private void ApplyLanguage()
    {
        Title = UiText.Get(LanguageCode, "PortableTitle");
        SettingsTitleTextBlock.Text = UiText.Get(LanguageCode, "Settings");
        LanguageLabelTextBlock.Text = UiText.Get(LanguageCode, "Language");
        SetComboBoxItemText(UiText.English, UiText.Get(LanguageCode, "English"));
        SetComboBoxItemText(UiText.Chinese, UiText.Get(LanguageCode, "Chinese"));
        StartupLabelTextBlock.Text = UiText.Get(LanguageCode, "Startup");
        StartupCheckBox.Content = UiText.Get(LanguageCode, "StartWithWindows");
        DataLabelTextBlock.Text = UiText.Get(LanguageCode, "Data");
        DataPathHintTextBlock.Text = UiText.Get(LanguageCode, "DataPathApplyHint");
        LogLabelTextBlock.Text = UiText.Get(LanguageCode, "Log");
        DataBrowseButton.ToolTip = UiText.Get(LanguageCode, "BrowseDataPath");
        LogBrowseButton.ToolTip = UiText.Get(LanguageCode, "BrowseLogPath");
        CancelButton.Content = UiText.Get(LanguageCode, "Cancel");
        ResetDefaultsButton.Content = UiText.Get(LanguageCode, "RestoreDefaults");
        SaveButton.Content = UiText.Get(LanguageCode, "Apply");
    }

    private void RefreshControlsFromDraft()
    {
        _isInitializing = true;
        SelectLanguage(_draftSettings.LanguageCode);
        SetStartupCheckBox(_draftSettings.StartWithWindows);
        UpdatePathFields();
        ApplyLanguage();
        _isInitializing = false;
    }

    private void UpdatePathFields()
    {
        DataPathTextBlock.Text = PortablePaths.ToDisplayPath(PortablePaths.GetDataDirectory(_draftSettings));
        DataPathTextBlock.ToolTip = DataPathTextBlock.Text;
        DataPathTextBox.Text = DataPathTextBlock.Text;

        LogPathTextBlock.Text = PortablePaths.ToDisplayPath(PortablePaths.GetLogPath(_draftSettings));
        LogPathTextBlock.ToolTip = LogPathTextBlock.Text;
        LogPathTextBox.Text = LogPathTextBlock.Text;
    }

    private void SelectLanguage(string languageCode)
    {
        SelectComboBoxTag(LanguageComboBox, languageCode);
    }

    private void SetStartupCheckBox(bool enabled)
    {
        _isUpdatingStartupCheckBox = true;
        StartupCheckBox.IsChecked = enabled;
        _isUpdatingStartupCheckBox = false;
    }

    private static void SelectComboBoxTag(WpfComboBox comboBox, string tag)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag as string, tag, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }

    private void SetComboBoxItemText(string languageCode, string text)
    {
        SetComboBoxItemText(LanguageComboBox, languageCode, text);
    }

    private static void SetComboBoxItemText(WpfComboBox comboBox, string tag, string text)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag as string, tag, StringComparison.OrdinalIgnoreCase))
            {
                item.Content = text;
                return;
            }
        }
    }
}
