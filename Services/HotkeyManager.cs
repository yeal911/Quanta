using System.Runtime.InteropServices;
using System.Windows.Interop;
using Quanta.Models;
using Quanta.Helpers;

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
        if (_isRegistered) 
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            _isRegistered = false;
        }

        uint modifiers = config.Modifier?.ToUpper() switch 
        { 
            "ALT" => MOD_ALT, 
            "CTRL" => MOD_CONTROL, 
            "SHIFT" => MOD_SHIFT, 
            "WIN" => MOD_WIN, 
            _ => MOD_ALT 
        };

        uint vk = ParseVirtualKey(config.Key);
        
        Logger.Log($"[Hotkey] Registering: Modifier={config.Modifier}({modifiers}), Key={config.Key}({vk})");
        
        _isRegistered = RegisterHotKey(_windowHandle, HOTKEY_ID, modifiers, vk);
        
        if (!_isRegistered)
        {
            Logger.Log($"[Hotkey] Failed to register hotkey!");
        }
    }

    private uint ParseVirtualKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return 0x20; // Space

        return key.ToUpper() switch
        {
            // Function keys
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
            "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
            "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
            // Special keys
            "SPACE" => 0x20,
            "ENTER" => 0x0D,
            "ESCAPE" => 0x1B,
            "TAB" => 0x09,
            "BACKSPACE" => 0x08,
            "DELETE" => 0x2E,
            "INSERT" => 0x2D,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            // Arrow keys
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            // Numbers
            "0" => 0x30, "1" => 0x31, "2" => 0x32, "3" => 0x33, "4" => 0x34,
            "5" => 0x35, "6" => 0x36, "7" => 0x37, "8" => 0x38, "9" => 0x39,
            // Letters
            _ when key.Length == 1 && char.IsLetter(key[0]) => (uint)char.ToUpper(key[0]),
            // Default to space
            _ => 0x20
        };
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID) 
        { 
            Logger.Log("[Hotkey] Hotkey pressed!");
            HotkeyPressed?.Invoke(this, EventArgs.Empty); 
            handled = true; 
        }
        return IntPtr.Zero;
    }

    public void Reregister(HotkeyConfig config) => RegisterHotkey(config);
    public void Dispose() 
    { 
        if (!_disposed) 
        { 
            if (_isRegistered) UnregisterHotKey(_windowHandle, HOTKEY_ID); 
            _source?.RemoveHook(HwndHook); 
            _disposed = true; 
        } 
    }
}
