using System.Drawing;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;

namespace FileShelf.Win.Services;

public sealed class TrayService : IDisposable
{
    private readonly Action _toggleShelf;
    private readonly Action _showSettings;
    private readonly Action _showAbout;
    private readonly Action _exit;
    private readonly string? _iconPath;
    private Forms.ToolStripItem? _settingsMenuItem;
    private Forms.ToolStripItem? _aboutMenuItem;
    private Forms.ToolStripItem? _exitMenuItem;
    private Forms.NotifyIcon? _notifyIcon;
    private Icon? _icon;

    public TrayService(Action toggleShelf, Action showSettings, Action showAbout, Action exit, string? iconPath = null)
    {
        _toggleShelf = toggleShelf;
        _showSettings = showSettings;
        _showAbout = showAbout;
        _exit = exit;
        _iconPath = iconPath;
    }

    public void Initialize()
    {
        var menu = new Forms.ContextMenuStrip();
        _settingsMenuItem = menu.Items.Add(UiText.Get(UiText.English, "Settings"), null, (_, _) => Dispatch(_showSettings));
        _aboutMenuItem = menu.Items.Add(UiText.Get(UiText.English, "About"), null, (_, _) => Dispatch(_showAbout));
        menu.Items.Add(new Forms.ToolStripSeparator());
        _exitMenuItem = menu.Items.Add(UiText.Get(UiText.English, "Exit"), null, (_, _) => Dispatch(_exit));

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = UiText.FormatTrayTooltip(UiText.English, 0),
            Icon = LoadIcon(),
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.MouseUp += NotifyIcon_MouseUp;
    }

    public void UpdateText(string languageCode, int itemCount)
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Text = TrimNotifyText(UiText.FormatTrayTooltip(languageCode, itemCount));
        }

        if (_settingsMenuItem is not null)
        {
            _settingsMenuItem.Text = UiText.Get(languageCode, "Settings");
        }

        if (_aboutMenuItem is not null)
        {
            _aboutMenuItem.Text = UiText.Get(languageCode, "About");
        }

        if (_exitMenuItem is not null)
        {
            _exitMenuItem.Text = UiText.Get(languageCode, "Exit");
        }
    }

    public void Dispose()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _notifyIcon = null;
        _icon?.Dispose();
        _icon = null;
    }

    private void NotifyIcon_MouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            Dispatch(_toggleShelf);
        }
    }

    private static void Dispatch(Action action)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(action);
    }

    private Icon LoadIcon()
    {
        if (!string.IsNullOrWhiteSpace(_iconPath))
        {
            var iconPath = Path.IsPathRooted(_iconPath)
                ? _iconPath
                : Path.Combine(AppContext.BaseDirectory, _iconPath);

            if (File.Exists(iconPath))
            {
                _icon = new Icon(iconPath);
                return _icon;
            }
        }

        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            _icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);
            if (_icon is not null)
            {
                return _icon;
            }
        }

        return SystemIcons.Application;
    }

    private static string TrimNotifyText(string text)
    {
        return text.Length <= 63 ? text : text[..63];
    }
}
