// ============================================================================
// 文件名：CommandSettingsWindow.Hotkey.cs
// 文件用途：全局快捷键配置面板的交互逻辑（录入、格式化、保存快捷键）。
// ============================================================================

using System.Windows;
using System.Windows.Input;
using Quanta.Helpers;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.Views;

public partial class CommandSettingsWindow
{
    /// <summary>
    /// 将修饰键和主键格式化为可读的快捷键字符串（如 "Alt+Space"）。
    /// </summary>
    private string FormatHotkey(string modifier, string key)
    {
        if (string.IsNullOrEmpty(modifier) && string.IsNullOrEmpty(key))
            return "";
        return $"{modifier}+{key}".TrimStart('+');
    }

    /// <summary>
    /// 快捷键输入框获取焦点事件处理，显示提示文本并全选。
    /// </summary>
    private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        HotkeyTextBox.Text = LocalizationService.Get("HotkeyPress");
        HotkeyTextBox.SelectAll();
    }

    /// <summary>
    /// 快捷键输入框双击事件处理，清除快捷键。
    /// </summary>
    private void HotkeyTextBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _currentHotkey = "";
        HotkeyTextBox.Text = "";
        var config = ConfigLoader.Load();
        config.Hotkey = new HotkeyConfig { Modifier = "", Key = "" };
        ConfigLoader.Save(config);
        ShowAutoSaveToast();
    }

    /// <summary>
    /// 快捷键输入框键盘按下预处理事件。
    /// 捕获修饰键 + 主键的组合，格式化后显示并自动保存。
    /// 忽略单独的修饰键按下。
    /// </summary>
    private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        var modifiers = Keyboard.Modifiers;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (modifiers == ModifierKeys.None)
        {
            HotkeyTextBox.Text = LocalizationService.Get("HotkeyPress");
            return;
        }

        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        string modifierStr = "";
        if (modifiers.HasFlag(ModifierKeys.Control)) modifierStr += "Ctrl+";
        if (modifiers.HasFlag(ModifierKeys.Alt)) modifierStr += "Alt+";
        if (modifiers.HasFlag(ModifierKeys.Shift)) modifierStr += "Shift+";
        if (modifiers.HasFlag(ModifierKeys.Windows)) modifierStr += "Win+";

        string keyStr = key.ToString();
        _currentHotkey = modifierStr + keyStr;
        HotkeyTextBox.Text = _currentHotkey;

        SaveHotkey();
    }

    /// <summary>
    /// 保存当前配置的快捷键到配置文件。
    /// 解析快捷键字符串，分离修饰键和主键后写入配置。
    /// </summary>
    private void SaveHotkey()
    {
        if (string.IsNullOrEmpty(_currentHotkey) || _currentHotkey == LocalizationService.Get("HotkeyPress"))
            return;

        var config = ConfigLoader.Load();
        var parts = _currentHotkey.Split('+');
        string modifier = "";
        string key = "";

        foreach (var part in parts)
        {
            if (part == "Ctrl" || part == "Alt" || part == "Shift" || part == "Win")
                modifier = part;
            else
                key = part;
        }

        config.Hotkey = new HotkeyConfig { Modifier = modifier, Key = key };
        ConfigLoader.Save(config);
    }
}
