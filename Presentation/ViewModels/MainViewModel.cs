// ============================================================================
// 文件名：MainViewModel.cs
// 文件用途：主窗口的视图模型，负责管理搜索逻辑、搜索结果列表、
//          参数模式切换、主题切换、全局热键注册、语言切换等核心业务逻辑。
//          使用 CommunityToolkit.Mvvm 框架实现数据绑定和命令模式。
//          录音业务逻辑见 MainViewModel.Recording.cs 分部类。
// ============================================================================

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quanta.Core.Config;
using Quanta.Core.Interfaces;
using Quanta.Domain.Search;
using Quanta.Infrastructure.Logging;
using Quanta.Presentation.Helpers;

namespace Quanta.Presentation.ViewModels;

/// <summary>
/// Tab 键在当前搜索结果上可触发的动作类型。
/// </summary>
public enum TabAction
{
    /// <summary>无自定义命令可补全，选择下一项（普通 Tab 行为）</summary>
    SelectNext,

    /// <summary>匹配到自定义命令，进入其参数输入模式</summary>
    EnterParamMode,

    /// <summary>匹配到录音命令，进入 record 专用参数模式</summary>
    EnterRecordParamMode,
}

/// <summary>
/// 主窗口视图模型，管理搜索框输入、搜索结果显示、命令执行、热键注册等核心交互逻辑。
/// 继承自 ObservableObject，支持属性变更通知。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    /// <summary>搜索引擎实例，负责执行搜索和命令</summary>
    private readonly SearchEngine _searchEngine;

    /// <summary>使用频率追踪器，用于记录命令使用统计</summary>
    private readonly UsageTracker _usageTracker;

    /// <summary>配置加载服务（DI 注入）</summary>
    private readonly IConfigLoader _configLoader;

    /// <summary>全局热键管理器（DI 注入）</summary>
    private readonly IHotkeyManager _hotkeyManager;

    /// <summary>搜索防抖取消令牌源，用于取消上一次未完成的搜索</summary>
    private CancellationTokenSource? _searchCts;

    /// <summary>当前热键配置，用于占位符文本显示</summary>
    private HotkeyConfig? _hotkeyConfig;

    /// <summary>执行完毕后是否需要向前台窗口发送 Ctrl+V 粘贴</summary>
    private bool _pendingPaste;

    /// <summary>搜索框中的文本内容</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>参数模式下的命令关键字（如 "google"）</summary>
    [ObservableProperty]
    private string _commandKeyword = string.Empty;

    /// <summary>参数模式下用户输入的参数部分</summary>
    [ObservableProperty]
    private string _commandParam = string.Empty;

    /// <summary>是否处于参数输入模式（Tab 键进入自定义命令的参数输入状态）</summary>
    [ObservableProperty]
    private bool _isParamMode;

    /// <summary>当前选中的搜索结果项</summary>
    [ObservableProperty]
    private SearchResult? _selectedResult;

    /// <summary>是否正在加载搜索结果</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>当前选中结果的索引</summary>
    [ObservableProperty]
    private int _selectedIndex;

    /// <summary>是否为暗色主题</summary>
    [ObservableProperty]
    private bool _isDarkTheme;

    /// <summary>搜索框占位符文本（含当前快捷键信息）</summary>
    [ObservableProperty]
    private string _placeholderText = string.Empty;

    /// <summary>
    /// 用于显示在搜索框中的文本。
    /// 参数模式下显示"关键字 + 参数"，普通模式下显示搜索文本。
    /// </summary>
    public string DisplayText => IsParamMode ? $"{CommandKeyword} {CommandParam}" : SearchText;

    /// <summary>占位符是否可见：非参数模式且搜索框为空时显示</summary>
    public bool IsPlaceholderVisible => !IsParamMode && string.IsNullOrEmpty(SearchText);

    /// <summary>搜索结果的可观察集合，作为 ResultsView 的数据源</summary>
    public ObservableCollection<SearchResult> Results { get; } = new();

    /// <summary>
    /// 带分组支持的结果视图，绑定到 ListBox.ItemsSource。
    /// 当搜索结果包含多种类型时（命令/应用/文件/窗口）自动按 GroupLabel 分组显示。
    /// </summary>
    public ICollectionView ResultsView { get; }

    /// <summary>公开搜索引擎实例，供视图层直接调用执行命令</summary>
    public SearchEngine SearchEngine => _searchEngine;

    /// <summary>注册的全局热键被按下时触发（转发自 HotkeyManager），供视图层切换窗口可见性</summary>
    public event EventHandler? HotkeyPressed;

    /// <summary>命令执行完成且搜索文本已清空时触发，请求视图层隐藏窗口</summary>
    public event EventHandler? HideRequested;

    /// <summary>界面语言切换完成后触发，供视图层刷新本地化文本并重建托盘菜单</summary>
    public event EventHandler? LocalizationChanged;

    /// <summary>用户请求退出应用（录音中会被拦截）时触发，供视图层释放托盘并关闭应用</summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// 构造函数，注入搜索引擎、使用追踪器、配置、热键管理等依赖。
    /// </summary>
    /// <param name="searchEngine">搜索引擎实例</param>
    /// <param name="usageTracker">使用频率追踪器</param>
    /// <param name="configLoader">配置加载服务</param>
    /// <param name="recordingService">录音服务</param>
    /// <param name="hotkeyManager">全局热键管理器</param>
    public MainViewModel(
        SearchEngine searchEngine,
        UsageTracker usageTracker,
        IConfigLoader configLoader,
        IRecordingService recordingService,
        IHotkeyManager hotkeyManager)
    {
        _searchEngine = searchEngine;
        _usageTracker = usageTracker;
        _configLoader = configLoader;
        _recordingService = recordingService;
        _hotkeyManager = hotkeyManager;

        // 构建带分组描述的结果视图（按 GroupLabel 分组，空 GroupLabel 归为同一组不显示标题）
        ResultsView = CollectionViewSource.GetDefaultView(Results);
        ResultsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SearchResult.GroupLabel)));

        // 转发热键按下事件，视图层无需直接依赖 HotkeyManager
        _hotkeyManager.HotkeyPressed += (s, e) => HotkeyPressed?.Invoke(s, e);
    }

    // ── 搜索 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 搜索文本变更时触发，通知 DisplayText 更新并执行异步搜索。
    /// 参数模式下同步参数值（原 SearchBox.TextChanged 的职责）；
    /// record 参数模式下 SearchText 由 CommandParam 推导，此处不同步以避免回环。
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(IsPlaceholderVisible));

        if (IsParamMode && CommandKeyword != "record")
        {
            CommandParam = value;
        }

        // record 参数模式需要实时搜索以更新文件名预览；其他参数模式不触发搜索
        if (!IsParamMode || CommandKeyword == "record")
        {
            _ = SearchAsync();
        }
    }

    /// <summary>
    /// 命令关键字变更时触发，通知 DisplayText 更新。
    /// </summary>
    partial void OnCommandKeywordChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayText));
    }

    /// <summary>
    /// 命令参数变更时触发，通知 DisplayText 更新。
    /// record 参数模式下同步 SearchText 以触发实时搜索（文件名预览等）。
    /// </summary>
    partial void OnCommandParamChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayText));
        // record 参数模式：同步 SearchText 触发 SearchAsync，使搜索结果显示录音命令
        if (IsParamMode && CommandKeyword == "record")
        {
            SearchText = string.IsNullOrEmpty(value) ? "record" : "record " + value;
        }
    }

    /// <summary>
    /// 参数模式切换时触发，通知 DisplayText 与占位符可见性更新。
    /// </summary>
    partial void OnIsParamModeChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(IsPlaceholderVisible));
    }

    /// <summary>
    /// 选中结果变更时触发，通知执行命令的可执行状态更新。
    /// </summary>
    partial void OnSelectedResultChanged(SearchResult? value)
    {
        ExecuteSelectedCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 选中索引变更时触发，同步更新 SelectedResult。
    /// </summary>
    partial void OnSelectedIndexChanged(int value)
    {
        if (value >= 0 && value < Results.Count)
        {
            SelectedResult = Results[value];
        }
    }

    /// <summary>
    /// 执行当前选中的搜索结果。
    /// 参数模式下使用自定义命令执行，普通模式下直接执行结果。
    /// 剪贴板历史项执行后标记待粘贴；执行成功后自动清空搜索状态并请求隐藏窗口。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteSelected))]
    private async Task ExecuteSelectedAsync()
    {
        if (SelectedResult == null)
        {
            Logger.Debug("ExecuteSelectedAsync: SelectedResult is null!");
            return;
        }

        Logger.Debug($"ExecuteSelectedAsync: IsParamMode={IsParamMode}, Type={SelectedResult.Type}, CommandParam='{CommandParam}'");

        // 剪贴板历史项：执行后自动粘贴到前台窗口
        if (SelectedResult.GroupLabel == LocalizationService.Get("GroupClip"))
            _pendingPaste = true;

        bool success;
        if (IsParamMode && SelectedResult.Type == SearchResultType.CustomCommand)
        {
            Logger.Debug("ExecuteSelectedAsync: Using ExecuteCustomCommandAsync");
            success = await _searchEngine.ExecuteCustomCommandAsync(SelectedResult, CommandParam);
        }
        else
        {
            Logger.Debug("ExecuteSelectedAsync: Using ExecuteResultAsync");
            success = await _searchEngine.ExecuteResultAsync(SelectedResult, "");
        }

        if (success)
        {
            ClearSearch();
        }

        // 执行成功后搜索文本被清空，此时请求视图隐藏窗口
        if (string.IsNullOrEmpty(SearchText))
        {
            HideRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 判断是否有选中的结果可供执行。
    /// </summary>
    /// <returns>如果有选中的结果返回 true</returns>
    private bool CanExecuteSelected() => SelectedResult != null;

    /// <summary>
    /// 按索引快速执行搜索结果（Ctrl+数字 快捷键）。
    /// </summary>
    /// <param name="index">结果列表中的索引（0 起）</param>
    [RelayCommand]
    private void ExecuteByIndex(int index)
    {
        if (index >= 0 && index < Results.Count)
        {
            SelectedIndex = index;
            ExecuteSelectedCommand.Execute(null);
        }
    }

    /// <summary>
    /// 选择结果列表中的下一项（循环选择）。
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasResults))]
    private void SelectNext()
    {
        if (Results.Count > 0)
        {
            SelectedIndex = (SelectedIndex + 1) % Results.Count;
        }
    }

    /// <summary>
    /// 选择结果列表中的上一项（循环选择）。
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasResults))]
    private void SelectPrevious()
    {
        if (Results.Count > 0)
        {
            SelectedIndex = SelectedIndex <= 0 ? Results.Count - 1 : SelectedIndex - 1;
        }
    }

    /// <summary>
    /// 判断结果列表是否非空。
    /// </summary>
    /// <returns>有结果时返回 true</returns>
    private bool HasResults() => Results.Count > 0;

    /// <summary>
    /// 计算 Tab 键应触发的动作：优先匹配自定义命令进入参数模式，
    /// 其次匹配录音命令进入 record 参数模式，否则选择下一项。
    /// </summary>
    /// <returns>动作类型及自定义命令关键字（仅 EnterParamMode 时非空）</returns>
    public (TabAction Action, string? Keyword) GetTabAction()
    {
        string? matchedKeyword = null;
        bool hasRecordCommand = false;
        foreach (var result in Results)
        {
            if (result.Type == SearchResultType.CustomCommand)
            {
                matchedKeyword = result.Title;
                break;
            }
            if (result.Type == SearchResultType.RecordCommand)
            {
                hasRecordCommand = true;
            }
        }

        if (matchedKeyword != null)
            return (TabAction.EnterParamMode, matchedKeyword);
        if (hasRecordCommand)
            return (TabAction.EnterRecordParamMode, null);
        return (TabAction.SelectNext, null);
    }

    /// <summary>
    /// 切换到参数输入模式，设置命令关键字并清空参数。
    /// </summary>
    /// <param name="keyword">要进入参数模式的命令关键字</param>
    [RelayCommand]
    private void SwitchToParamMode(string keyword)
    {
        CommandKeyword = keyword;
        IsParamMode = true;
        CommandParam = "";
    }

    /// <summary>
    /// 切换回普通搜索模式，清空关键字和参数。
    /// </summary>
    [RelayCommand]
    private void SwitchToNormalMode()
    {
        IsParamMode = false;
        CommandKeyword = "";
        CommandParam = "";
    }

    /// <summary>
    /// 更新参数模式下的参数值，同时同步 SearchText。
    /// </summary>
    /// <param name="param">新的参数值</param>
    [RelayCommand]
    private void UpdateParam(string param)
    {
        CommandParam = param;
        SearchText = CommandKeyword + " " + param;
    }

    /// <summary>
    /// 清空搜索状态：重置搜索文本、退出参数模式、清空结果列表。
    /// </summary>
    [RelayCommand]
    public void ClearSearch()
    {
        SearchText = "";
        SwitchToNormalMode();
        Results.Clear();
        SelectedIndex = -1;
        SelectedResult = null;
    }

    /// <summary>
    /// 消费"执行后粘贴"标记：读取并复位，返回是否需要向前台窗口发送 Ctrl+V。
    /// </summary>
    public bool ConsumePendingPaste()
    {
        if (!_pendingPaste) return false;
        _pendingPaste = false;
        return true;
    }

    /// <summary>
    /// 复制颜色结果的指定格式值到剪贴板并提示。
    /// </summary>
    /// <param name="result">颜色搜索结果</param>
    /// <param name="format">格式：Hex / Rgb / Hsl</param>
    public void CopyColor(SearchResult result, string format)
    {
        var colorInfo = result.ColorInfo;
        if (colorInfo == null) return;

        var value = format switch
        {
            "Hex" => colorInfo.Hex,
            "Rgb" => colorInfo.Rgb,
            "Hsl" => colorInfo.Hsl,
            _ => null,
        };
        if (value == null) return;

        System.Windows.Clipboard.SetText(value);
        ToastService.Instance.ShowInfo(LocalizationService.Get("ColorCopied", value));
    }

    // ── 主题 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 主题变更事件，供视图层订阅以更新 UI（如图标）。
    /// </summary>
    public event EventHandler<bool>? ThemeChanged;

    /// <summary>
    /// 启动时恢复上次保存的主题（Dark/Light）并应用到应用程序。
    /// </summary>
    public void ApplyInitialTheme()
    {
        var config = _configLoader.Load();
        IsDarkTheme = config.Theme?.Equals("Dark", StringComparison.OrdinalIgnoreCase) ?? false;
        ThemeService.ApplyTheme(IsDarkTheme ? "Dark" : "Light");
        ToastService.Instance.SetTheme(IsDarkTheme);
    }

    /// <summary>
    /// 切换明暗主题，并应用主题到应用程序，持久化到配置文件。
    /// </summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;

        // 应用主题
        ThemeService.ApplyTheme(IsDarkTheme ? "Dark" : "Light");
        ToastService.Instance.SetTheme(IsDarkTheme);

        // 持久化配置
        var config = _configLoader.Load();
        config.Theme = IsDarkTheme ? "Dark" : "Light";
        _configLoader.Save(config);

        // 触发事件通知视图层更新（如图标）
        ThemeChanged?.Invoke(this, IsDarkTheme);
    }

    // ── 热键 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 初始化全局热键：绑定窗口句柄并按当前配置注册，同时刷新占位符文本。
    /// </summary>
    /// <param name="windowHandle">主窗口句柄（由视图层提供）</param>
    /// <returns>注册成功返回 true；冲突或配置无法解析返回 false</returns>
    public bool InitializeHotkey(IntPtr windowHandle)
    {
        var config = _configLoader.Load();
        _hotkeyConfig = config.Hotkey;

        var registered = _hotkeyManager.Initialize(windowHandle, config.Hotkey);

        // 日志输出当前快捷键配置
        Logger.Debug($"[Hotkey] Config loaded: Modifier={config.Hotkey.Modifier}, Key={config.Hotkey.Key}");

        UpdatePlaceholderText();
        return registered;
    }

    /// <summary>
    /// 设置窗口关闭后重新加载配置并重注册热键（热键可能被用户修改）。
    /// </summary>
    /// <returns>重注册成功返回 true</returns>
    public bool ReregisterHotkey()
    {
        var config = _configLoader.Load();
        _hotkeyConfig = config.Hotkey;

        var registered = _hotkeyManager.Reregister(config.Hotkey);

        UpdatePlaceholderText();
        return registered;
    }

    /// <summary>
    /// 根据当前热键配置与界面语言刷新搜索框占位符文本。
    /// </summary>
    private void UpdatePlaceholderText()
    {
        var hotkey = _hotkeyConfig;
        var hotkeyStr = hotkey == null ? "" : $"{hotkey.Modifier}+{hotkey.Key}";
        PlaceholderText = LocalizationService.Get("SearchPlaceholder") + " | " + hotkeyStr;
    }

    // ── 语言 / 关于 / 退出 ───────────────────────────────────────────

    /// <summary>
    /// 切换界面语言，刷新占位符文本并通知视图层刷新本地化文本。
    /// </summary>
    /// <param name="code">语言代码（如 zh-CN）</param>
    [RelayCommand]
    private void SwitchLanguage(string code)
    {
        LocalizationService.CurrentLanguage = code;
        UpdatePlaceholderText();
        LocalizationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 显示"关于"信息提示。
    /// </summary>
    [RelayCommand]
    private void ShowAbout()
    {
        ToastService.Instance.ShowInfo(
            $"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
    }

    /// <summary>
    /// 请求退出应用：录音进行中时提示并拦截，否则通知视图层释放托盘并关闭应用。
    /// </summary>
    [RelayCommand]
    private void ExitApplication()
    {
        if (!ConfirmCanExit()) return;
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    // ── 搜索执行 ─────────────────────────────────────────────────────

    /// <summary>
    /// 异步执行搜索。使用防抖机制（30ms 延迟）避免频繁搜索。
    /// 每次搜索前取消上一次未完成的搜索请求。
    /// </summary>
    private async Task SearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        try
        {
            IsLoading = true;
            await Task.Delay(30, _searchCts.Token);

            var results = await _searchEngine.SearchAsync(SearchText, _searchCts.Token);

            Results.Clear();
            foreach (var r in results)
            {
                Results.Add(r);
            }

            SelectedIndex = Results.Count > 0 ? 0 : -1;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.Debug($"Search error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
