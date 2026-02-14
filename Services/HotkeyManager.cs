using System.Runtime.InteropServices;
using System.Windows.Interop;
using Quanta.Models;

namespace Quanta.Services;

public class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312, HOTKEY_ID = 9000;
    private const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_WIN = 0x0008;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private IntPtr _windowHandle;
    private HwndSource? _source;
    private bool _isRegistered, _disposed;
    public event EventHandler? HotkeyPressed;

    public void Initialize(IntPtr windowHandle, HotkeyConfig config)
    {
        _windowHandle = windowHandle;
        _source = HwndSource.FromHwnd(windowHandle);
        _source?.AddHook(HwndHook);
        RegisterHotkey(config);
    }

    private void RegisterHotkey(HotkeyConfig config)
    {
        if (_isRegistered) UnregisterHotKey(_windowHandle, HOTKEY_ID);
        uint modifiers = config.Modifier.ToUpper() switch { "ALT" => MOD_ALT, "CTRL" => MOD_CONTROL, "SHIFT" => MOD_SHIFT, "WIN" => MOD_WIN, _ => MOD_ALT };
        uint vk = config.Key.ToUpper() switch { "SPACE" => 0x20, "ENTER" => 0x0D, "ESCAPE" => 0x1B, _ => (config.Key.Length == 1 && char.IsLetter(config.Key[0])) ? (uint)char.ToUpper(config.Key[0]) : 0x20 };
        _isRegistered = RegisterHotKey(_windowHandle, HOTKEY_ID, modifiers, vk);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID) { HotkeyPressed?.Invoke(this, EventArgs.Empty); handled = true; }
        return IntPtr.Zero;
    }

    public void Reregister(HotkeyConfig config) => RegisterHotkey(config);
    public void Dispose() { if (!_disposed) { if (_isRegistered) UnregisterHotKey(_windowHandle, HOTKEY_ID); _source?.RemoveHook(HwndHook); _disposed = true; } }
}
