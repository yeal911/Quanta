// ============================================================================
// 文件名：MainWindow.Keyboard.cs
// 文件用途：键盘事件处理、搜索结果执行、列表交互、设置窗口。
// ============================================================================

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Quanta.Helpers;
using Quanta.Models;
using Quanta.Services;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Quanta.Views;

public partial class MainWindow
{
    /// <summary>
    /// 窗口键盘按下预处理事件。
    /// 处理 Ctrl+数字 快速执行、Escape 退出/返回、方向键选择、Enter 执行、Tab 补全等快捷键。
    /// </summary>
    private void Window_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        // Ctrl+数字 快速执行
        if (e.Key >= Key.D1 && e.Key <= Key.D9 && Keyboard.Modifiers == ModifierKeys.Control)
        {
            int index = e.Key - Key.D1;
            ExecuteByIndex(index);
            e.Handled = true;
            return;
        }

        // Ctrl+数字 (小键盘)
        if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad9 && Keyboard.Modifiers == ModifierKeys.Control)
        {
            int index = e.Key - Key.NumPad1;
            ExecuteByIndex(index);
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                if (_viewModel.IsParamMode)
                {
                    RestoreSearchBinding();
                    _viewModel.SwitchToNormalModeCommand.Execute(null);
                    SearchBox.Text = "";
                    ParamIndicator.Visibility = Visibility.Collapsed;
                    SearchBox.Padding = new Thickness(6, 4, 0, 4);
                    PlaceholderText.Visibility = Visibility.Visible;
                }
                else
                {
                    HideWindow();
                }
                e.Handled = true;
                break;

            case Key.Down:
                _viewModel.SelectNextCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Up:
                _viewModel.SelectPreviousCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Enter:
                _ = ExecuteSelectedAsync();
                e.Handled = true;
                break;

            case Key.Tab:
                HandleTabKey();
                e.Handled = true;
                break;

            case Key.Back:
                if (_viewModel.IsParamMode)
                {
                    // 参数模式下的删除逻辑：
                    // 情况3→情况2：SearchBox有参数，删除参数字符，参数空了进入情况2
                    // 情况2→情况1：SearchBox为空，删除">"，退出参数模式但保留命令关键字

                    if (string.IsNullOrEmpty(SearchBox.Text))
                    {
                        // 情况2：SearchBox已空，删除">"退出参数模式
                        var keyword = _viewModel.CommandKeyword;
                        _viewModel.SwitchToNormalModeCommand.Execute(null);
                        SearchBox.Text = keyword;
                        SearchBox.CaretIndex = SearchBox.Text.Length;
                        ParamIndicator.Visibility = Visibility.Collapsed;
                        SearchBox.Padding = new Thickness(6, 4, 0, 4);
                        PlaceholderText.Visibility = Visibility.Collapsed;
                        e.Handled = true;
                    }
                    else if (SearchBox.Text.Length == 1)
                    {
                        // 情况3→情况2：只剩一个参数字符，删除后变成空
                        _viewModel.CommandParam = "";
                        // 不拦截，让系统处理删除
                    }
                    else
                    {
                        // 情况3：有多个参数字符，正常删除
                        _viewModel.CommandParam = SearchBox.Text.Substring(0, SearchBox.Text.Length - 1);
                        // 不拦截，让系统处理删除
                    }
                }
                break;
        }
    }

    /// <summary>
    /// 按索引快速执行搜索结果。用于 Ctrl+数字 快捷键。
    /// </summary>
    private void ExecuteByIndex(int index)
    {
        if (index >= 0 && index < _viewModel.Results.Count)
        {
            _viewModel.SelectedIndex = index;
            _ = ExecuteSelectedAsync();
        }
    }

    /// <summary>
    /// 处理 Tab 键逻辑：
    /// 参数模式下聚焦搜索框并移动光标到末尾；
    /// 普通模式下尝试匹配自定义命令进入参数模式，否则选择下一项。
    /// </summary>
    private void HandleTabKey()
    {
        if (_viewModel.IsParamMode)
        {
            SearchBox.Focus();
            SearchBox.CaretIndex = SearchBox.Text.Length;
            return;
        }

        string? matchedKeyword = null;
        bool hasRecordCommand = false;
        foreach (var result in _viewModel.Results)
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
        {
            EnterParamMode(matchedKeyword);
            return;
        }

        if (hasRecordCommand)
        {
            EnterRecordParamMode();
            return;
        }

        // Normal tab behavior - select next item
        _viewModel.SelectNextCommand.Execute(null);
    }

    /// <summary>
    /// 同步执行当前选中的搜索结果（Fire-and-Forget 模式）。
    /// </summary>
    private void ExecuteSelected()
    {
        if (_viewModel.SelectedResult == null)
        {
            Logger.Debug("ExecuteSelected: SelectedResult is null!");
            return;
        }

        Logger.Debug($"ExecuteSelected: IsParamMode={_viewModel.IsParamMode}, Type={_viewModel.SelectedResult.Type}, CommandConfig={_viewModel.SelectedResult.CommandConfig?.Keyword}, CommandParam='{_viewModel.CommandParam}'");

        Task.Run(async () =>
        {
            bool success;
            if (_viewModel.IsParamMode && _viewModel.SelectedResult?.CommandConfig != null)
            {
                success = await _viewModel.SearchEngine.ExecuteCustomCommandAsync(_viewModel.SelectedResult, _viewModel.CommandParam);
            }
            else
            {
                success = _viewModel.SelectedResult != null
                    ? await _viewModel.SearchEngine.ExecuteResultAsync(_viewModel.SelectedResult)
                    : false;
            }

            if (success)
            {
                Dispatcher.Invoke(() =>
                {
                    _viewModel.ClearSearchCommand.Execute(null);
                    HideWindow();
                });
            }
        });
    }

    /// <summary>
    /// 异步执行当前选中的搜索结果，执行成功且搜索文本为空时自动隐藏窗口。
    /// </summary>
    private async Task ExecuteSelectedAsync()
    {
        // 剪贴板历史项：执行后自动粘贴到前台窗口
        if (_viewModel.SelectedResult?.GroupLabel == "Clip")
            _pendingPaste = true;

        await _viewModel.ExecuteSelectedCommand.ExecuteAsync(null);
        if (string.IsNullOrEmpty(_viewModel.SearchText))
        {
            HideWindow();
        }
    }

    /// <summary>
    /// 结果列表选择变更事件处理，自动滚动到选中项使其可见。
    /// </summary>
    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem != null)
            ResultsList.ScrollIntoView(ResultsList.SelectedItem);
    }

    /// <summary>
    /// 结果列表鼠标单击事件处理，点击即执行选中的搜索结果。
    /// </summary>
    private void ResultsList_MouseClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var item = (e.OriginalSource as FrameworkElement)?.DataContext as SearchResult;
        if (item != null)
        {
            _viewModel.SelectedResult = item;
            _ = ExecuteSelectedAsync();
        }
    }

    /// <summary>
    /// 打开命令设置窗口。窗口关闭后自动重新加载快捷键配置和命令列表。
    /// </summary>
    private void OpenCommandSettings(object? sender = null, RoutedEventArgs? e = null)
    {
        var win = new CommandSettingsWindow(_viewModel.SearchEngine) { Owner = this };
        win.SetDarkTheme(_viewModel.IsDarkTheme);
        win.Show();

        win.Closed += (s, args) =>
        {
            var config = ConfigLoader.Load();
            var registered = _hotkeyManager.Reregister(config.Hotkey);
            _viewModel.SearchEngine.ReloadCommands();
            UpdatePlaceholderWithHotkey();
            if (!registered)
            {
                ToastService.Instance.ShowWarning(LocalizationService.Get("HotkeyRegisterFailed"));
            }
        };
    }
}
