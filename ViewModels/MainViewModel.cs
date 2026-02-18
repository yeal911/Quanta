// ============================================================================
// 文件名：MainViewModel.cs
// 文件用途：主窗口的视图模型，负责管理搜索逻辑、搜索结果列表、
//          参数模式切换、主题切换等核心业务逻辑。
//          使用 CommunityToolkit.Mvvm 框架实现数据绑定和命令模式。
// ============================================================================

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.ViewModels;

/// <summary>
/// 主窗口视图模型，管理搜索框输入、搜索结果显示、命令执行等核心交互逻辑。
/// 继承自 ObservableObject，支持属性变更通知。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    /// <summary>搜索引擎实例，负责执行搜索和命令</summary>
    private readonly SearchEngine _searchEngine;

    /// <summary>使用频率追踪器，用于记录命令使用统计</summary>
    private readonly UsageTracker _usageTracker;

    /// <summary>搜索防抖取消令牌源，用于取消上一次未完成的搜索</summary>
    private CancellationTokenSource? _searchCts;

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

    /// <summary>
    /// 用于显示在搜索框中的文本。
    /// 参数模式下显示"关键字 + 参数"，普通模式下显示搜索文本。
    /// </summary>
    public string DisplayText => IsParamMode ? $"{CommandKeyword} {CommandParam}" : SearchText;

    /// <summary>搜索结果的可观察集合，作为 ResultsView 的数据源</summary>
    public ObservableCollection<SearchResult> Results { get; } = new();

    /// <summary>
    /// 带分组支持的结果视图，绑定到 ListBox.ItemsSource。
    /// 当搜索结果包含多种类型时（命令/应用/文件/窗口）自动按 GroupLabel 分组显示。
    /// </summary>
    public ICollectionView ResultsView { get; }

    /// <summary>公开搜索引擎实例，供视图层直接调用执行命令</summary>
    public SearchEngine SearchEngine => _searchEngine;

    /// <summary>
    /// 构造函数，注入搜索引擎和使用追踪器依赖。
    /// </summary>
    /// <param name="searchEngine">搜索引擎实例</param>
    /// <param name="usageTracker">使用频率追踪器</param>
    public MainViewModel(SearchEngine searchEngine, UsageTracker usageTracker)
    {
        _searchEngine = searchEngine;
        _usageTracker = usageTracker;

        // 构建带分组描述的结果视图（按 GroupLabel 分组，空 GroupLabel 归为同一组不显示标题）
        ResultsView = CollectionViewSource.GetDefaultView(Results);
        ResultsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SearchResult.GroupLabel)));
    }

    /// <summary>
    /// 搜索文本变更时触发，通知 DisplayText 更新并执行异步搜索。
    /// 仅在非参数模式下触发搜索。
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayText));
        if (!IsParamMode)
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
    /// </summary>
    partial void OnCommandParamChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayText));
    }

    /// <summary>
    /// 参数模式切换时触发，通知 DisplayText 更新。
    /// </summary>
    partial void OnIsParamModeChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayText));
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
    /// 执行成功后自动清空搜索状态。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteSelected))]
    private async Task ExecuteSelectedAsync()
    {
        if (SelectedResult == null) return;

        bool success;
        if (IsParamMode && SelectedResult.Type == SearchResultType.CustomCommand)
        {
            success = await _searchEngine.ExecuteCustomCommandAsync(SelectedResult, CommandParam);
        }
        else
        {
            success = await _searchEngine.ExecuteResultAsync(SelectedResult);
        }

        if (success)
        {
            ClearSearch();
        }
    }

    /// <summary>
    /// 判断是否有选中的结果可供执行。
    /// </summary>
    /// <returns>如果有选中的结果返回 true</returns>
    private bool CanExecuteSelected() => SelectedResult != null;

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
    /// 切换明暗主题。
    /// </summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

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
            DebugLog.Log("Search error: {0}", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
