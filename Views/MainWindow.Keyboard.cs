// ============================================================================
// 文件名：MainWindow.Keyboard.cs
// 文件用途：键盘事件 → ViewModel 命令的视图接线：
//          Ctrl+数字快速执行、Escape 退出/返回、方向键选择、
//          Enter 执行、Tab 补全、参数模式下的 Backspace 删除、
//          结果列表交互、设置窗口打开。
//          键盘决策逻辑在 MainViewModel（GetTabAction / ExecuteByIndex 等）。
// ============================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using Quanta.Domain.Search;
using Quanta.Presentation.Helpers;
using Quanta.Presentation.ViewModels;

namespace Quanta.Views;

public partial class MainWindow
{
    /// <summary>
    /// 窗口键盘按下预处理事件。
    /// 处理 Ctrl+数字 快速执行、Escape 退出/返回、方向键选择、Enter 执行、Tab 补全等快捷键。
    /// </summary>
    private void Window_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        // Ctrl+数字 快速执行（主键盘与小键盘）
        if (Keyboard.Modifiers == ModifierKeys.Control &&
            ((e.Key >= Key.D1 && e.Key <= Key.D9) || (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad9)))
        {
            int index = (e.Key >= Key.D1 && e.Key <= Key.D9) ? e.Key - Key.D1 : e.Key - Key.NumPad1;
            _viewModel.ExecuteByIndexCommand.Execute(index);
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                if (_viewModel.IsParamMode)
                {
                    // 退出参数模式（指示器/占位符可见性由绑定自动还原）
                    RestoreSearchBinding();
                    _viewModel.SwitchToNormalModeCommand.Execute(null);
                    SearchBox.Text = "";
                    SearchBox.Padding = new Thickness(6, 4, 0, 4);
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
                _viewModel.ExecuteSelectedCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Tab:
                HandleTabKey();
                e.Handled = true;
                break;

            case Key.Back:
                HandleBackspaceInParamMode(e);
                break;
        }
    }

    /// <summary>
    /// 处理 Tab 键：动作决策在 MainViewModel.GetTabAction，
    /// 这里只做参数模式的 UI 切换与命令分发。
    /// </summary>
    private void HandleTabKey()
    {
        if (_viewModel.IsParamMode)
        {
            SearchBox.Focus();
            SearchBox.CaretIndex = SearchBox.Text.Length;
            return;
        }

        var (action, keyword) = _viewModel.GetTabAction();
        switch (action)
        {
            case TabAction.EnterParamMode:
                EnterParamMode(keyword!);
                return;

            case TabAction.EnterRecordParamMode:
                EnterRecordParamMode();
                return;

            default:
                // Normal tab behavior - select next item
                _viewModel.SelectNextCommand.Execute(null);
                break;
        }
    }

    /// <summary>
    /// 参数模式下的 Backspace 删除逻辑（文本框操作属视图职责）：
    /// 情况3→情况2：SearchBox 有参数，删除参数字符，参数空了进入情况2；
    /// 情况2→情况1：SearchBox 为空，删除"&gt;"，退出参数模式但保留命令关键字。
    /// </summary>
    private void HandleBackspaceInParamMode(WpfKeyEventArgs e)
    {
        if (!_viewModel.IsParamMode) return;

        if (string.IsNullOrEmpty(SearchBox.Text))
        {
            // 情况2：SearchBox 已空，删除">"退出参数模式（保留关键字）
            var keyword = _viewModel.CommandKeyword;
            _viewModel.SwitchToNormalModeCommand.Execute(null);
            SearchBox.Text = keyword;
            SearchBox.CaretIndex = SearchBox.Text.Length;
            SearchBox.Padding = new Thickness(6, 4, 0, 4);
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
            _viewModel.ExecuteSelectedCommand.Execute(null);
        }
    }

    /// <summary>
    /// 打开命令设置窗口。窗口关闭后自动重注册快捷键并重新加载命令列表
    /// （业务在 MainViewModel.ReregisterHotkey）。
    /// </summary>
    private void OpenCommandSettings(object? sender = null, RoutedEventArgs? e = null)
    {
        var win = new CommandSettingsWindow(_viewModel.SearchEngine) { Owner = this };
        win.SetDarkTheme(_viewModel.IsDarkTheme);
        win.Show();

        win.Closed += (s, args) =>
        {
            _viewModel.SearchEngine.ReloadCommands();
            if (!_viewModel.ReregisterHotkey())
            {
                ToastService.Instance.ShowWarning(LocalizationService.Get("HotkeyRegisterFailed"));
            }
        };
    }
}
