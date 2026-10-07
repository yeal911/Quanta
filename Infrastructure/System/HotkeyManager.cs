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

        // 快捷键已被清空（双击清除）：无需注册，直接视为成功
        if (string.IsNullOrEmpty(config.Modifier) && string.IsNullOrEmpty(config.Key))
        {
            Logger.Debug("[Hotkey] No hotkey configured, skipping registration");
            return true;
        }

        // 修饰键支持多键组合（如 "Ctrl+Shift"），按位组合 MOD_* 标志；
        // 无法识别的组合不再静默回退，返回 false 由调用方给出提示
        if (!TryParseModifiers(config.Modifier, out uint modifiers))
        {
            Logger.Debug($"[Hotkey] Unrecognized modifier: {config.Modifier}");
            return false;
        }

        if (!TryParseVirtualKey(config.Key, out uint vk))
        {
            Logger.Debug($"[Hotkey] Unrecognized key: {config.Key}");
            return false;
        }

        Logger.Debug($"[Hotkey] Registering: Modifier={config.Modifier}({modifiers}), Key={config.Key}({vk})");

        _isRegistered = RegisterHotKey(_windowHandle, HOTKEY_ID, modifiers, vk);

        if (!_isRegistered)
        {
            Logger.Debug($"[Hotkey] Failed to register hotkey!");
        }

        return _isRegistered;
    }

    /// <summary>
    /// 判断按键名称是否为可注册的虚拟键码。
    /// 供设置界面在录制阶段校验，避免保存无法注册的组合。
    /// </summary>
    /// <param name="key">按键名称字符串，例如 "F1"、"P" 等</param>
    /// <returns>能解析为虚拟键码返回 true，否则 false</returns>
    public static bool IsSupportedKey(string? key) => TryParseVirtualKey(key, out _);

    /// <summary>
    /// 解析修饰键字符串为 MOD_* 标志位的按位组合。
    /// 支持以 "+" 分隔的多修饰键组合（如 "Ctrl+Shift"）。
    /// </summary>
    /// <param name="modifier">修饰键字符串，如 "Ctrl"、"Ctrl+Shift"</param>
    /// <param name="modifiers">解析出的 MOD_* 标志位组合</param>
    /// <returns>全部令牌可识别返回 true；为空或含未知令牌返回 false</returns>
    private static bool TryParseModifiers(string? modifier, out uint modifiers)
    {
        modifiers = 0;
        if (string.IsNullOrWhiteSpace(modifier))
            return false;

        foreach (var part in modifier.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bit = part.ToUpperInvariant() switch
            {
                "ALT" => MOD_ALT,
                "CTRL" => MOD_CONTROL,
                "SHIFT" => MOD_SHIFT,
                "WIN" => MOD_WIN,
                _ => 0u
            };
            if (bit == 0)
                return false;
            modifiers |= bit;
        }
        return true;
    }

    /// <summary>
    /// 将按键名称字符串解析为对应的 Windows 虚拟键码（Virtual Key Code）。
    /// 支持功能键（F1-F12）、特殊键（Space、Enter、Escape 等）、方向键、数字键和字母键。
    /// </summary>
    /// <param name="key">按键名称字符串，例如 "F1"、"SPACE"、"A" 等</param>
    /// <param name="vk">解析出的虚拟键码</param>
    /// <returns>可识别返回 true 并输出虚拟键码；无法识别（含空值）返回 false，不再静默回退为空格键</returns>
    private static bool TryParseVirtualKey(string? key, out uint vk)
    {
        vk = 0;
        if (string.IsNullOrEmpty(key)) return false;

        switch (key.ToUpper())
        {
            // 功能键 F1-F12
            case "F1": vk = 0x70; return true;
            case "F2": vk = 0x71; return true;
            case "F3": vk = 0x72; return true;
            case "F4": vk = 0x73; return true;
            case "F5": vk = 0x74; return true;
            case "F6": vk = 0x75; return true;
            case "F7": vk = 0x76; return true;
            case "F8": vk = 0x77; return true;
            case "F9": vk = 0x78; return true;
            case "F10": vk = 0x79; return true;
            case "F11": vk = 0x7A; return true;
            case "F12": vk = 0x7B; return true;
            // 特殊键
            case "SPACE": vk = 0x20; return true;
            case "ENTER": vk = 0x0D; return true;
            case "ESCAPE": vk = 0x1B; return true;
            case "TAB": vk = 0x09; return true;
            case "BACKSPACE": vk = 0x08; return true;
            case "DELETE": vk = 0x2E; return true;
            case "INSERT": vk = 0x2D; return true;
            case "HOME": vk = 0x24; return true;
            case "END": vk = 0x23; return true;
            case "PAGEUP": vk = 0x21; return true;
            case "PAGEDOWN": vk = 0x22; return true;
            // 方向键
            case "UP": vk = 0x26; return true;
            case "DOWN": vk = 0x28; return true;
            case "LEFT": vk = 0x25; return true;
            case "RIGHT": vk = 0x27; return true;
            // 数字键 0-9
            case "0": vk = 0x30; return true;
            case "1": vk = 0x31; return true;
            case "2": vk = 0x32; return true;
            case "3": vk = 0x33; return true;
            case "4": vk = 0x34; return true;
            case "5": vk = 0x35; return true;
            case "6": vk = 0x36; return true;
            case "7": vk = 0x37; return true;
            case "8": vk = 0x38; return true;
            case "9": vk = 0x39; return true;
            default:
                // 字母键（单个字母字符转换为大写后获取其 ASCII 码）
                if (key.Length == 1 && char.IsLetter(key[0]))
                {
                    vk = (uint)char.ToUpper(key[0]);
                    return true;
                }
                return false;
        }
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
