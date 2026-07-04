using System.Windows;
using System.Windows.Interop;
using FileShelf.Win.Models;
using FileShelf.Win.Services;
using Forms = System.Windows.Forms;
using DrawingPoint = System.Drawing.Point;
using WpfPoint = System.Windows.Point;

namespace FileShelf.Win;

public partial class App : System.Windows.Application
{
    private Mutex? _singleInstanceMutex;
    private bool _hasSingleInstanceLock;
    private MainWindow? _mainWindow;
    private TrayService? _trayService;
    private HotkeyService? _hotkeyService;
    private MouseHookService? _mouseHookService;
    private LoggerService? _logger;
    private SettingsService? _settingsService;
    private ShelfStateService? _shelfStateService;
    private AppSettings? _settings;
    private bool _hotkeyRegistered;
    private string _mouseHookMode = "Manual";
    private string _mouseHookDockMode = "RightCenter";
    private string _mouseHookIgnoredProcesses = string.Empty;
    private double _mouseHookShelfHeight;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, @"Local\FileShelf.Win.SingleInstance", out _hasSingleInstanceLock);
        if (!_hasSingleInstanceLock)
        {
            Shutdown();
            return;
        }

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

        _mainWindow = new MainWindow(_settings, _logger, _shelfStateService, AddIgnoredProcess);
        _mainWindow.ItemCountChanged += (_, count) => UpdateTrayText(count);
        _trayService = new TrayService(
            toggleShelf: ToggleShelf,
            showSettings: ShowSettings,
            showAbout: ShowAbout,
            exit: ExitApplication,
            iconPath: "Resources\\FileShelfIconNotion.ico");
        _trayService.Initialize();
        UpdateTrayText(_mainWindow.ItemCount);

        _mainWindow.SourceInitialized += (_, _) =>
        {
            _hotkeyService = new HotkeyService(_mainWindow);
            _hotkeyService.HotkeyPressed += (_, _) => ShowShelf();
            _hotkeyRegistered = _hotkeyService.Register();
            _logger.Info(_hotkeyRegistered ? "Global hotkey registered" : "Global hotkey unavailable");
        };

        _ = new WindowInteropHelper(_mainWindow).EnsureHandle();
        ConfigureMouseHook();
        _mainWindow.Hide();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SaveWindowSettings();
        _mouseHookService?.Dispose();
        _hotkeyService?.Dispose();
        _trayService?.Dispose();
        _logger?.Info("Application exited");
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
        PositionShelf(triggerPosition);
        var wasShowActivated = _mainWindow.ShowActivated;
        var shouldAnimate = !_mainWindow.IsVisible;
        _mainWindow.ShowActivated = activate;

        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.ShowActivated = wasShowActivated;
        if (activate)
        {
            _mainWindow.Activate();
        }

        _mainWindow.Topmost = true;
        if (shouldAnimate)
        {
            _mainWindow.PlayShowAnimation();
        }
    }

    private void ToggleShelf()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (_mainWindow.IsVisible && !_mainWindow.IsHidingToTray)
        {
            _mainWindow.HideToTray();
            return;
        }

        ShowShelf();
    }

    private void PositionShelf(WpfPoint? triggerPosition = null)
    {
        if (_mainWindow is null || _settings is null)
        {
            return;
        }

        var area = GetWorkArea(triggerPosition);
        var isRight = _settings.ShelfDockMode.StartsWith("Right", StringComparison.OrdinalIgnoreCase);
        _mainWindow.Left = isRight
            ? area.Right - _mainWindow.Width + 10
            : area.Left - 10;

        _mainWindow.Top = _settings.ShelfDockMode switch
        {
            "LeftTop" or "RightTop" => area.Top + 24,
            "LeftBottom" or "RightBottom" => area.Bottom - _mainWindow.Height - 24,
            _ => area.Top + (area.Height - _mainWindow.Height) / 2
        };
    }

    private static Rect GetWorkArea(WpfPoint? triggerPosition)
    {
        if (triggerPosition is null)
        {
            return SystemParameters.WorkArea;
        }

        var screen = Forms.Screen.FromPoint(new DrawingPoint(
            (int)Math.Round(triggerPosition.Value.X),
            (int)Math.Round(triggerPosition.Value.Y)));
        var area = screen.WorkingArea;
        return new Rect(area.Left, area.Top, area.Width, area.Height);
    }

    private void ShowSettings()
    {
        if (_settings is null || _settingsService is null || _mainWindow is null)
        {
            return;
        }

        CaptureCurrentWindowSize();
        var settingsWindow = new SettingsWindow(
            _settings,
            _settingsService,
            _hotkeyRegistered,
            () => _mainWindow.ClearShelf())
        {
            Owner = _mainWindow.IsVisible ? _mainWindow : null
        };
        if (settingsWindow.ShowDialog() == true)
        {
            _mainWindow.ApplySettings(_settings);
            _mainWindow.SaveShelfState();
            UpdateTrayText(_mainWindow.ItemCount);
            ConfigureMouseHook();
        }
    }

    private void ShowAbout()
    {
        if (_settings is null)
        {
            return;
        }

        var aboutWindow = new AboutWindow(_settings)
        {
            Owner = _mainWindow?.IsVisible == true ? _mainWindow : null
        };
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

    private void SaveWindowSettings()
    {
        if (_mainWindow is null || _settings is null || _settingsService is null)
        {
            return;
        }

        try
        {
            CaptureCurrentWindowSize();
            _settingsService.Save(_settings);
        }
        catch (Exception ex)
        {
            _logger?.Error("Window settings save failed", ex);
        }
    }

    private void ConfigureMouseHook()
    {
        if (_settings is not null && _settings.TriggerMode != "Manual")
        {
            if (_mouseHookService is not null &&
                _mouseHookMode == _settings.TriggerMode &&
                _mouseHookDockMode == _settings.ShelfDockMode &&
                Math.Abs(_mouseHookShelfHeight - _settings.ShelfHeight) < 1 &&
                string.Equals(_mouseHookIgnoredProcesses, _settings.IgnoredProcessNames, StringComparison.Ordinal))
            {
                return;
            }

            _mouseHookService?.Dispose();
            _mouseHookService = new MouseHookService(
                _settings.TriggerMode,
                _settings.ShelfDockMode,
                _settings.ShelfHeight,
                _settings.IgnoredProcessNames);
            _mouseHookMode = _settings.TriggerMode;
            _mouseHookDockMode = _settings.ShelfDockMode;
            _mouseHookShelfHeight = _settings.ShelfHeight;
            _mouseHookIgnoredProcesses = _settings.IgnoredProcessNames;
            _mouseHookService.DragTriggerDetected += (_, args) => Dispatcher.Invoke(() =>
            {
                _mainWindow?.SetLastTriggerSourceProcess(args.SourceProcessName);
                ShowShelf(activate: false, triggerPosition: args.Position);
            });

            if (!_mouseHookService.Start())
            {
                _logger?.Warning("Mouse trigger hook unavailable");
                _mouseHookService.Dispose();
                _mouseHookService = null;
                _mouseHookMode = "Manual";
                _mouseHookDockMode = "RightCenter";
                _mouseHookShelfHeight = 0;
                _mouseHookIgnoredProcesses = string.Empty;
            }
            else
            {
                _logger?.Info($"Mouse trigger hook enabled; mode={_settings.TriggerMode}");
            }

            return;
        }

        _mouseHookService?.Dispose();
        _mouseHookService = null;
        _mouseHookMode = "Manual";
        _mouseHookDockMode = "RightCenter";
        _mouseHookShelfHeight = 0;
        _mouseHookIgnoredProcesses = string.Empty;
        _logger?.Info("Mouse trigger hook disabled");
    }

    private void CaptureCurrentWindowSize()
    {
        if (_mainWindow is null || _settings is null)
        {
            return;
        }

        _settings.ShelfWidth = _mainWindow.Width;
        _settings.ShelfHeight = _mainWindow.Height;
    }

    private void AddIgnoredProcess(string processName)
    {
        if (_settings is null || _settingsService is null || string.IsNullOrWhiteSpace(processName))
        {
            return;
        }

        var names = _settings.IgnoredProcessNames
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (names.Any(name => string.Equals(NormalizeProcessName(name), NormalizeProcessName(processName), StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        names.Add(NormalizeProcessName(processName));
        _settings.IgnoredProcessNames = string.Join(", ", names);
        _settingsService.Save(_settings);
        ConfigureMouseHook();
        _logger?.Info($"Ignored app added; process={NormalizeProcessName(processName)}");
    }

    private static string NormalizeProcessName(string processName)
    {
        var trimmed = processName.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }
}
