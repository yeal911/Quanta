// ============================================================================
// 文件名：MainWindow.UI.cs
// 文件用途：主题图标更新、应用菜单构建（搜索图标/主题按钮右键共用）、
//          本地化 ToolTip 刷新、颜色复制事件 → ViewModel 接线。
//          菜单动作（语言切换/关于/退出）与颜色复制业务在 MainViewModel。
// ============================================================================

using System.Windows.Controls;
using System.Windows.Input;
using WpfButton = System.Windows.Controls.Button;
using WpfToolTip = System.Windows.Controls.ToolTip;
using Quanta.Domain.Search;
using Quanta.Presentation.Helpers;

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

    /// <summary>更新主题切换按钮的图标文字（☀ / 🌙）</summary>
    public void UpdateThemeIcon(bool isDark)
    {
        if (FindName("ThemeIcon") is TextBlock icon)
            icon.Text = isDark ? "☀" : "🌙";
    }

    /// <summary>
    /// 应用主题：通过 ThemeService 切换 MergedDictionaries，所有使用 DynamicResource 的控件自动刷新。
    /// （供 SearchEngine 的语言切换系统动作在 Dispatcher 上调用）
    /// </summary>
    public void ApplyTheme(bool isDark)
    {
        ThemeService.ApplyTheme(isDark ? "Dark" : "Light");
        UpdateThemeIcon(isDark);
    }

    /// <summary>
    /// 主题切换按钮右键点击，显示上下文菜单（与搜索图标菜单相同）。
    /// </summary>
    private void ThemeToggleButton_RightClick(object sender, MouseButtonEventArgs e)
    {
        var menu = new ContextMenu();
        BuildAppMenu(menu);
        menu.IsOpen = true;
        e.Handled = true;
    }

    // ── 本地化 ───────────────────────────────────────────────────────

    /// <summary>
    /// 语言切换事件处理：刷新本地化文本并更新布局方向（ar-SA 为 RTL）。
    /// 事件可能从非 UI 线程触发，统一调度回 UI 线程执行。
    /// </summary>
    private void OnLanguageChanged(object? sender, string langCode)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => OnLanguageChanged(sender, langCode));
            return;
        }

        RefreshLocalization();
        ApplyFlowDirection();
    }

    /// <summary>
    /// 按当前语言应用窗口布局方向：ar-SA（阿拉伯语）从右到左，其余语言从左到右。
    /// 在启动与每次语言切换后调用。
    /// </summary>
    private void ApplyFlowDirection()
    {
        FlowDirection = LocalizationService.IsCurrentLanguageRightToLeft
            ? System.Windows.FlowDirection.RightToLeft
            : System.Windows.FlowDirection.LeftToRight;
    }

    /// <summary>
    /// 刷新界面的本地化文本（如搜索框占位符、菜单项文字等）。
    /// 在语言切换后调用。
    /// </summary>
    public void RefreshLocalization()
    {
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

    // ── 应用菜单（搜索图标右键 / 主题按钮右键共用） ──────────────────

    /// <summary>
    /// 重建搜索图标的右键上下文菜单。
    /// </summary>
    private void BuildSearchIconMenu()
    {
        BuildAppMenu(SearchIconMenu);
    }

    /// <summary>
    /// 构建应用菜单：设置、语言切换、关于、退出。
    /// 菜单项动作全部委托给 MainViewModel 命令。
    /// </summary>
    private void BuildAppMenu(ContextMenu menu)
    {
        menu.Items.Clear();

        var settingsItem = new MenuItem { Header = LocalizationService.Get("TraySettings") };
        settingsItem.Click += (s, e) => OpenCommandSettings();
        menu.Items.Add(settingsItem);

        var langItem = new MenuItem { Header = LocalizationService.Get("TrayLanguage") };
        foreach (var lang in LocalizationService.GetSupportedLanguages())
        {
            var langMenuItem = new MenuItem
            {
                Header = LocalizationService.Get(LocalizationService.GetLanguageDisplayKey(lang.Code)),
                IsChecked = LocalizationService.CurrentLanguage == lang.Code
            };
            langMenuItem.Click += (s, e) => _viewModel.SwitchLanguageCommand.Execute(lang.Code);
            langItem.Items.Add(langMenuItem);
        }
        menu.Items.Add(langItem);

        menu.Items.Add(new Separator());

        var aboutItem = new MenuItem { Header = LocalizationService.Get("TrayAbout") };
        aboutItem.Click += (s, e) => _viewModel.ShowAboutCommand.Execute(null);
        menu.Items.Add(aboutItem);

        var exitItem = new MenuItem { Header = LocalizationService.Get("TrayExit") };
        exitItem.Click += (s, e) => _viewModel.ExitApplicationCommand.Execute(null);
        menu.Items.Add(exitItem);
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

    // ── 颜色复制事件处理（业务在 MainViewModel.CopyColor） ────────────

    private void CopyColorHex_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.DataContext is SearchResult result)
            _viewModel.CopyColor(result, "Hex");
    }

    private void CopyColorRgb_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.DataContext is SearchResult result)
            _viewModel.CopyColor(result, "Rgb");
    }

    private void CopyColorHsl_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.DataContext is SearchResult result)
            _viewModel.CopyColor(result, "Hsl");
    }
}
