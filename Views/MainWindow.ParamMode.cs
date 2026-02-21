// ============================================================================
// 文件名：MainWindow.ParamMode.cs
// 文件用途：参数模式 UI 切换（Tab 触发的普通参数模式、record 专用参数模式）。
// ============================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfBinding = System.Windows.Data.Binding;
using WpfSize = System.Windows.Size;

namespace Quanta.Views;

public partial class MainWindow
{
    /// <summary>
    /// 进入 record 专用参数模式。
    /// 关键点：先把 SearchBox 的绑定从 SearchText 切换到 CommandParam，
    /// 再调用 SwitchToParamMode，这样清空 SearchBox 不会把 SearchText 置空，
    /// OnCommandParamChanged 负责更新 SearchText="record " 触发搜索，结果保持录音命令。
    /// </summary>
    private void EnterRecordParamMode()
    {
        _isRecordParamMode = true;

        // 1. 先切换绑定：SearchBox ↔ CommandParam（而非 SearchText）
        BindingOperations.ClearBinding(SearchBox, WpfTextBox.TextProperty);
        SearchBox.SetBinding(WpfTextBox.TextProperty, new WpfBinding("CommandParam")
        {
            Source = _viewModel,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });

        // 2. 切换到 param 模式（此时 OnCommandParamChanged→SearchText="record"→搜索→RecordCommand 结果）
        _viewModel.SwitchToParamModeCommand.Execute("record");
        ParamKeywordText.Text = "record";
        ParamIndicator.Visibility = Visibility.Visible;
        PlaceholderText.Visibility = Visibility.Collapsed;

        // 3. 清空 SearchBox（只更新 CommandParam，不影响 SearchText）
        SearchBox.Text = "";
        SearchBox.Focus();

        // 4. 调整左内边距
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            ParamIndicator.Measure(new WpfSize(double.PositiveInfinity, double.PositiveInfinity));
            SearchBox.Padding = new Thickness(ParamIndicator.DesiredSize.Width + 6, 4, 0, 4);
            SearchBox.CaretIndex = 0;
        });
    }

    /// <summary>
    /// 退出 record 参数模式，把 SearchBox 绑定还原回 SearchText。
    /// 当 IsParamMode 变为 false 时（Escape/执行后）由 PropertyChanged 钩子自动调用。
    /// </summary>
    private void RestoreSearchBinding()
    {
        if (!_isRecordParamMode) return;
        _isRecordParamMode = false;
        BindingOperations.ClearBinding(SearchBox, WpfTextBox.TextProperty);
        SearchBox.SetBinding(WpfTextBox.TextProperty, new WpfBinding("SearchText")
        {
            Source = _viewModel,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
    }

    /// <summary>
    /// 进入参数输入模式：显示命令关键字标签，清空搜索框，
    /// 并动态调整搜索框左内边距以避免与关键字标签重叠。
    /// </summary>
    private void EnterParamMode(string keyword)
    {
        _viewModel.SwitchToParamModeCommand.Execute(keyword);
        ParamKeywordText.Text = keyword;
        ParamIndicator.Visibility = Visibility.Visible;
        PlaceholderText.Visibility = Visibility.Collapsed;
        SearchBox.Text = "";
        SearchBox.Focus();

        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            ParamIndicator.Measure(new WpfSize(double.PositiveInfinity, double.PositiveInfinity));
            var indicatorWidth = ParamIndicator.DesiredSize.Width;
            SearchBox.Padding = new Thickness(indicatorWidth + 6, 4, 0, 4);
            SearchBox.CaretIndex = 0;
        });
    }

    /// <summary>
    /// 更新参数指示器的显示状态和位置。
    /// 参数模式下显示关键字标签并调整搜索框内边距；普通模式下隐藏标签。
    /// </summary>
    private void UpdateParamIndicator()
    {
        if (_viewModel.IsParamMode)
        {
            ParamKeywordText.Text = _viewModel.CommandKeyword;
            ParamIndicator.Visibility = Visibility.Visible;
            PlaceholderText.Visibility = Visibility.Collapsed;

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                ParamIndicator.Measure(new WpfSize(double.PositiveInfinity, double.PositiveInfinity));
                var indicatorWidth = ParamIndicator.DesiredSize.Width;
                SearchBox.Padding = new Thickness(indicatorWidth + 6, 4, 0, 4);
            });
        }
        else
        {
            ParamIndicator.Visibility = Visibility.Collapsed;
            SearchBox.Padding = new Thickness(6, 4, 0, 4);
        }
    }
}
