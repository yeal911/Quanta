// ============================================================================
// 文件名: HotkeyManager.cs
// 文件用途: 提供全局热键注册与管理服务。通过调用 Windows API（user32.dll）
//          实现系统级别的全局热键监听，支持自定义修饰键和按键组合。
//          当用户按下已注册的热键时，会触发 HotkeyPressed 事件。
// ============================================================================

using System.Runtime.InteropServices;
using System.Windows.Interop;
using Quanta.Core.Interfaces;
using Quanta.Helpers;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 全局热键管理器，负责注册、监听和注销系统级全局热键。
/// 通过 Win32 API 实现热键功能，使用 WPF 的 HwndSource 挂钩窗口消息处理。
/// 实现 IDisposable 接口以确保在销毁时正确注销热键并释放资源。
/// </summary>
public class HotkeyManager : IHotkeyManager
{
    /// <summary>
    /// Windows 热键消息常量（WM_HOTKEY = 0x0312）
    /// </summary>
    /// <summary>
    /// 热键注册使用的唯一标识 ID
    /// </summary>
    private const int WM_HOTKEY = 0x0312, HOTKEY_ID = 9000;

    /// <summary>
    /// 修饰键常量：Alt、Ctrl、Shift、Win 键对应的标志位
    /// </summary>
    private const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_WIN = 0x0008;

    /// <summary>
    /// Win32 API：注册全局热键
    /// </summary>
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    /// <summary>
    /// Win32 API：注销全局热键
    /// </summary>
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>
    /// 关联窗口的句柄，用于接收热键消息
    /// </summary>
    private IntPtr _windowHandle;

    /// <summary>
    /// WPF 窗口消息源，用于挂钩和处理 Windows 消息
    /// </summary>
    private HwndSource? _source;

    /// <summary>
    /// 标记热键是否已成功注册
    /// </summary>
    /// <summary>
    /// 标记对象是否已被释放
    /// </summary>
    private bool _isRegistered, _disposed;

    /// <summary>
    /// 当注册的全局热键被按下时触发的事件
    /// </summary>
    public event EventHandler? HotkeyPressed;

    /// <summary>
    /// 初始化热键管理器，绑定窗口句柄并注册热键。
    /// </summary>
    /// <param name="windowHandle">要关联的窗口句柄，用于接收热键消息</param>
    /// <param name="config">热键配置信息，包含修饰键和按键设置</param>
    /// <returns>如果热键注册成功返回 true，否则返回 false</returns>
    public bool Initialize(IntPtr windowHandle, HotkeyConfig config)
    {
        _windowHandle = windowHandle;
        _source = HwndSource.FromHwnd(windowHandle);
        _source?.AddHook(HwndHook);
        return RegisterHotkey(config);
    }

    /// <summary>
    /// 注册或重新注册全局热键。如果之前已注册热键，会先注销再重新注册。
    /// </summary>
    /// <param name="config">热键配置信息，包含修饰键和按键设置</param>
    /// <returns>如果热键注册成功返回 true，否则返回 false</returns>
    private bool RegisterHotkey(HotkeyConfig config)
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

        return _isRegistered;
    }

    /// <summary>
    /// 将按键名称字符串解析为对应的 Windows 虚拟键码（Virtual Key Code）。
    /// 支持功能键（F1-F12）、特殊键（Space、Enter、Escape 等）、方向键、数字键和字母键。
    /// </summary>
    /// <param name="key">按键名称字符串，例如 "F1"、"SPACE"、"A" 等</param>
    /// <returns>对应的虚拟键码。如果无法识别则默认返回空格键（0x20）</returns>
    private uint ParseVirtualKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return 0x20; // 默认为空格键

        return key.ToUpper() switch
        {
            // 功能键 F1-F12
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            // 特殊键
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
            // 方向键
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            // 数字键 0-9
            "0" => 0x30,
            "1" => 0x31,
            "2" => 0x32,
            "3" => 0x33,
            "4" => 0x34,
            "5" => 0x35,
            "6" => 0x36,
            "7" => 0x37,
            "8" => 0x38,
            "9" => 0x39,
            // 字母键（单个字母字符转换为大写后获取其 ASCII 码）
            _ when key.Length == 1 && char.IsLetter(key[0]) => (uint)char.ToUpper(key[0]),
            // 无法识别的按键默认为空格键
            _ => 0x20
        };
    }

    /// <summary>
    /// 窗口消息处理钩子回调方法。
    /// 当接收到 WM_HOTKEY 消息且 ID 匹配时，触发 HotkeyPressed 事件。
    /// </summary>
    /// <param name="hwnd">窗口句柄</param>
    /// <param name="msg">消息类型</param>
    /// <param name="wParam">消息附加参数（热键 ID）</param>
    /// <param name="lParam">消息附加参数</param>
    /// <param name="handled">标记消息是否已被处理</param>
    /// <returns>始终返回 IntPtr.Zero</returns>
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

    /// <summary>
    /// 使用新的配置重新注册热键。
    /// </summary>
    /// <param name="config">新的热键配置信息</param>
    /// <returns>如果热键重新注册成功返回 true，否则返回 false</returns>
    public bool Reregister(HotkeyConfig config) => RegisterHotkey(config);

    /// <summary>
    /// 释放热键管理器占用的资源。
    /// 注销已注册的全局热键，并移除窗口消息处理钩子。
    /// </summary>
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
