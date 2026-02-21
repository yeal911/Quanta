// ============================================================================
// 文件名：CommandSettingsViewModel.cs
// 文件用途：命令设置窗口的视图模型，负责管理命令配置、快捷键、主题、语言等设置。
//          使用 CommunityToolkit.Mvvm 框架实现数据绑定和命令模式。
// ============================================================================

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quanta.Helpers;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.ViewModels;

/// <summary>
/// 命令设置窗口视图模型，管理命令配置、快捷键、主题、语言等设置。
/// 继承自 ObservableObject，支持属性变更通知。
/// </summary>
public partial class CommandSettingsViewModel : ObservableObject
{
    /// <summary>搜索引擎实例，用于执行"测试命令"功能</summary>
    private readonly SearchEngine? _searchEngine;

    /// <summary>命令配置的可观察集合，绑定到 DataGrid 控件</summary>
    [ObservableProperty]
    private ObservableCollection<CommandConfig> _commands = new();

    /// <summary>命令集合的视图，支持过滤和排序</summary>
    public ICollectionView? CommandsView { get; private set; }

    /// <summary>命令搜索框中的当前文本</summary>
    [ObservableProperty]
    private string _commandSearchText = string.Empty;

    /// <summary>当前加载的应用配置</summary>
    [ObservableProperty]
    private AppConfig? _config;

    /// <summary>当前是否为暗色主题</summary>
    [ObservableProperty]
    private bool _isDarkTheme;

    /// <summary>当前菜单项</summary>
    [ObservableProperty]
    private string _currentMenu = "General";

    /// <summary>快捷键文本</summary>
    [ObservableProperty]
    private string _hotkeyText = string.Empty;

    /// <summary>是否开机启动</summary>
    [ObservableProperty]
    private bool _startWithWindows;

    /// <summary>最多显示结果数</summary>
    [ObservableProperty]
    private int _maxResults = 8;

    /// <summary>二维码阈值</summary>
    [ObservableProperty]
    private int _qrCodeThresholdValue = 20;

    /// <summary>当前语言</summary>
    [ObservableProperty]
    private string _currentLanguage = "zh-CN";

    /// <summary>支持的语言列表</summary>
    [ObservableProperty]
    private ObservableCollection<LanguageInfo> _supportedLanguages = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="searchEngine">搜索引擎实例（可选），用于测试命令执行</param>
    public CommandSettingsViewModel(SearchEngine? searchEngine = null)
    {
        _searchEngine = searchEngine;
        LoadConfig();
        LoadSupportedLanguages();
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfig()
    {
        Config = ConfigLoader.Load();

        if (Config?.Commands != null)
        {
            Commands = new ObservableCollection<CommandConfig>(
                Config.Commands.OrderByDescending(c => c.ModifiedAt));
        }
        else
        {
            Commands = new ObservableCollection<CommandConfig>();
        }

        // 设置命令视图
        CommandsView = CollectionViewSource.GetDefaultView(Commands);
        CommandsView.Filter = CommandFilter;

        // 加载应用设置
        if (Config != null)
        {
            IsDarkTheme = Config.Theme == "Dark";
            MaxResults = Config.AppSettings?.MaxResults ?? 10;
            QrCodeThresholdValue = Config.AppSettings?.QRCodeThreshold ?? 20;
            CurrentLanguage = Config.AppSettings?.Language ?? "zh-CN";
        }

        // 加载快捷键
        if (Config?.Hotkey != null)
        {
            HotkeyText = string.IsNullOrEmpty(Config.Hotkey.Modifier)
                ? LocalizationService.Get("HotkeyPress")
                : $"{Config.Hotkey.Modifier}+{Config.Hotkey.Key}";
        }
        else
        {
            HotkeyText = LocalizationService.Get("HotkeyPress");
        }
    }

    /// <summary>
    /// 加载支持的语言列表
    /// </summary>
    private void LoadSupportedLanguages()
    {
        var languages = LocalizationService.GetSupportedLanguages();
        SupportedLanguages = new ObservableCollection<LanguageInfo>(languages);
    }

    /// <summary>
    /// 命令列表过滤器
    /// </summary>
    private bool CommandFilter(object obj)
    {
        if (obj is not CommandConfig cmd) return false;
        if (string.IsNullOrWhiteSpace(CommandSearchText)) return true;
        var q = CommandSearchText;
        return cmd.Keyword.Contains(q, StringComparison.OrdinalIgnoreCase)
            || cmd.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || cmd.Type.Contains(q, StringComparison.OrdinalIgnoreCase)
            || cmd.Path.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 搜索文本变更时触发
    /// </summary>
    partial void OnCommandSearchTextChanged(string value)
    {
        CommandsView?.Refresh();
    }

    /// <summary>
    /// 添加命令
    /// </summary>
    [RelayCommand]
    private void AddCommand()
    {
        var cmd = new CommandConfig
        {
            Keyword = "",
            Name = "",
            Type = "Url",
            Path = "",
            Enabled = true,
            RunAsAdmin = false,
            RunHidden = false,
            ModifiedAt = DateTime.Now
        };
        Commands.Insert(0, cmd);
        ToastService.Instance.ShowSuccess(LocalizationService.Get("Added"));
    }

    /// <summary>
    /// 删除命令
    /// </summary>
    [RelayCommand]
    private void DeleteCommand(CommandConfig? command)
    {
        if (command != null)
        {
            Commands.Remove(command);
            ToastService.Instance.ShowSuccess(LocalizationService.Get("Deleted"));
        }
    }

    /// <summary>
    /// 保存命令
    /// </summary>
    [RelayCommand]
    private void SaveCommand()
    {
        var config = ConfigLoader.Load();
        config.Commands = Commands.Where(c => !c.IsBuiltIn).ToList();
        ConfigLoader.Save(config);
        ToastService.Instance.ShowSuccess(LocalizationService.Get("Saved"));
    }

    /// <summary>
    /// 切换主题
    /// </summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ThemeService.ApplyTheme(IsDarkTheme ? "Dark" : "Light");

        // 持久化配置
        var config = ConfigLoader.Load();
        if (config == null) config = new AppConfig();
        if (config.AppSettings == null) config.AppSettings = new AppSettings();
        config.Theme = IsDarkTheme ? "Dark" : "Light";
        ConfigLoader.Save(config);
    }

    /// <summary>
    /// 设置语言
    /// </summary>
    [RelayCommand]
    private void SetLanguage(string languageCode)
    {
        if (!string.IsNullOrEmpty(languageCode))
        {
            CurrentLanguage = languageCode;
            LocalizationService.CurrentLanguage = languageCode;

            // 持久化配置
            var config = ConfigLoader.Load();
            if (config.AppSettings == null) config.AppSettings = new AppSettings();
            config.AppSettings.Language = languageCode;
            ConfigLoader.Save(config);
        }
    }

    /// <summary>
    /// 选择菜单
    /// </summary>
    [RelayCommand]
    private void SelectMenu(string menuName)
    {
        CurrentMenu = menuName;
    }
}
