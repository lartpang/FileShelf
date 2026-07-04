using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileShelf.Win.Models;
using FileShelf.Win.Services;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace FileShelf.Win;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly bool _hotkeyRegistered;
    private readonly Action _clearShelf;
    private bool _isInitializing = true;
    private static readonly ShelfSizePreset[] SizePresets =
    {
        new("Small", 260, 430),
        new("Medium", 335, 540),
        new("Large", 410, 620)
    };

    public SettingsWindow(
        AppSettings settings,
        SettingsService settingsService,
        bool hotkeyRegistered,
        Action clearShelf)
    {
        InitializeComponent();

        _settings = settings;
        _settingsService = settingsService;
        _hotkeyRegistered = hotkeyRegistered;
        _clearShelf = clearShelf;

        SelectLanguage(_settings.LanguageCode);
        SelectComboBoxTag(TriggerModeComboBox, _settings.TriggerMode);
        SelectComboBoxTag(DockModeComboBox, _settings.ShelfDockMode);
        SelectShelfSize();
        IgnoredAppsTextBox.Text = _settings.IgnoredProcessNames;
        HotkeyTextBlock.Text = _settings.HotkeyText;
        HotkeyStatusTextBlock.Foreground = _hotkeyRegistered
            ? System.Windows.Media.Brushes.ForestGreen
            : System.Windows.Media.Brushes.Firebrick;
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
        SaveSettings();
        ApplyLanguage();
    }

    private void TriggerModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateTriggerHint();
    }

    private void ClearShelf_Click(object sender, RoutedEventArgs e)
    {
        _clearShelf();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.TriggerMode = GetSelectedTag(TriggerModeComboBox, "Manual");
        _settings.EnableDragTrigger = _settings.TriggerMode != "Manual";
        _settings.ShelfDockMode = GetSelectedTag(DockModeComboBox, "RightCenter");
        ApplyShelfSizePreset();
        _settings.IgnoredProcessNames = IgnoredAppsTextBox.Text.Trim();
        if (!SaveSettings())
        {
            return;
        }

        DialogResult = true;
        Close();
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

    private void PathTextBox_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (sender is not WpfTextBox textBox)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            UpdatePathFields();
            textBox.Visibility = Visibility.Collapsed;
            DataPathTextBlock.Visibility = Visibility.Visible;
            LogPathTextBlock.Visibility = Visibility.Visible;
            e.Handled = true;
        }
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
        TriggerLabelTextBlock.Text = UiText.Get(LanguageCode, "Trigger");
        PositionLabelTextBlock.Text = UiText.Get(LanguageCode, "Position");
        SizeLabelTextBlock.Text = UiText.Get(LanguageCode, "Size");
        IgnoredAppsLabelTextBlock.Text = UiText.Get(LanguageCode, "IgnoredApps");
        UpdateTriggerHint();
        IgnoredAppsTextBox.ToolTip = UiText.Get(LanguageCode, "IgnoredAppsHint");
        SetComboBoxItemText(TriggerModeComboBox, "Manual", UiText.Get(LanguageCode, "TriggerManual"));
        SetComboBoxItemText(TriggerModeComboBox, "AnyDrag", UiText.Get(LanguageCode, "TriggerAnyDrag"));
        SetComboBoxItemText(TriggerModeComboBox, "AltDrag", UiText.Get(LanguageCode, "TriggerAltDrag"));
        SetComboBoxItemText(TriggerModeComboBox, "DockZone", UiText.Get(LanguageCode, "TriggerDockZone"));
        SetComboBoxItemText(TriggerModeComboBox, "ScreenEdge", UiText.Get(LanguageCode, "TriggerScreenEdge"));
        SetComboBoxItemText(DockModeComboBox, "LeftTop", UiText.Get(LanguageCode, "LeftTop"));
        SetComboBoxItemText(DockModeComboBox, "LeftCenter", UiText.Get(LanguageCode, "LeftCenter"));
        SetComboBoxItemText(DockModeComboBox, "LeftBottom", UiText.Get(LanguageCode, "LeftBottom"));
        SetComboBoxItemText(DockModeComboBox, "RightTop", UiText.Get(LanguageCode, "RightTop"));
        SetComboBoxItemText(DockModeComboBox, "RightCenter", UiText.Get(LanguageCode, "RightCenter"));
        SetComboBoxItemText(DockModeComboBox, "RightBottom", UiText.Get(LanguageCode, "RightBottom"));
        SetComboBoxItemText(ShelfSizeComboBox, "Small", UiText.Get(LanguageCode, "SizeSmall"));
        SetComboBoxItemText(ShelfSizeComboBox, "Medium", UiText.Get(LanguageCode, "SizeMedium"));
        SetComboBoxItemText(ShelfSizeComboBox, "Large", UiText.Get(LanguageCode, "SizeLarge"));
        SetComboBoxItemText(ShelfSizeComboBox, "Custom", UiText.Get(LanguageCode, "SizeCustom"));
        HotkeyLabelTextBlock.Text = UiText.Get(LanguageCode, "Hotkey");
        StatusLabelTextBlock.Text = UiText.Get(LanguageCode, "Status");
        HotkeyStatusTextBlock.Text = UiText.Get(LanguageCode, _hotkeyRegistered ? "Registered" : "Unavailable");
        DataLabelTextBlock.Text = UiText.Get(LanguageCode, "Data");
        LogLabelTextBlock.Text = UiText.Get(LanguageCode, "Log");
        ClearShelfButton.Content = UiText.Get(LanguageCode, "ClearUnpinned");
        CancelButton.Content = UiText.Get(LanguageCode, "Cancel");
        SaveButton.Content = UiText.Get(LanguageCode, "Save");
    }

    private void UpdateTriggerHint()
    {
        var triggerMode = GetSelectedTag(TriggerModeComboBox, _settings.TriggerMode);
        TriggerHintTextBlock.Text = UiText.Get(LanguageCode, $"TriggerHint{triggerMode}");
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

    private void SelectShelfSize()
    {
        foreach (var preset in SizePresets)
        {
            if (Math.Abs(_settings.ShelfWidth - preset.Width) < 1 &&
                Math.Abs(_settings.ShelfHeight - preset.Height) < 1)
            {
                SelectComboBoxTag(ShelfSizeComboBox, preset.Tag);
                return;
            }
        }

        SelectComboBoxTag(ShelfSizeComboBox, "Custom");
    }

    private void ApplyShelfSizePreset()
    {
        var selectedTag = GetSelectedTag(ShelfSizeComboBox, "Custom");
        var preset = SizePresets.FirstOrDefault(item => string.Equals(item.Tag, selectedTag, StringComparison.Ordinal));
        if (preset is null)
        {
            return;
        }

        _settings.ShelfWidth = preset.Width;
        _settings.ShelfHeight = preset.Height;
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

    private static string GetSelectedTag(WpfComboBox comboBox, string fallback)
    {
        return comboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag
            ? tag
            : fallback;
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

    private sealed record ShelfSizePreset(string Tag, double Width, double Height);
}
