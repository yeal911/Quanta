// ============================================================================
// 文件名：MainWindow.Window.cs
// 文件用途：窗口显示/隐藏动画（ShowWindow、HideWindow、ToggleVisibility）
//          以及视觉树辅助方法。
// ============================================================================

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Quanta.Services;

namespace Quanta.Views;

public partial class MainWindow
{
    /// <summary>
    /// 切换窗口的显示/隐藏状态。由全局快捷键触发调用。
    /// </summary>
    public void ToggleVisibility()
    {
        if (_isVisible)
            HideWindow();
        else
            ShowWindow();
    }

    /// <summary>
    /// 显示主窗口：居中定位、清空搜索状态、播放淡入动画、聚焦搜索框。
    /// </summary>
    public void ShowWindow()
    {
        var screen = SystemParameters.WorkArea;
        Left = (screen.Width - Width) / 2;
        Top = (screen.Height - Height) / 2;

        // 动画优先级 > 本地值，必须先清除旧动画（HoldEnd 持有的 Opacity=1）
        // 再设本地值为 0，否则 Show() 时 Opacity 仍是 1，出现闪白
        BeginAnimation(OpacityProperty, null);
        Opacity = 0;

        Show(); Activate(); Focus();

        // WPF Window 只允许 Identity Transform（ScaleX=ScaleY=1），非 Identity 会抛异常
        // 保持 (1,1) 初始化；动画 From=0.9 在 BeginAnimation 调用瞬间即覆盖为 0.9
        // 此时 Opacity=0，窗口透明，用户看不到 Scale=1 的那一帧
        RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        var scaleTransform = new ScaleTransform(1, 1);
        RenderTransform = scaleTransform;

        _viewModel.ClearSearchCommand.Execute(null);
        ParamIndicator.Visibility = Visibility.Collapsed;
        SearchBox.Padding = new Thickness(6, 4, 0, 4);
        PlaceholderText.Visibility = Visibility.Visible;
        SearchBox.Focus();

        // Update toast position
        ToastService.Instance.SetMainWindow(this);

        // 淡入 + 缩放动画
        var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(100) };
        var scaleIn = new DoubleAnimation { From = 0.9, To = 1, Duration = TimeSpan.FromMilliseconds(100) };

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
        BeginAnimation(OpacityProperty, fadeIn);

        _isVisible = true;
    }

    /// <summary>
    /// 隐藏主窗口，播放淡出+缩小动画后执行 Hide()。
    /// </summary>
    private void HideWindow()
    {
        var fadeOut = new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromMilliseconds(100) };

        var scaleTransform = RenderTransform as ScaleTransform;
        if (scaleTransform == null)
        {
            scaleTransform = new ScaleTransform(1, 1);
            RenderTransform = scaleTransform;
        }
        var scaleOut = new DoubleAnimation { From = 1, To = 0.9, Duration = TimeSpan.FromMilliseconds(100) };

        fadeOut.Completed += (s, e) =>
        {
            Hide();
            if (_pendingPaste)
            {
                _pendingPaste = false;
                // 等待前台窗口重新获得焦点后再发送 Ctrl+V
                Task.Delay(150).ContinueWith(_ => Dispatcher.Invoke(() =>
                {
                    keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
                    keybd_event(VK_V, 0, 0, UIntPtr.Zero);
                    keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                }));
            }
        };
        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut);
        BeginAnimation(OpacityProperty, fadeOut);

        _isVisible = false;
    }

    /// <summary>
    /// 搜索结果列表加载完成时的事件处理（图标颜色通过 XAML DataTrigger 自动处理）。
    /// </summary>
    private void ResultsList_Loaded(object sender, RoutedEventArgs e)
    {
        // 图标颜色现在通过 XAML DataTrigger 自动处理
    }

    /// <summary>
    /// 将结果列表上的鼠标滚轮事件转发给外层 ScrollViewer，避免 ListBox 内部滚动宿主吞掉滚轮。
    /// </summary>
    private void ResultsList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ResultsScrollViewer == null)
            return;

        var nextOffset = ResultsScrollViewer.VerticalOffset - e.Delta;
        nextOffset = Math.Max(0, Math.Min(nextOffset, ResultsScrollViewer.ScrollableHeight));
        ResultsScrollViewer.ScrollToVerticalOffset(nextOffset);
        e.Handled = true;
    }

    /// <summary>
    /// 递归查找 Visual Tree 中指定类型的子元素。
    /// </summary>
    private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }
}
