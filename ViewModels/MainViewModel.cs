using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SearchEngine _searchEngine;
    private readonly UsageTracker _usageTracker;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _commandKeyword = string.Empty;

    [ObservableProperty]
    private string _commandParam = string.Empty;

    [ObservableProperty]
    private bool _isParamMode;

    [ObservableProperty]
    private SearchResult? _selectedResult;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedIndex;

    [ObservableProperty]
    private bool _isDarkTheme;

    public string DisplayText => IsParamMode ? $"{CommandKeyword} {CommandParam}" : SearchText;

    public ObservableCollection<SearchResult> Results { get; } = new();

    public SearchEngine SearchEngine => _searchEngine;

    public MainViewModel(SearchEngine searchEngine, UsageTracker usageTracker)
    {
        _searchEngine = searchEngine;
        _usageTracker = usageTracker;
    }

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayText));
        if (!IsParamMode)
        {
            _ = SearchAsync();
        }
    }

    partial void OnCommandKeywordChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayText));
    }

    partial void OnCommandParamChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayText));
    }

    partial void OnIsParamModeChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayText));
    }

    partial void OnSelectedResultChanged(SearchResult? value)
    {
        ExecuteSelectedCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (value >= 0 && value < Results.Count)
        {
            SelectedResult = Results[value];
        }
    }

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

    private bool CanExecuteSelected() => SelectedResult != null;

    [RelayCommand(CanExecute = nameof(HasResults))]
    private void SelectNext()
    {
        if (Results.Count > 0)
        {
            SelectedIndex = (SelectedIndex + 1) % Results.Count;
        }
    }

    [RelayCommand(CanExecute = nameof(HasResults))]
    private void SelectPrevious()
    {
        if (Results.Count > 0)
        {
            SelectedIndex = SelectedIndex <= 0 ? Results.Count - 1 : SelectedIndex - 1;
        }
    }

    private bool HasResults() => Results.Count > 0;

    [RelayCommand]
    private void SwitchToParamMode(string keyword)
    {
        CommandKeyword = keyword;
        IsParamMode = true;
        CommandParam = "";
    }

    [RelayCommand]
    private void SwitchToNormalMode()
    {
        IsParamMode = false;
        CommandKeyword = "";
        CommandParam = "";
    }

    [RelayCommand]
    private void UpdateParam(string param)
    {
        CommandParam = param;
        SearchText = CommandKeyword + " " + param;
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchText = "";
        SwitchToNormalMode();
        Results.Clear();
        SelectedIndex = -1;
        SelectedResult = null;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

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
            Logger.Log($"Search error: {ex.Message}");
        }
        finally 
        { 
            IsLoading = false; 
        }
    }
}
