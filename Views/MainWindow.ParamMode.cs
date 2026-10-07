// ============================================================================
// 文件名：MainWindow.ParamMode.cs
// 文件用途：参数模式 UI 切换的视图接线（Tab 触发的普通参数模式、
//          record 专用参数模式的 SearchBox 绑定切换、内边距与光标调整）。
//          模式状态与搜索联动在 MainViewModel，此处只操作视图元素。
// ============================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
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

        // 3. 清空 SearchBox（只更新 CommandParam，不影响 SearchText）
        SearchBox.Text = "";
        SearchBox.Focus();

        // 4. 调整左内边距并复位光标
        AdjustSearchBoxPaddingForParamIndicator();
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
    /// 进入参数输入模式：清空搜索框并聚焦，
    /// 动态调整搜索框左内边距以避免与关键字标签重叠。
    /// 关键字标签与占位符可见性由绑定跟随 ViewModel 状态自动更新。
    /// </summary>
    private void EnterParamMode(string keyword)
    {
        _viewModel.SwitchToParamModeCommand.Execute(keyword);
        SearchBox.Text = "";
        SearchBox.Focus();

        AdjustSearchBoxPaddingForParamIndicator();
    }

    /// <summary>
    /// 按参数指示器的实际宽度调整搜索框左内边距，并把光标移到起始处。
    /// </summary>
    private void AdjustSearchBoxPaddingForParamIndicator()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            ParamIndicator.Measure(new WpfSize(double.PositiveInfinity, double.PositiveInfinity));
            SearchBox.Padding = new Thickness(ParamIndicator.DesiredSize.Width + 6, 4, 0, 4);
            SearchBox.CaretIndex = 0;
        });
    }
}
