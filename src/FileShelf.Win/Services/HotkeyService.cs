using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FileShelf.Win.Services;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x46534846;
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint VkSpace = 0x20;

    private readonly Window _window;
    private HwndSource? _source;
    private nint _handle;
    private bool _registered;

    public HotkeyService(Window window)
    {
        _window = window;
    }

    public event EventHandler? HotkeyPressed;

    public bool Register()
    {
        _handle = new WindowInteropHelper(_window).Handle;
        if (_handle == nint.Zero)
        {
            return false;
        }

        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WndProc);

        _registered = RegisterHotKey(_handle, HotkeyId, ModControl | ModAlt, VkSpace);
        return _registered;
    }

    public void Dispose()
    {
        if (_registered)
        {
            UnregisterHotKey(_handle, HotkeyId);
            _registered = false;
        }

        _source?.RemoveHook(WndProc);
        _source = null;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return nint.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);
}
