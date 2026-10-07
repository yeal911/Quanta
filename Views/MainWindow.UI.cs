// ============================================================================
// 文件名：MainWindow.UI.cs
// 文件用途：主题切换、本地化刷新、搜索图标菜单、颜色复制事件处理。
// ============================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Quanta.Models;
using Quanta.Services;
using Quanta.Core.Interfaces;
using WpfButton = System.Windows.Controls.Button;
using WpfToolTip = System.Windows.Controls.ToolTip;

namespace Quanta.Views;

public partial class MainWindow
{
    // ── IMainWindowService 接口实现 ───────────────────────────────────

    /// <summary>是否处于暗色主题（实现 IMainWindowService）</summary>
    public bool IsDarkTheme => _viewModel.IsDarkTheme;

    /// <summary>切换主题（实现 IMainWindowService，委托给 ViewModel）</summary>
    public void ToggleTheme()
    {
        _viewModel.ToggleThemeCommand.Execute(null);
    }

    // ── 主题 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 主题切换按钮点击事件处理。
    /// </summary>
    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleThemeCommand.Execute(null);
    }

    /// <summary>
    /// 应用主题：通过 ThemeService 切换 MergedDictionaries，所有使用 DynamicResource 的控件自动刷新。
    /// </summary>
    public void ApplyTheme(bool isDark)
    {
        ThemeService.ApplyTheme(isDark ? "Dark" : "Light");
        UpdateThemeIcon(isDark);
    }

    /// <summary>更新主题切换按钮的图标文字（☀ / 🌙）</summary>
    public void UpdateThemeIcon(bool isDark)
    {
        if (FindName("ThemeIcon") is TextBlock icon)
            icon.Text = isDark ? "☀" : "🌙";
    }

    /// <summary>
    /// 主题切换按钮右键点击，显示上下文菜单（与托盘菜单相同）。
    /// </summary>
    private void ThemeToggleButton_RightClick(object sender, MouseButtonEventArgs e)
    {
        var menu = new ContextMenu();

        var settingsItem = new MenuItem { Header = LocalizationService.Get("TraySettings") };
        settingsItem.Click += (s, args) => OpenCommandSettings();
        menu.Items.Add(settingsItem);

        var langItem = new MenuItem { Header = LocalizationService.Get("TrayLanguage") };
        foreach (var lang in LocalizationService.GetSupportedLanguages())
        {
            var langMenuItem = new MenuItem
            {
                Header = LocalizationService.Get(LocalizationService.GetLanguageDisplayKey(lang.Code)),
                IsChecked = LocalizationService.CurrentLanguage == lang.Code
            };
            langMenuItem.Click += (s, args) =>
            {
                LocalizationService.CurrentLanguage = lang.Code;
                RefreshLocalization();
                _trayService?.Initialize();
            };
            langItem.Items.Add(langMenuItem);
        }
        menu.Items.Add(langItem);

        menu.Items.Add(new Separator());

        var aboutItem = new MenuItem { Header = LocalizationService.Get("TrayAbout") };
        aboutItem.Click += (s, args) => ToastService.Instance.ShowInfo(
            $"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
        menu.Items.Add(aboutItem);

        var exitItem = new MenuItem { Header = LocalizationService.Get("TrayExit") };
        exitItem.Click += (s, args) =>
        {
            if (_recordingService != null && _recordingService.State != RecordingState.Idle)
            {
                ToastService.Instance.ShowWarning(LocalizationService.Get("RecordAlreadyRecording"));
                return;
            }
            _trayService?.Dispose();
            System.Windows.Application.Current.Shutdown();
        };
        menu.Items.Add(exitItem);

        menu.IsOpen = true;
        e.Handled = true;
    }

    // ── 本地化 ───────────────────────────────────────────────────────

    /// <summary>
    /// 刷新界面的本地化文本（如搜索框占位符、菜单项文字等）。
    /// 在语言切换后调用。
    /// </summary>
    public void RefreshLocalization()
    {
        UpdatePlaceholderWithHotkey();
        BuildSearchIconMenu();
        UpdateTooltips();
    }

    /// <summary>
    /// 更新所有界面元素的 ToolTip 文本。
    /// </summary>
    private void UpdateTooltips()
    {
        if (FindName("ThemeToggleButton") is WpfButton themeBtn)
            themeBtn.ToolTip = LocalizationService.Get("ThemeSwitch");

        try
        {
            if (FindName("CopyHexTooltip") is WpfToolTip hexTip)
                hexTip.Content = LocalizationService.Get("RightClickCopy");
            if (FindName("CopyRgbTooltip") is WpfToolTip rgbTip)
                rgbTip.Content = LocalizationService.Get("RightClickCopy");
            if (FindName("CopyHslTooltip") is WpfToolTip hslTip)
                hslTip.Content = LocalizationService.Get("RightClickCopy");
        }
        catch
        {
            // ToolTips 可能尚未初始化
        }
    }

    /// <summary>
    /// 更新搜索框占位符，包含当前快捷键信息。
    /// </summary>
    private void UpdatePlaceholderWithHotkey()
    {
        var config = _configLoader.Load();
        var hotkey = config.Hotkey;
        var hotkeyStr = $"{hotkey.Modifier}+{hotkey.Key}";
        PlaceholderText.Text = LocalizationService.Get("SearchPlaceholder") + " | " + hotkeyStr;
    }

    // ── 搜索图标菜单 ─────────────────────────────────────────────────

    /// <summary>
    /// 构建搜索图标的右键上下文菜单，包含设置、语言切换、关于、退出等菜单项。
    /// </summary>
    private void BuildSearchIconMenu()
    {
        SearchIconMenu.Items.Clear();

        var settingsItem = new MenuItem { Header = LocalizationService.Get("TraySettings") };
        settingsItem.Click += (s, e) => OpenCommandSettings();
        SearchIconMenu.Items.Add(settingsItem);

        var langItem = new MenuItem { Header = LocalizationService.Get("TrayLanguage") };
        foreach (var lang in LocalizationService.GetSupportedLanguages())
        {
            var langMenuItem = new MenuItem
            {
                Header = LocalizationService.Get(LocalizationService.GetLanguageDisplayKey(lang.Code)),
                IsChecked = LocalizationService.CurrentLanguage == lang.Code
            };
            langMenuItem.Click += (s, e) =>
            {
                LocalizationService.CurrentLanguage = lang.Code;
                RefreshLocalization();
                _trayService?.Initialize();
            };
            langItem.Items.Add(langMenuItem);
        }
        SearchIconMenu.Items.Add(langItem);

        SearchIconMenu.Items.Add(new Separator());

        var aboutItem = new MenuItem { Header = LocalizationService.Get("TrayAbout") };
        aboutItem.Click += (s, e) => ToastService.Instance.ShowInfo(
            $"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
        SearchIconMenu.Items.Add(aboutItem);

        var exitItem = new MenuItem { Header = LocalizationService.Get("TrayExit") };
        exitItem.Click += (s, e) =>
        {
            if (_recordingService != null && _recordingService.State != RecordingState.Idle)
            {
                ToastService.Instance.ShowWarning(LocalizationService.Get("RecordAlreadyRecording"));
                return;
            }
            _trayService?.Dispose();
            System.Windows.Application.Current.Shutdown();
        };
        SearchIconMenu.Items.Add(exitItem);
    }

    /// <summary>
    /// 搜索图标右键点击事件处理，打开上下文菜单。
    /// </summary>
    private void SearchIcon_RightClick(object sender, MouseButtonEventArgs e)
    {
        BuildSearchIconMenu();
        SearchIconMenu.IsOpen = true;
        e.Handled = true;
    }

    // ── 颜色复制事件处理 ──────────────────────────────────────────────

    private void CopyColorHex_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
        {
            var textBlock = contextMenu.PlacementTarget as TextBlock;
            if (textBlock?.DataContext is SearchResult result && result.ColorInfo != null)
            {
                System.Windows.Clipboard.SetText(result.ColorInfo.Hex);
                ToastService.Instance.ShowInfo(LocalizationService.Get("ColorCopied", result.ColorInfo.Hex));
            }
        }
    }

    private void CopyColorRgb_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
        {
            var textBlock = contextMenu.PlacementTarget as TextBlock;
            if (textBlock?.DataContext is SearchResult result && result.ColorInfo != null)
            {
                System.Windows.Clipboard.SetText(result.ColorInfo.Rgb);
                ToastService.Instance.ShowInfo(LocalizationService.Get("ColorCopied", result.ColorInfo.Rgb));
            }
        }
    }

    private void CopyColorHsl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
        {
            var textBlock = contextMenu.PlacementTarget as TextBlock;
            if (textBlock?.DataContext is SearchResult result && result.ColorInfo != null)
            {
                System.Windows.Clipboard.SetText(result.ColorInfo.Hsl);
                ToastService.Instance.ShowInfo(LocalizationService.Get("ColorCopied", result.ColorInfo.Hsl));
            }
        }
    }

    private void CopyColorHex_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.DataContext is SearchResult result && result.ColorInfo != null)
        {
            System.Windows.Clipboard.SetText(result.ColorInfo.Hex);
            ToastService.Instance.ShowInfo(LocalizationService.Get("ColorCopied", result.ColorInfo.Hex));
        }
    }

    private void CopyColorRgb_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.DataContext is SearchResult result && result.ColorInfo != null)
        {
            System.Windows.Clipboard.SetText(result.ColorInfo.Rgb);
            ToastService.Instance.ShowInfo(LocalizationService.Get("ColorCopied", result.ColorInfo.Rgb));
        }
    }

    private void CopyColorHsl_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.DataContext is SearchResult result && result.ColorInfo != null)
        {
            System.Windows.Clipboard.SetText(result.ColorInfo.Hsl);
            ToastService.Instance.ShowInfo(LocalizationService.Get("ColorCopied", result.ColorInfo.Hsl));
        }
    }
}
