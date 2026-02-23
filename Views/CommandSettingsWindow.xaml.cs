// ============================================================================
// 文件名：CommandSettingsWindow.xaml.cs
// 文件用途：命令设置窗口主文件，包含字段声明、构造函数、窗口事件、
//          菜单导航、本地化文本应用及通用辅助逻辑。
//          各功能面板的具体逻辑分别位于同名 partial 文件中。
// ============================================================================

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.Views;

/// <summary>
/// 命令设置窗口，允许用户管理自定义命令（增、删、编辑、导入、导出）、
/// 配置全局快捷键，并支持命令搜索过滤和明暗主题切换。
/// </summary>
public partial class CommandSettingsWindow : Window
{
    // ── 字段 ──────────────────────────────────────────────────────────

    /// <summary>命令配置的可观察集合，绑定到 DataGrid 控件</summary>
    public ObservableCollection<CommandConfig> Commands { get; set; } = new();

    /// <summary>命令集合的视图，支持过滤和排序</summary>
    private ICollectionView? _commandsView;

    /// <summary>命令搜索框中的当前文本</summary>
    private string _commandSearchText = "";

    /// <summary>当前加载的应用配置</summary>
    private AppConfig? _config;

    /// <summary>当前配置的快捷键字符串（如 "Alt+Space"）</summary>
    private string _currentHotkey = "";

    /// <summary>当前是否为暗色主题</summary>
    private bool _isDarkTheme;

    /// <summary>搜索引擎实例，用于执行"测试命令"功能；为 null 时禁用测试按钮</summary>
    private readonly SearchEngine? _searchEngine;

    /// <summary>当前选中的菜单项</summary>
    private string _currentMenu = "General";

    /// <summary>自动保存提示定时器</summary>
    private System.Windows.Threading.DispatcherTimer? _autoSaveTimer;

    // ── 构造函数 ──────────────────────────────────────────────────────

    /// <summary>
    /// 构造函数，初始化组件、加载配置、应用本地化文本，
    /// 并设置命令列表的过滤视图。
    /// </summary>
    /// <param name="searchEngine">搜索引擎实例（可选），用于测试命令执行</param>
    public CommandSettingsWindow(SearchEngine? searchEngine = null)
    {
        _searchEngine = searchEngine;
        Logger.Debug("Constructor started");
        InitializeComponent();
        LoadConfig();
        ApplyLocalization();
        DataContext = this;

        _commandsView = CollectionViewSource.GetDefaultView(Commands);
        _commandsView.Filter = CommandFilter;
        CommandsGrid.ItemsSource = _commandsView;

        Logger.Debug($"CommandsGrid ItemsSource set with {Commands.Count} commands");
        Logger.Debug($"CommandsGrid actual items count: {CommandsGrid.Items.Count}");
    }

    // ── 窗口生命周期 ──────────────────────────────────────────────────

    /// <summary>窗口加载完成后播放显示动画</summary>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadExchangeRateSettings();
        LoadFileSearchSettings();
        SelectMenu("MenuCommands");

        RootBorder.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        var scaleTransform = new System.Windows.Media.ScaleTransform(1, 1);
        RootBorder.RenderTransform = scaleTransform;

