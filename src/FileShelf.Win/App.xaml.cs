using System.Windows;
using FileShelf.Win.Models;
using FileShelf.Win.Services;
using WpfPoint = System.Windows.Point;

namespace FileShelf.Win;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Local\FileShelf.Win.SingleInstance";
    private const string ShowShelfEventName = @"Local\FileShelf.Win.ShowShelf";

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _showShelfEvent;
    private RegisteredWaitHandle? _showShelfWaitHandle;
    private bool _hasSingleInstanceLock;
    private MainWindow? _mainWindow;
    private TrayService? _trayService;
    private LoggerService? _logger;
    private SettingsService? _settingsService;
    private ShelfStateService? _shelfStateService;
    private AppSettings? _settings;
    private SettingsWindow? _settingsWindow;
    private AboutWindow? _aboutWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out _hasSingleInstanceLock);
        if (!_hasSingleInstanceLock)
        {
            SignalExistingInstance();
            Shutdown();
            return;
        }

        InitializeSingleInstanceSignal();

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += (_, args) =>
        {
            _logger?.Error("Unhandled UI exception", args.Exception);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                _logger?.Error("Unhandled process exception", exception);
            }
        };

        _logger = new LoggerService();
        _settingsService = new SettingsService(_logger);
        _settings = _settingsService.Load();
        PortablePaths.Configure(_settings);
        _logger.Info("Application starting");
        _shelfStateService = new ShelfStateService(_logger);

        _mainWindow = new MainWindow(_settings, _logger, _shelfStateService);
        _mainWindow.ItemCountChanged += (_, count) => UpdateTrayText(count);
        _trayService = new TrayService(
            toggleShelf: ToggleShelf,
            showSettings: ShowSettings,
            showAbout: ShowAbout,
            exit: ExitApplication,
            iconPath: "Resources\\FileShelfIconNotion.ico");
        _trayService.Initialize();
        UpdateTrayText(_mainWindow.ItemCount);

        _mainWindow.ShowIconAtDefault();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SaveWindowSettings();
        _trayService?.Dispose();
        _logger?.Info("Application exited");
        _showShelfWaitHandle?.Unregister(null);
        _showShelfEvent?.Dispose();
        if (_hasSingleInstanceLock)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void ShowShelf(bool activate = true, WpfPoint? triggerPosition = null)
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.CancelHideAnimation();
        _mainWindow.ShowPanel(activate, triggerPosition);
    }

    private void ToggleShelf()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (_mainWindow.IsPanelOpen && !_mainWindow.IsHidingToTray)
        {
            _mainWindow.HideToTray();
            return;
        }

        ShowShelf();
    }

    private void ShowSettings()
    {
        if (_settings is null || _settingsService is null || _mainWindow is null)
        {
            return;
        }

        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var settingsWindow = new SettingsWindow(
            _settings,
            _settingsService,
            new StartupShortcutService())
        {
            ShowInTaskbar = false
        };
        _settingsWindow = settingsWindow;
        settingsWindow.Closed += (_, _) => _settingsWindow = null;
        var previousLogPath = PortablePaths.LogPath;
        if (settingsWindow.ShowDialog() == true)
        {
            if (!PathsEqual(previousLogPath, PortablePaths.LogPath))
            {
                _logger?.Reset();
            }

            _mainWindow.ApplySettings(_settings);
            _mainWindow.SaveShelfState();
            UpdateTrayText(_mainWindow.ItemCount);
        }
    }

    private void ShowAbout()
    {
        if (_settings is null)
        {
            return;
        }

        if (_aboutWindow is not null)
        {
            _aboutWindow.Activate();
            return;
        }

        var aboutWindow = new AboutWindow(_settings)
        {
            ShowInTaskbar = false
        };
        _aboutWindow = aboutWindow;
        aboutWindow.Closed += (_, _) => _aboutWindow = null;
        aboutWindow.ShowDialog();
    }

    private void ExitApplication()
    {
        _mainWindow?.AllowClose();
        Shutdown();
    }

    private void UpdateTrayText(int itemCount)
    {
        if (_settings is not null)
        {
            _trayService?.UpdateText(_settings.LanguageCode, itemCount);
        }
    }

    private void ApplyLanguage()
    {
        if (_settings is null || _mainWindow is null)
        {
            return;
        }

        _mainWindow.ApplyLanguage();
        UpdateTrayText(_mainWindow.ItemCount);
    }

    private void InitializeSingleInstanceSignal()
    {
        _showShelfEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowShelfEventName);
        _showShelfWaitHandle = ThreadPool.RegisterWaitForSingleObject(
            _showShelfEvent,
            (_, timedOut) =>
            {
                if (timedOut)
                {
                    return;
                }

                Dispatcher.BeginInvoke(() => ShowShelf());
            },
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var showShelfEvent = EventWaitHandle.OpenExisting(ShowShelfEventName);
            showShelfEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                System.IO.Path.GetFullPath(left),
                System.IO.Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void SaveWindowSettings()
    {
        if (_mainWindow is null || _settings is null || _settingsService is null)
        {
            return;
        }

        try
        {
            _settingsService.Save(_settings);
        }
        catch (Exception ex)
        {
            _logger?.Error("Window settings save failed", ex);
        }
    }
}
