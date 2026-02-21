// ============================================================================
// 文件名: ToastService.cs
// 文件描述: Toast 通知服务，提供轻量级的弹窗通知功能。
//           支持成功、错误、警告和信息四种通知类型，带有淡入淡出动画效果，
//           通知会自动消失，也可以点击手动关闭。
// ============================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Quanta.Core.Interfaces;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Application = System.Windows.Application;
using IToastService = Quanta.Core.Interfaces.IToastService;

namespace Quanta.Services;

/// <summary>
/// Toast 通知服务，使用单例模式提供全局的轻量级弹窗通知功能。
/// 通知会以浮动窗口形式显示在可见窗口的中央位置，
/// 支持淡入淡出动画，自动定时消失，也可以通过点击立即关闭。
/// </summary>
public class ToastService : IToastService
{
    /// <summary>
    /// 单例实例（懒加载）
    /// </summary>
    private static ToastService? _instance;

    /// <summary>
    /// 获取 ToastService 的全局单例实例
    /// </summary>
    public static ToastService Instance => _instance ??= new ToastService();

    /// <summary>
    /// 当前活动的 Toast 窗口字典，Key 为唯一标识符，Value 为对应的窗口对象
    /// </summary>
    private readonly Dictionary<string, Window> _activeToasts = new();

    /// <summary>
    /// 主窗口引用（可选），用于确定 Toast 的显示位置
    /// </summary>
    private Window? _mainWindow;

    /// <summary>
    /// 当前是否为深色主题
    /// </summary>
    private bool _isDarkTheme = false;

    /// <summary>
    /// 设置主题模式
    /// </summary>
    /// <param name="isDarkTheme">是否为深色主题</param>
    public void SetTheme(bool isDarkTheme)
    {
        _isDarkTheme = isDarkTheme;
    }

    /// <summary>
    /// 设置主窗口引用，Toast 会尝试居中显示在主窗口上
    /// </summary>
    /// <param name="window">主窗口实例</param>
    public void SetMainWindow(Window window)
    {
        _mainWindow = window;
    }

