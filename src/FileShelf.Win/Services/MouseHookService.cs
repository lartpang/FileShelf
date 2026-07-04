using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using DrawingPoint = System.Drawing.Point;
using Forms = System.Windows.Forms;
using WpfPoint = System.Windows.Point;

namespace FileShelf.Win.Services;

public sealed class MouseHookService : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int VkMenu = 0x12;
    private const int DragThreshold = 20;
    private const int EdgeThreshold = 8;
    private const int DockZoneWidth = 48;
    private const int MinimumDockZoneHeight = 120;
    private static readonly TimeSpan Cooldown = TimeSpan.FromMilliseconds(800);

    private readonly HookProc _hookProc;
    private readonly string _triggerMode;
    private readonly string _dockMode;
    private readonly double _shelfHeight;
    private readonly HashSet<string> _ignoredProcesses;
    private nint _hookHandle;
    private nint _cachedForegroundWindow;
    private bool _hasCachedScreen;
    private System.Drawing.Rectangle _cachedScreenBounds;
    private Rect _cachedWorkingArea;
    private string? _cachedForegroundProcessName;
    private bool _isLeftDown;
    private WpfPoint _startPosition;
    private DateTime _lastTriggerAt = DateTime.MinValue;

    public MouseHookService(string triggerMode, string dockMode, double shelfHeight, string ignoredProcessNames)
    {
        _triggerMode = triggerMode;
        _dockMode = dockMode;
        _shelfHeight = shelfHeight;
        _ignoredProcesses = ParseIgnoredProcesses(ignoredProcessNames);
        _hookProc = HookCallback;
    }

    public event EventHandler<DragTriggerEventArgs>? DragTriggerDetected;

    public bool Start()
    {
        if (_hookHandle != nint.Zero)
        {
            return true;
        }

        _hookHandle = SetWindowsHookEx(WhMouseLl, _hookProc, GetModuleHandle(null), 0);
        return _hookHandle != nint.Zero;
    }

    public void Dispose()
    {
        if (_hookHandle != nint.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = nint.Zero;
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var message = wParam.ToInt32();
            var hookData = Marshal.PtrToStructure<MouseHookStruct>(lParam);
            var position = new WpfPoint(hookData.Point.X, hookData.Point.Y);

            if (message == WmLButtonDown)
            {
                _isLeftDown = true;
                _startPosition = position;
            }
            else if (message == WmLButtonUp)
            {
                _isLeftDown = false;
            }
            else if (message == WmMouseMove)
            {
                TryDetectDragTrigger(position);
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void TryDetectDragTrigger(WpfPoint position)
    {
        if (!_isLeftDown)
        {
            return;
        }

        var distanceX = Math.Abs(position.X - _startPosition.X);
        var distanceY = Math.Abs(position.Y - _startPosition.Y);
        if (distanceX < DragThreshold && distanceY < DragThreshold)
        {
            return;
        }

        var now = DateTime.Now;
        if (now - _lastTriggerAt < Cooldown)
        {
            return;
        }

        if (!ShouldTrigger(position))
        {
            return;
        }

        var sourceProcessName = GetForegroundProcessName();
        if (IsProcessIgnored(sourceProcessName))
        {
            return;
        }

        _lastTriggerAt = now;
        _isLeftDown = false;
        DragTriggerDetected?.Invoke(this, new DragTriggerEventArgs(position, sourceProcessName));
    }

    private bool ShouldTrigger(WpfPoint position)
    {
        return _triggerMode switch
        {
            "AnyDrag" => true,
            "AltDrag" => IsAltPressed(),
            "DockZone" => IsInDockStartZone(),
            "ScreenEdge" => IsNearDockEdge(position),
            _ => false
        };
    }

    private bool IsInDockStartZone()
    {
        var area = GetWorkingArea(_startPosition);
        var isLeft = _dockMode.StartsWith("Left", StringComparison.OrdinalIgnoreCase);
        var isRight = _dockMode.StartsWith("Right", StringComparison.OrdinalIgnoreCase);
        if (!isLeft && !isRight)
        {
            return false;
        }

        var inHorizontalZone = isLeft
            ? _startPosition.X <= area.Left + DockZoneWidth
            : _startPosition.X >= area.Right - DockZoneWidth;
        if (!inHorizontalZone)
        {
            return false;
        }

        return IsInsideDockVerticalBand(area, _startPosition.Y);
    }

    private bool IsNearDockEdge(WpfPoint position)
    {
        var area = GetWorkingArea(position);
        if (_dockMode.StartsWith("Left", StringComparison.OrdinalIgnoreCase))
        {
            return position.X <= area.Left + EdgeThreshold && IsInsideDockVerticalBand(area, position.Y);
        }

        if (_dockMode.StartsWith("Right", StringComparison.OrdinalIgnoreCase))
        {
            return position.X >= area.Right - EdgeThreshold && IsInsideDockVerticalBand(area, position.Y);
        }

        return false;
    }

    private bool IsInsideDockVerticalBand(Rect area, double y)
    {
        var zoneHeight = Math.Clamp(_shelfHeight, MinimumDockZoneHeight, Math.Max(MinimumDockZoneHeight, area.Height - 48));
        var zoneTop = _dockMode switch
        {
            "LeftTop" or "RightTop" => area.Top + 24,
            "LeftBottom" or "RightBottom" => area.Bottom - zoneHeight - 24,
            _ => area.Top + (area.Height - zoneHeight) / 2
        };
        var zoneBottom = zoneTop + zoneHeight;
        return y >= zoneTop && y <= zoneBottom;
    }

    private Rect GetWorkingArea(WpfPoint position)
    {
        var x = (int)Math.Round(position.X);
        var y = (int)Math.Round(position.Y);
        if (_hasCachedScreen && _cachedScreenBounds.Contains(x, y))
        {
            return _cachedWorkingArea;
        }

        var screen = Forms.Screen.FromPoint(new DrawingPoint(
            x,
            y));
        var area = screen.WorkingArea;
        _cachedScreenBounds = screen.Bounds;
        _cachedWorkingArea = new Rect(area.Left, area.Top, area.Width, area.Height);
        _hasCachedScreen = true;
        return _cachedWorkingArea;
    }

    private static bool IsAltPressed()
    {
        return (GetKeyState(VkMenu) & 0x8000) != 0;
    }

    private bool IsProcessIgnored(string? processName)
    {
        return !string.IsNullOrWhiteSpace(processName) && _ignoredProcesses.Contains(processName);
    }

    private string? GetForegroundProcessName()
    {
        var window = GetForegroundWindow();
        if (window == nint.Zero)
        {
            return null;
        }

        if (window == _cachedForegroundWindow)
        {
            return _cachedForegroundProcessName;
        }

        _cachedForegroundWindow = window;
        _ = GetWindowThreadProcessId(window, out var processId);
        if (processId == 0)
        {
            _cachedForegroundProcessName = null;
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            var processName = NormalizeProcessName(process.ProcessName);
            _cachedForegroundProcessName = processName;
            return processName;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
        {
            _cachedForegroundProcessName = null;
            return null;
        }
    }

    private static HashSet<string> ParseIgnoredProcesses(string names)
    {
        return names
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeProcessName)
            .Where(name => name.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeProcessName(string name)
    {
        var trimmed = name.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }

    private delegate nint HookProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct MouseHookStruct
    {
        public readonly NativePoint Point;
        public readonly uint MouseData;
        public readonly uint Flags;
        public readonly uint Time;
        public readonly nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint
    {
        public readonly int X;
        public readonly int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}

public sealed class DragTriggerEventArgs : EventArgs
{
    public DragTriggerEventArgs(WpfPoint position, string? sourceProcessName)
    {
        Position = position;
        SourceProcessName = sourceProcessName;
    }

    public WpfPoint Position { get; }

    public string? SourceProcessName { get; }
}
