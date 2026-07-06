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
    private bool _isInitializing = true;
    private bool _isUpdatingStartupCheckBox;

    public event EventHandler? LanguageChanged;

    public SettingsWindow(
        AppSettings settings,
        SettingsService settingsService,
        StartupShortcutService startupShortcutService)
    {
        InitializeComponent();

        _settings = settings;
        _settingsService = settingsService;
        _startupShortcutService = startupShortcutService;

        SelectLanguage(_settings.LanguageCode);
        _settings.StartWithWindows = _startupShortcutService.IsEnabled();
        SetStartupCheckBox(_settings.StartWithWindows);
        UpdatePathFields();
        ApplyLanguage();

        _isInitializing = false;
    }

    private string LanguageCode => _settings.LanguageCode;

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || LanguageComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string languageCode)
        {
            return;
        }

        _settings.LanguageCode = languageCode;
        var saved = SaveSettings();
        ApplyLanguage();
        if (saved)
        {
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.TriggerMode = "Manual";
        _settings.EnableDragTrigger = false;
        if (!SaveSettings())
        {
            return;
        }

        DialogResult = true;
        Close();
    }

    private void StartupCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || _isUpdatingStartupCheckBox)
        {
            return;
        }

        var previous = _startupShortcutService.IsEnabled();
        var enabled = StartupCheckBox.IsChecked == true;

        try
        {
            _startupShortcutService.SetEnabled(enabled, LanguageCode);
            _settings.StartWithWindows = enabled;
            if (!SaveSettings())
            {
                _startupShortcutService.SetEnabled(previous, LanguageCode);
                _settings.StartWithWindows = previous;
                SetStartupCheckBox(previous);
            }
        }
        catch (StartupShortcutConflictException ex)
        {
            _settings.StartWithWindows = previous;
            SetStartupCheckBox(previous);
            System.Windows.MessageBox.Show(
                $"{UiText.Get(LanguageCode, "StartupUpdateFailed")}\n\n{UiText.FormatPath(LanguageCode, "StartupShortcutConflict", ex.ShortcutPath)}",
                "FileShelf",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (StartupExecutablePathUnavailableException)
        {
            _settings.StartWithWindows = previous;
            SetStartupCheckBox(previous);
            System.Windows.MessageBox.Show(
                $"{UiText.Get(LanguageCode, "StartupUpdateFailed")}\n\n{UiText.Get(LanguageCode, "StartupExecutablePathUnavailable")}",
                "FileShelf",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            _settings.StartWithWindows = previous;
            SetStartupCheckBox(previous);
            System.Windows.MessageBox.Show(
                $"{UiText.Get(LanguageCode, "StartupUpdateFailed")}\n\n{ex.Message}",
                "FileShelf",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
            SelectedPath = Directory.Exists(PortablePaths.DataDirectory)
                ? PortablePaths.DataDirectory
                : AppContext.BaseDirectory
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        _settings.DataDirectoryPath = dialog.SelectedPath;
        PortablePaths.Configure(_settings);
        SaveSettings();
        UpdatePathFields();
    }

    private void LogBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var currentLogPath = PortablePaths.LogPath;
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

        _settings.LogFilePath = dialog.FileName;
        PortablePaths.Configure(_settings);
        SaveSettings();
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
            _settings.DataDirectoryPath = path;
        }
        else
        {
            _settings.LogFilePath = path;
        }

        PortablePaths.Configure(_settings);
        SaveSettings();
        UpdatePathFields();
        editor.Visibility = Visibility.Collapsed;
        display.Visibility = Visibility.Visible;
    }

    private bool SaveSettings()
    {
        try
        {
            _settingsService.Save(_settings);
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"{UiText.Get(LanguageCode, "SettingsSaveFailed")}\n\n{ex.Message}",
                "FileShelf",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
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
        LogLabelTextBlock.Text = UiText.Get(LanguageCode, "Log");
        DataBrowseButton.ToolTip = UiText.Get(LanguageCode, "BrowseDataPath");
        LogBrowseButton.ToolTip = UiText.Get(LanguageCode, "BrowseLogPath");
        CancelButton.Content = UiText.Get(LanguageCode, "Cancel");
        SaveButton.Content = UiText.Get(LanguageCode, "Save");
    }

    private void UpdatePathFields()
    {
        DataPathTextBlock.Text = PortablePaths.ToDisplayPath(PortablePaths.DataDirectory);
        DataPathTextBlock.ToolTip = DataPathTextBlock.Text;
        DataPathTextBox.Text = DataPathTextBlock.Text;

        LogPathTextBlock.Text = PortablePaths.ToDisplayPath(PortablePaths.LogPath);
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