    /// <summary>
    /// 显示一条 Toast 通知消息。
    /// 该方法确保在 UI 线程上执行，可从任意线程安全调用。
    /// </summary>
    /// <param name="message">通知消息文本</param>
    /// <param name="type">通知类型（默认为 Info）</param>
    /// <param name="duration">显示持续时间，单位为秒（默认 1.5 秒）</param>
    public void Show(string message, IToastService.ToastType type = IToastService.ToastType.Info, double duration = 1.5)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var toast = CreateToastWindow(message, type);
            ShowToast(toast, duration);
        });
    }

    /// <summary>
    /// 显示成功类型的 Toast 通知
    /// </summary>
    /// <param name="message">通知消息文本</param>
    /// <param name="duration">显示持续时间，单位为秒（默认 1.5 秒）</param>
    public void ShowSuccess(string message, double duration = 1.5)
        => Show(message, IToastService.ToastType.Success, duration);

    /// <summary>
    /// 显示错误类型的 Toast 通知
    /// </summary>
    /// <param name="message">通知消息文本</param>
    /// <param name="duration">显示持续时间，单位为秒（默认 1.5 秒）</param>
    public void ShowError(string message, double duration = 1.5)
        => Show(message, IToastService.ToastType.Error, duration);

    /// <summary>
    /// 显示警告类型的 Toast 通知
    /// </summary>
    /// <param name="message">通知消息文本</param>
    /// <param name="duration">显示持续时间，单位为秒（默认 1.5 秒）</param>
    public void ShowWarning(string message, double duration = 1.5)
        => Show(message, IToastService.ToastType.Warning, duration);

    /// <summary>
    /// 显示信息类型的 Toast 通知
    /// </summary>
    /// <param name="message">通知消息文本</param>
    /// <param name="duration">显示持续时间，单位为秒（默认 1.5 秒）</param>
    public void ShowInfo(string message, double duration = 1.5)
        => Show(message, IToastService.ToastType.Info, duration);

    /// <summary>
    /// 创建 Toast 通知窗口。
    /// 窗口为无边框透明窗口，包含圆角边框、图标和消息文本，
    /// 初始不透明度为 0（用于淡入动画）。
    /// </summary>
    /// <param name="message">通知消息文本</param>
    /// <param name="type">通知类型，用于决定样式</param>
    /// <returns>创建好的 Toast 窗口对象</returns>
    private Window CreateToastWindow(string message, IToastService.ToastType type)
    {
        var (bgColor, icon, iconColor) = GetToastStyle(type);

        // 根据主题调整背景色和文字色
        // 白色主题：Toast背景黑色，文字白色
        // 黑色主题：Toast背景白色，文字黑色
        Brush toastBackground;
        Brush messageForeground;
        if (_isDarkTheme)
        {
            // 深色主题：白色背景，黑色文字
            toastBackground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            messageForeground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }
        else
        {
            // 浅色主题：黑色背景，白色文字
            toastBackground = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            messageForeground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }

        var window = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            Topmost = true,
            ShowInTaskbar = false,
            Width = 320,
            Height = 50,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = SystemParameters.WorkArea.Right - 340,
            Top = SystemParameters.WorkArea.Bottom - 70,
            Opacity = 0
        };

        // 创建圆角边框容器，带阴影效果
        var border = new Border
        {
            Background = toastBackground,
            CornerRadius = new CornerRadius(8),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 15,
                ShadowDepth = 2,
                Opacity = 0.3
            }
        };

        // 使用 Grid 布局，左侧为图标列，右侧为消息文本列
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // 图标文本块
        var iconBlock = new TextBlock
        {
            Text = icon,
            FontSize = 18,
            Foreground = iconColor,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetColumn(iconBlock, 0);

        // 消息文本块
        var messageBlock = new TextBlock
        {
            Text = message,
            Foreground = messageForeground,
            FontSize = 13,
            FontFamily = new FontFamily("微软雅黑"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 15, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(messageBlock, 1);

        grid.Children.Add(iconBlock);
        grid.Children.Add(messageBlock);
        border.Child = grid;
        window.Content = border;

        return window;
    }

    /// <summary>
    /// 根据通知类型获取对应的样式配置（背景色、图标字符、图标颜色）
    /// </summary>
    /// <param name="type">通知类型</param>
    /// <returns>包含背景画刷、图标字符和图标颜色画刷的元组</returns>
    private (Brush bg, string icon, Brush iconColor) GetToastStyle(IToastService.ToastType type)
    {
        return type switch
        {
            IToastService.ToastType.Success => (new SolidColorBrush(Color.FromRgb(72, 187, 120)), "✓", new SolidColorBrush(Color.FromRgb(72, 187, 120))),
            IToastService.ToastType.Error => (new SolidColorBrush(Color.FromRgb(245, 101, 101)), "✕", new SolidColorBrush(Color.FromRgb(245, 101, 101))),
            IToastService.ToastType.Warning => (new SolidColorBrush(Color.FromRgb(234, 179, 8)), "⚠", new SolidColorBrush(Color.FromRgb(234, 179, 8))),
            _ => (new SolidColorBrush(Color.FromRgb(66, 153, 225)), "ℹ", new SolidColorBrush(Color.FromRgb(66, 153, 225)))
        };
    }

    /// <summary>
    /// 显示 Toast 窗口，包含定位、淡入动画、自动消失定时器和点击关闭逻辑。
    /// Toast 会尝试居中显示在当前可见的应用窗口上，如果没有可见窗口则居中显示在屏幕上。
    /// </summary>
    /// <param name="toast">要显示的 Toast 窗口</param>
    /// <param name="duration">显示持续时间（秒）</param>
    private void ShowToast(Window toast, double duration)
    {
        var key = Guid.NewGuid().ToString();
        _activeToasts[key] = toast;

        // 查找当前最顶层的可见窗口，用于确定 Toast 的居中位置
        Window? targetWindow = null;
        if (Application.Current?.Windows != null)
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win.IsVisible && win is not Window toastWin || !_activeToasts.ContainsValue(win))
                {
                    if (win.IsVisible && !(win.WindowStyle == WindowStyle.None && win.Width == 320 && win.Height == 50))
                    {
                        targetWindow = win;
                    }
                }
            }
        }

        if (targetWindow != null)
        {
            // 将 Toast 居中显示在目标窗口上
            toast.Left = targetWindow.Left + (targetWindow.ActualWidth - toast.Width) / 2;
            toast.Top = targetWindow.Top + (targetWindow.ActualHeight - toast.Height) / 2;
        }
        else
        {
            // 没有可见窗口时，居中显示在屏幕工作区
            toast.Left = (SystemParameters.WorkArea.Width - toast.Width) / 2;
            toast.Top = (SystemParameters.WorkArea.Height - toast.Height) / 2;
        }

        toast.Show();

        // 淡入动画（200 毫秒）
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        toast.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        // 自动消失定时器
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(duration) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            FadeOutAndClose(toast, key);
        };
        timer.Start();

        // 点击 Toast 窗口立即关闭
        toast.MouseLeftButtonDown += (s, e) =>
        {
            timer.Stop();
            FadeOutAndClose(toast, key);
        };
    }

    /// <summary>
    /// 对 Toast 窗口执行淡出动画，动画完成后关闭窗口并从活动列表中移除
    /// </summary>
    /// <param name="toast">要关闭的 Toast 窗口</param>
    /// <param name="key">Toast 在活动列表中的唯一标识符</param>
    private void FadeOutAndClose(Window toast, string key)
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        fadeOut.Completed += (s, e) =>
        {
            toast.Close();
            _activeToasts.Remove(key);
        };
        toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }
}