        RootBorder.Opacity = 0;
        var fadeIn = new System.Windows.Media.Animation.DoubleAnimation { From = 0, To = 1, Duration = System.TimeSpan.FromMilliseconds(100) };
        var scaleIn = new System.Windows.Media.Animation.DoubleAnimation { From = 0.9, To = 1, Duration = System.TimeSpan.FromMilliseconds(100) };

        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleIn);
        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleIn);
        RootBorder.BeginAnimation(System.Windows.UIElement.OpacityProperty, fadeIn);
    }

    /// <summary>窗口鼠标左键按下事件处理，允许拖动窗口。</summary>
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    /// <summary>窗口键盘按下事件处理，按 Esc 键关闭窗口。</summary>
    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
            Close();
    }

    /// <summary>取消按钮点击事件处理，直接关闭设置窗口（不保存更改）。</summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ── 菜单导航 ──────────────────────────────────────────────────────

    /// <summary>菜单项点击事件处理</summary>
    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && !string.IsNullOrEmpty(btn.Name))
            SelectMenu(btn.Name);
    }

    /// <summary>
    /// 切换菜单选中状态并显示对应面板（带动画效果）
    /// </summary>
    private void SelectMenu(string menuName)
    {
        _currentMenu = menuName;

        MenuGeneral.Tag = null;
        MenuRecording.Tag = null;
        MenuFileSearch.Tag = null;
        MenuExchangeRate.Tag = null;
        MenuCommands.Tag = null;
        MenuBuiltInCommands.Tag = null;

        FrameworkElement? currentPanel = null;
        if (PanelGeneral.Visibility == Visibility.Visible) currentPanel = PanelGeneral;
        else if (PanelRecording.Visibility == Visibility.Visible) currentPanel = PanelRecording;
        else if (PanelFileSearch.Visibility == Visibility.Visible) currentPanel = PanelFileSearch;
        else if (PanelExchangeRate.Visibility == Visibility.Visible) currentPanel = PanelExchangeRate;
        else if (PanelCommands.Visibility == Visibility.Visible) currentPanel = PanelCommands;
        else if (PanelBuiltInCommands.Visibility == Visibility.Visible) currentPanel = PanelBuiltInCommands;

        PanelGeneral.Visibility = Visibility.Collapsed;
        PanelRecording.Visibility = Visibility.Collapsed;
        PanelFileSearch.Visibility = Visibility.Collapsed;
        PanelExchangeRate.Visibility = Visibility.Collapsed;
        PanelCommands.Visibility = Visibility.Collapsed;
        PanelBuiltInCommands.Visibility = Visibility.Collapsed;

        Grid? targetPanel = null;
        switch (menuName)
        {
            case "MenuGeneral":
                MenuGeneral.Tag = "Selected";
                targetPanel = PanelGeneral;
                break;
            case "MenuRecording":
                MenuRecording.Tag = "Selected";
                targetPanel = PanelRecording;
                UpdateEstimatedSize();
                break;
            case "MenuFileSearch":
                MenuFileSearch.Tag = "Selected";
                targetPanel = PanelFileSearch;
                break;
            case "MenuExchangeRate":
                MenuExchangeRate.Tag = "Selected";
                targetPanel = PanelExchangeRate;
                break;
            case "MenuCommands":
                MenuCommands.Tag = "Selected";
                targetPanel = PanelCommands;
                break;
            case "MenuBuiltInCommands":
                MenuBuiltInCommands.Tag = "Selected";
                targetPanel = PanelBuiltInCommands;
                PopulateBuiltInCommandsGrid();
                break;
        }

        if (targetPanel != null)
        {
            if (currentPanel != null)
            {
                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, System.TimeSpan.FromMilliseconds(80));
                fadeOut.Completed += (s, e) =>
                {
                    targetPanel.Opacity = 0;
                    targetPanel.Visibility = Visibility.Visible;
                    var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, System.TimeSpan.FromMilliseconds(120));
                    targetPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };
                currentPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            else
            {
                targetPanel.Visibility = Visibility.Visible;
                targetPanel.Opacity = 1;
            }
        }
    }

    // ── 本地化 ────────────────────────────────────────────────────────

    /// <summary>
    /// 应用本地化文本到窗口中的所有 UI 元素（标题、按钮、列头、页脚等）。
    /// </summary>
    private void ApplyLocalization()
    {
        TitleText.Text = LocalizationService.Get("SettingsTitle");

        MenuGeneral.Content = LocalizationService.Get("MenuGeneral");
        MenuRecording.Content = LocalizationService.Get("MenuRecording");
        MenuFileSearch.Content = LocalizationService.Get("MenuFileSearch");
        MenuExchangeRate.Content = LocalizationService.Get("MenuExchangeRate");
        MenuCommands.Content = LocalizationService.Get("MenuCommands");
        MenuBuiltInCommands.Content = LocalizationService.Get("MenuBuiltInCommands");

        // Built-in Commands Panel
        BuiltInCommandsSectionLabel.Text = LocalizationService.Get("BuiltInCommandsSettings");
        BuiltInCommandGroupColumn.Header = LocalizationService.Get("BuiltInCommandGroup");
        BuiltInCommandColumn.Header = LocalizationService.Get("BuiltInCommand");
        BuiltInFunctionColumn.Header = LocalizationService.Get("BuiltInCommandFunction");
        BuiltInUsageColumn.Header = LocalizationService.Get("BuiltInCommandUsage");

        GeneralSectionLabel.Text = LocalizationService.Get("GeneralSettings");
        CommandsSectionLabel.Text = LocalizationService.Get("CommandManagement");
        RecordingSectionLabel.Text = LocalizationService.Get("RecordingSettings");
        ExchangeRateSectionLabel.Text = LocalizationService.Get("MenuExchangeRate");
        ExchangeRateApiHint.Text = LocalizationService.Get("ExchangeRateApiHint");
        ExchangeRateCacheLabel.Text = LocalizationService.Get("ExchangeRateCacheLabel");
        ExchangeRateCacheUnit.Text = LocalizationService.Get("ExchangeRateCacheUnit");
        ExchangeRateCacheHint.Text = LocalizationService.Get("ExchangeRateCacheHint");

        HotkeyLabel.Text = LocalizationService.Get("Hotkey");
        ImportButton.Content = LocalizationService.Get("ImportCommand");
        ExportButton.Content = LocalizationService.Get("ExportCommand");
        StartWithWindowsCheck.Content = LocalizationService.Get("StartWithWindows");
        DarkThemeCheck.Content = LocalizationService.Get("DarkTheme");
        MaxResultsLabel.Text = LocalizationService.Get("MaxResults");
        MaxResultsSuffix.Text = LocalizationService.Get("MaxResultsSuffix");
        QRCodeThresholdLabel.Text = LocalizationService.Get("QRCodeThreshold");
        QRCodeThresholdSuffix.Text = LocalizationService.Get("QRCodeThresholdSuffix");
        LanguageLabel.Text = LocalizationService.Get("LanguageLabel");

        RecordSourceLabel.Text = LocalizationService.Get("RecordSource") + "：";
        RecordFormatLabel.Text = LocalizationService.Get("RecordFormat") + "：";
        RecordBitrateLabel.Text = LocalizationService.Get("RecordBitrate") + "：";
        RecordChannelsLabel.Text = LocalizationService.Get("RecordChannels") + "：";
        RecordOutputPathLabel.Text = LocalizationService.Get("RecordOutputPath") + "：";
        RecordBrowseButton.Content = LocalizationService.Get("RecordBrowse");
        RecordEstimatedSizeLabel.Text = LocalizationService.Get("RecordEstimatedSize") + "：";

        // File Search
        FileSearchSectionLabel.Text = LocalizationService.Get("FileSearchSettings");
        FileSearchEnabledCheck.Content = LocalizationService.Get("FileSearchEnabled");
        FileSearchDirectoriesLabel.Text = LocalizationService.Get("FileSearchDirectories");
        FileSearchDirectoriesHint.Text = LocalizationService.Get("FileSearchDirectoriesHint");
        FileSearchMaxFilesLabel.Text = LocalizationService.Get("FileSearchMaxFiles");
        FileSearchMaxFilesSuffix.Text = LocalizationService.Get("FileSearchMaxFilesSuffix");
        FileSearchMaxResultsLabel.Text = LocalizationService.Get("FileSearchMaxResults");
        FileSearchMaxResultsSuffix.Text = LocalizationService.Get("FileSearchMaxResultsSuffix");
        FileSearchRecursiveCheck.Content = LocalizationService.Get("FileSearchRecursive");

        PopulateLanguageComboBox();
        PopulateRecordSourceCombo();
        PopulateRecordFormatCombo();
        PopulateRecordBitrateCombo();
        PopulateRecordChannelsCombo();

        AddButton.Content = LocalizationService.Get("Add");
        DeleteButton.Content = LocalizationService.Get("Delete");
        TestButton.Content = LocalizationService.Get("TestCommand");
        SaveButton.Content = LocalizationService.Get("Save");
        KeywordColumn.Header = LocalizationService.Get("Keyword");
        NameColumn.Header = LocalizationService.Get("Name");
        TypeColumn.Header = LocalizationService.Get("Type");
        PathColumn.Header = LocalizationService.Get("Path");
        AdminColumn.Header = LocalizationService.Get("Admin");
        FooterText.Text = "";
        CommandHintText.Text = LocalizationService.Get("Footer");
        CommandSearchPlaceholder.Text = LocalizationService.Get("SearchCommands");
    }

    // ── 命令搜索 ──────────────────────────────────────────────────────

    /// <summary>
    /// 命令搜索框文本变更事件处理。更新搜索文本、切换占位符和清除按钮的可见性，并刷新过滤视图。
    /// </summary>
    private void CommandSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _commandSearchText = CommandSearchBox.Text;
        CommandSearchPlaceholder.Visibility = string.IsNullOrEmpty(_commandSearchText)
            ? Visibility.Visible : Visibility.Collapsed;
        SearchClearBtn.Visibility = string.IsNullOrEmpty(_commandSearchText)
            ? Visibility.Collapsed : Visibility.Visible;
        _commandsView?.Refresh();
    }

    /// <summary>搜索清除按钮点击事件处理，清空搜索框并重新聚焦。</summary>
    private void SearchClearBtn_Click(object sender, MouseButtonEventArgs e)
    {
        CommandSearchBox.Text = "";
        CommandSearchBox.Focus();
    }

    // ── Toast 提示 ────────────────────────────────────────────────────

    /// <summary>显示自动保存成功提示（2秒后自动消失）</summary>
    private void ShowAutoSaveToast()
    {
        _autoSaveTimer?.Stop();

        var savedText = LocalizationService.Get("Saved");
        AutoSaveIndicator.Text = " · " + savedText;
        AutoSaveIndicator.Visibility = Visibility.Visible;

        _autoSaveTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _autoSaveTimer.Tick += (s, e) =>
        {
            AutoSaveIndicator.Visibility = Visibility.Collapsed;
            _autoSaveTimer.Stop();
        };
        _autoSaveTimer.Start();
    }

    // ── 内置命令一览 ─────────────────────────────────────────────────

    /// <summary>填充内置命令表格数据（每次调用时重新填充以支持语言切换）</summary>
    private void PopulateBuiltInCommandsGrid()
    {
        var commands = new List<BuiltInCommandItem>();

        // Command Line Tools
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupCommand"), "cmd", LocalizationService.Get("BuiltinCmd_cmd"), LocalizationService.Get("BuiltinDesc_cmd")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupCommand"), "powershell", LocalizationService.Get("BuiltinCmd_powershell"), LocalizationService.Get("BuiltinDesc_powershell")));

        // Applications
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupApp"), "notepad", LocalizationService.Get("BuiltinCmd_notepad"), LocalizationService.Get("BuiltinDesc_notepad")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupApp"), "calc", LocalizationService.Get("BuiltinCmd_calc"), LocalizationService.Get("BuiltinDesc_calc")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupApp"), "mspaint", LocalizationService.Get("BuiltinCmd_mspaint"), LocalizationService.Get("BuiltinDesc_mspaint")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupApp"), "explorer", LocalizationService.Get("BuiltinCmd_explorer"), LocalizationService.Get("BuiltinDesc_explorer")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupApp"), "winrecord", LocalizationService.Get("BuiltinCmd_winrecord"), LocalizationService.Get("BuiltinDesc_winrecord")));

        // System Management
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupSystem"), "taskmgr", LocalizationService.Get("BuiltinCmd_taskmgr"), LocalizationService.Get("BuiltinDesc_taskmgr")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupSystem"), "devmgmt", LocalizationService.Get("BuiltinCmd_devmgmt"), LocalizationService.Get("BuiltinDesc_devmgmt")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupSystem"), "services", LocalizationService.Get("BuiltinCmd_services"), LocalizationService.Get("BuiltinDesc_services")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupSystem"), "regedit", LocalizationService.Get("BuiltinCmd_regedit"), LocalizationService.Get("BuiltinDesc_regedit")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupSystem"), "control", LocalizationService.Get("BuiltinCmd_control"), LocalizationService.Get("BuiltinDesc_control")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupSystem"), "emptybin", LocalizationService.Get("BuiltinCmd_emptybin"), LocalizationService.Get("BuiltinDesc_emptybin")));

        // Network Diagnostics
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupNetwork"), "ipconfig", LocalizationService.Get("BuiltinCmd_ipconfig"), LocalizationService.Get("BuiltinDesc_ipconfig")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupNetwork"), "ping", LocalizationService.Get("BuiltinCmd_ping"), LocalizationService.Get("BuiltinDesc_ping")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupNetwork"), "tracert", LocalizationService.Get("BuiltinCmd_tracert"), LocalizationService.Get("BuiltinDesc_tracert")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupNetwork"), "nslookup", LocalizationService.Get("BuiltinCmd_nslookup"), LocalizationService.Get("BuiltinDesc_nslookup")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupNetwork"), "netstat", LocalizationService.Get("BuiltinCmd_netstat"), LocalizationService.Get("BuiltinDesc_netstat")));

        // Power Management
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupPower"), "lock", LocalizationService.Get("BuiltinCmd_lock"), LocalizationService.Get("BuiltinDesc_lock")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupPower"), "shutdown", LocalizationService.Get("BuiltinCmd_shutdown"), LocalizationService.Get("BuiltinDesc_shutdown")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupPower"), "restart", LocalizationService.Get("BuiltinCmd_restart"), LocalizationService.Get("BuiltinDesc_restart")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupPower"), "sleep", LocalizationService.Get("BuiltinCmd_sleep"), LocalizationService.Get("BuiltinDesc_sleep")));

        // Feature Commands
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupFeature"), "record", LocalizationService.Get("BuiltinCmd_record"), LocalizationService.Get("BuiltinDesc_record")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupFeature"), "clip", LocalizationService.Get("BuiltinCmd_clip"), LocalizationService.Get("BuiltinDesc_clip")));

        // Quanta System Commands
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupQuanta"), "setting", LocalizationService.Get("BuiltinCmd_setting"), LocalizationService.Get("BuiltinDesc_setting")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupQuanta"), "about", LocalizationService.Get("BuiltinCmd_about"), LocalizationService.Get("BuiltinDesc_about")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupQuanta"), "exit", LocalizationService.Get("BuiltinCmd_exit"), LocalizationService.Get("BuiltinDesc_exit")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupQuanta"), "english", LocalizationService.Get("BuiltinCmd_english"), LocalizationService.Get("BuiltinDesc_english")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupQuanta"), "chinese", LocalizationService.Get("BuiltinCmd_chinese"), LocalizationService.Get("BuiltinDesc_chinese")));
        commands.Add(new BuiltInCommandItem(LocalizationService.Get("GroupQuanta"), "spanish", LocalizationService.Get("BuiltinCmd_spanish"), LocalizationService.Get("BuiltinDesc_spanish")));

        BuiltInCommandsGrid.ItemsSource = commands;
    }
}

/// <summary>内置命令项数据模型</summary>
public class BuiltInCommandItem
{
    public string Group { get; set; }
    public string Command { get; set; }
    public string Function { get; set; }
    public string Usage { get; set; }

    public BuiltInCommandItem(string group, string command, string function, string usage)
    {
        Group = group;
        Command = command;
        Function = function;
        Usage = usage;
    }
}