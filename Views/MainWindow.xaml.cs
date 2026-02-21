// ============================================================================
// 文件名：MainWindow.xaml.cs
// 文件用途：主窗口的代码隐藏文件，负责处理窗口的显示/隐藏动画、
//          全局快捷键注册、系统托盘初始化、键盘事件分发、
//          参数模式 UI 切换以及搜索结果的交互逻辑。
// ============================================================================

using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Media.Animation;
using Quanta.Helpers;
using Quanta.Models;
using Quanta.Services;
using Quanta.ViewModels;
using Quanta.Interfaces;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfBinding = System.Windows.Data.Binding;

namespace Quanta.Views;

/// <summary>
/// 主窗口类，作为 Quanta 启动器的核心 UI 界面。
/// 负责搜索框交互、快捷键响应、窗口动画、系统托盘管理等功能。
/// </summary>
public partial class MainWindow : Window, IMainWindowService
{
    /// <summary>主窗口的视图模型，管理搜索逻辑和数据</summary>
    private readonly MainViewModel _viewModel;

    /// <summary>全局快捷键管理器，用于注册和监听系统级热键</summary>
    private readonly HotkeyManager _hotkeyManager;

    /// <summary>窗口句柄，用于快捷键注册和 Win32 交互</summary>
    private IntPtr _windowHandle;

    /// <summary>窗口当前是否可见</summary>
    private bool _isVisible;

    /// <summary>托盘双击显示窗口的时间戳，用于短暂阻止自动隐藏</summary>
    public DateTime LastShownFromTray { get; set; }

    /// <summary>系统托盘服务，管理托盘图标和右键菜单</summary>
    private TrayService? _trayService;

    /// <summary>剪贴板变化监听器</summary>
    private readonly ClipboardMonitor _clipboardMonitor;

    /// <summary>执行完毕后是否需要向前台窗口发送 Ctrl+V 粘贴</summary>
    private bool _pendingPaste;

    /// <summary>当前录音服务实例（录音进行中时不为 null）</summary>
    private Services.RecordingService? _recordingService;

    /// <summary>当前录音悬浮窗口</summary>
    private RecordingOverlayWindow? _recordingOverlay;

    /// <summary>是否正处于 record 专用参数模式（SearchBox 绑定切换到 CommandParam）</summary>
    private bool _isRecordParamMode = false;

    // ── Win32：模拟键盘输入 ────────────────────────────────────
    [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr extra);
    private const byte VK_CONTROL = 0x11;
    private const byte VK_V = 0x56;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>
    /// 构造函数，通过 DI 注入所有依赖服务。
    /// </summary>
    public MainWindow(
        MainViewModel viewModel,
        HotkeyManager hotkeyManager,
        ClipboardMonitor clipboardMonitor)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _hotkeyManager = hotkeyManager;
        _clipboardMonitor = clipboardMonitor;

        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;

        MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;

        // 订阅主题变更事件，更新 UI 图标
        _viewModel.ThemeChanged += (s, isDark) => UpdateThemeIcon(isDark);
    }

    /// <summary>
    /// 窗口失去焦点时自动隐藏。
    /// 若有子窗口（如设置窗口）打开则跳过，避免误隐藏。
    /// </summary>
    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (OwnedWindows.Count > 0) return;
        
        // 如果窗口是最近1秒内从托盘显示的，不隐藏（避免双击托盘图标时闪烁）
        if ((DateTime.Now - LastShownFromTray).TotalMilliseconds < 1000) return;
        
        HideWindow();
    }

    /// <summary>
    /// 鼠标左键按下时允许拖动窗口。
    /// </summary>
    private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    /// <summary>
    /// 搜索框文本变更事件处理。
    /// 参数模式下更新 ViewModel 的参数值；普通模式下切换占位符可见性。
    /// </summary>
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel.IsParamMode)
        {
            // In param mode, update the param in ViewModel
            _viewModel.CommandParam = SearchBox.Text;
            // Placeholder stays hidden, ParamIndicator stays visible
        }
        else
        {
            // Show/hide placeholder based on text
            PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// 窗口加载完成事件处理。
    /// 依次执行：加载语言设置、设置占位符文本、初始化 Toast 服务、
    /// 注册全局快捷键、初始化系统托盘、构建搜索图标菜单，
    /// 最后隐藏窗口等待快捷键唤起。
    /// </summary>
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Load language setting
        LocalizationService.LoadFromConfig();

        // 当 IsParamMode 变为 false 时，还原 SearchBox 绑定（record 参数模式退出时使用）
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsParamMode) && !_viewModel.IsParamMode)
                RestoreSearchBinding();
        };

        // Update placeholder with current hotkey
        UpdatePlaceholderWithHotkey();

        // Initialize ToastService with main window
        ToastService.Instance.SetMainWindow(this);

        _windowHandle = new WindowInteropHelper(this).Handle;
        var config = ConfigLoader.Load();

        // 恢复上次保存的主题（Dark/Light）
        var savedTheme = config.Theme?.Equals("Dark", StringComparison.OrdinalIgnoreCase) ?? false;
        _viewModel.IsDarkTheme = savedTheme;
        ApplyTheme(savedTheme);
        ToastService.Instance.SetTheme(savedTheme);

        var registered = _hotkeyManager.Initialize(_windowHandle, config.Hotkey);
        _hotkeyManager.HotkeyPressed += (s, args) => Dispatcher.Invoke(() => ToggleVisibility());
        
        // 日志输出当前快捷键配置
        Logger.Log($"[Hotkey] Config loaded: Modifier={config.Hotkey?.Modifier}, Key={config.Hotkey?.Key}");
        
        if (!registered)
        {
            Dispatcher.BeginInvoke(() =>
                ToastService.Instance.ShowWarning(LocalizationService.Get("HotkeyRegisterFailed")));
        }

        // Initialize system tray
        _trayService = new TrayService(this);
        _trayService.SettingsRequested += (s, args) => Dispatcher.Invoke(() => OpenCommandSettings());
        _trayService.ExitRequested += (s, args) => Dispatcher.Invoke(() => _trayService?.Dispose());
        _trayService.CanExit = () =>
        {
            if (_recordingService != null && _recordingService.State != Services.RecordingState.Idle)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    ToastService.Instance.ShowWarning(LocalizationService.Get("RecordAlreadyRecording")));
                return false;
            }
            return true;
        };
        _trayService.Initialize();

        BuildSearchIconMenu();

        // 启动剪贴板监听（窗口 Handle 在 Loaded 后可用）
        _clipboardMonitor.Start(_windowHandle);
        _clipboardMonitor.ClipboardChanged += text => ClipboardHistoryService.Instance.Add(text);

        SearchBox.Focus();
        Hide();
        _isVisible = false;

        Logger.Debug("MainWindow loaded");
    }

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
        var scaleTransform = new System.Windows.Media.ScaleTransform(1, 1);
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

        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleIn);
        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleIn);
        BeginAnimation(OpacityProperty, fadeIn);
        
        _isVisible = true;
    }

    // ── IMainWindowService 接口实现 ───────────────────────────────────

    /// <summary>是否处于暗色主题（实现 IMainWindowService）</summary>
    public bool IsDarkTheme => _viewModel.IsDarkTheme;

    /// <summary>切换主题（实现 IMainWindowService，委托给 ViewModel）</summary>
    public void ToggleTheme()
    {
        _viewModel.ToggleThemeCommand.Execute(null);
    }

    /// <summary>
    /// 主题切换按钮点击事件处理。
    /// 委托给 ViewModel 处理（主题逻辑已内聚到 ViewModel）。
    /// </summary>
    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleThemeCommand.Execute(null);
    }

    /// <summary>
    /// 应用主题：通过 ThemeService 切换 MergedDictionaries，所有使用 DynamicResource 的控件自动刷新。
    /// 只需额外更新无法用 DynamicResource 绑定的图标文字。
    /// </summary>
    /// <param name="isDark">是否为暗色主题</param>
    public void ApplyTheme(bool isDark)
    {
        ThemeService.ApplyTheme(isDark ? "Dark" : "Light");
        UpdateThemeIcon(isDark);
    }

    /// <summary>更新主题切换按钮的图标文字（☀ / 🌙）</summary>
    public void UpdateThemeIcon(bool isDark)
    {
        if (FindName("ThemeIcon") is TextBlock icon)
            icon.Text = isDark ? "☀" : "🌙";
    }

    /// <summary>
    /// 搜索结果列表加载完成时，订阅集合变化事件以自动更新新项的图标颜色
    /// </summary>
    private void ResultsList_Loaded(object sender, RoutedEventArgs e)
    {
        // 图标颜色现在通过 XAML DataTrigger 自动处理
    }

    /// <summary>
    /// 递归查找子元素
    /// </summary>
    private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }

    /// <summary>
    /// 主题切换按钮右键点击，显示上下文菜单（与托盘菜单相同）
    /// </summary>
    private void ThemeToggleButton_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Build a context menu similar to tray menu
        var menu = new System.Windows.Controls.ContextMenu();
        
        // Settings
        var settingsItem = new System.Windows.Controls.MenuItem { Header = LocalizationService.Get("TraySettings") };
        settingsItem.Click += (s, args) => OpenCommandSettings();
        menu.Items.Add(settingsItem);
        
        // Language submenu - dynamic generation
        var langItem = new System.Windows.Controls.MenuItem { Header = LocalizationService.Get("TrayLanguage") };
        foreach (var lang in LocalizationService.GetSupportedLanguages())
        {
            var langMenuItem = new System.Windows.Controls.MenuItem 
            { 
                Header = LocalizationService.Get(lang.Code), 
                IsChecked = LocalizationService.CurrentLanguage == lang.Code 
            };
            langMenuItem.Click += (s, args) => 
            { 
                LocalizationService.CurrentLanguage = lang.Code; 
                RefreshLocalization(); 
                _trayService?.Initialize(); 
            };
            langItem.Items.Add(langMenuItem);
        }
        menu.Items.Add(langItem);
        
        menu.Items.Add(new System.Windows.Controls.Separator());
        
        // About
        var aboutItem = new System.Windows.Controls.MenuItem { Header = LocalizationService.Get("TrayAbout") };
        aboutItem.Click += (s, args) => ToastService.Instance.ShowInfo($"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
        menu.Items.Add(aboutItem);
        
        // Exit
        var exitItem = new System.Windows.Controls.MenuItem { Header = LocalizationService.Get("TrayExit") };
        exitItem.Click += (s, args) =>
        {
            if (_recordingService != null && _recordingService.State != Services.RecordingState.Idle)
            {
                ToastService.Instance.ShowWarning(LocalizationService.Get("RecordAlreadyRecording"));
                return;
            }
            _trayService?.Dispose();
            System.Windows.Application.Current.Shutdown();
        };
        menu.Items.Add(exitItem);
        
        // Show menu
        menu.IsOpen = true;
        e.Handled = true;
    }

    /// <summary>
    /// 隐藏主窗口，播放淡出+缩小动画后执行 Hide()。
    /// </summary>
    private void HideWindow()
    {
        // Fancy hide animation: scale + fade out
        var fadeOut = new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromMilliseconds(100) };
        
        var scaleTransform = RenderTransform as System.Windows.Media.ScaleTransform;
        if (scaleTransform == null)
        {
            scaleTransform = new System.Windows.Media.ScaleTransform(1, 1);
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
        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleOut);
        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleOut);
        BeginAnimation(OpacityProperty, fadeOut);
        
        _isVisible = false;
    }

    /// <summary>
    /// 窗口键盘按下预处理事件。
    /// 处理 Ctrl+数字 快速执行、Escape 退出/返回、方向键选择、Enter 执行、Tab 补全等快捷键。
    /// </summary>
    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
                // Execute using async method
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
                        // 保留命令关键字到 SearchBox，让用户可以继续删除命令
                        var keyword = _viewModel.CommandKeyword;
                        _viewModel.SwitchToNormalModeCommand.Execute(null);
                        SearchBox.Text = keyword;
                        SearchBox.CaretIndex = SearchBox.Text.Length;
                        ParamIndicator.Visibility = Visibility.Collapsed;
                        SearchBox.Padding = new Thickness(6, 4, 0, 4);
                        PlaceholderText.Visibility = Visibility.Collapsed; // 有内容时不显示占位符
                        e.Handled = true;
                    }
                    else if (SearchBox.Text.Length == 1)
                    {
                        // 情况3→情况2：只剩一个参数字符，删除后变成空
                        // 先让系统删除这个字符，然后变成情况2
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
    /// <param name="index">结果列表中的索引（0-based）</param>
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

        // Try to find the first matching CustomCommand in results
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

        // Record 命令：进入专用参数模式（绑定切换 + 显示 "record >" 指示符）
        if (hasRecordCommand)
        {
            EnterRecordParamMode();
            return;
        }

        // Normal tab behavior - select next item
        _viewModel.SelectNextCommand.Execute(null);
    }

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
            ParamIndicator.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
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
    /// <param name="keyword">进入参数模式的命令关键字</param>
    private void EnterParamMode(string keyword)
    {
        _viewModel.SwitchToParamModeCommand.Execute(keyword);
        ParamKeywordText.Text = keyword;
        ParamIndicator.Visibility = Visibility.Visible;
        PlaceholderText.Visibility = Visibility.Collapsed;
        SearchBox.Text = "";
        SearchBox.Focus();

        // Adjust SearchBox left padding to avoid overlapping with ParamIndicator
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            ParamIndicator.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
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
                ParamIndicator.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
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

    /// <summary>
    /// 刷新界面的本地化文本（如搜索框占位符、菜单项文字等）。
    /// 在语言切换后调用。
    /// </summary>
    public void RefreshLocalization()
    {
        UpdatePlaceholderWithHotkey();
        BuildSearchIconMenu();
        UpdateTooltips();
    }

    /// <summary>
    /// 更新所有界面元素的 ToolTip 文本
    /// </summary>
    private void UpdateTooltips()
    {
        // 主题切换按钮 ToolTip
        if (FindName("ThemeToggleButton") is System.Windows.Controls.Button themeBtn)
        {
            themeBtn.ToolTip = LocalizationService.Get("ThemeSwitch");
        }

        // 颜色复制 ToolTip
        try
        {
            if (FindName("CopyHexTooltip") is System.Windows.Controls.ToolTip hexTip)
                hexTip.Content = LocalizationService.Get("RightClickCopy");
            if (FindName("CopyRgbTooltip") is System.Windows.Controls.ToolTip rgbTip)
                rgbTip.Content = LocalizationService.Get("RightClickCopy");
            if (FindName("CopyHslTooltip") is System.Windows.Controls.ToolTip hslTip)
                hslTip.Content = LocalizationService.Get("RightClickCopy");
        }
        catch
        {
            // ToolTips 可能尚未初始化
        }
    }

    /// <summary>
    /// 更新搜索框占位符，包含当前快捷键信息
    /// </summary>
    private void UpdatePlaceholderWithHotkey()
    {
        var config = ConfigLoader.Load();
        var hotkey = config.Hotkey;
        var hotkeyStr = $"{hotkey.Modifier}+{hotkey.Key}";
        PlaceholderText.Text = LocalizationService.Get("SearchPlaceholder") + " | " + hotkeyStr;
    }

    /// <summary>
    /// 构建搜索图标的右键上下文菜单，包含设置、语言切换、关于、退出等菜单项。
    /// </summary>
    private void BuildSearchIconMenu()
    {
        SearchIconMenu.Items.Clear();

        // Removed "Show" menu item per user request

        var settingsItem = new MenuItem { Header = LocalizationService.Get("TraySettings") };
        settingsItem.Click += (s, e) => OpenCommandSettings();
        SearchIconMenu.Items.Add(settingsItem);

        // Language submenu - dynamic generation
        var langItem = new MenuItem { Header = LocalizationService.Get("TrayLanguage") };
        foreach (var lang in LocalizationService.GetSupportedLanguages())
        {
            var langMenuItem = new MenuItem 
            { 
                Header = LocalizationService.Get(lang.Code), 
                IsChecked = LocalizationService.CurrentLanguage == lang.Code 
            };
            langMenuItem.Click += (s, e) => 
            { 
                LocalizationService.CurrentLanguage = lang.Code; 
                RefreshLocalization(); 
                _trayService?.Initialize(); 
            };
            langItem.Items.Add(langMenuItem);
        }
        SearchIconMenu.Items.Add(langItem);

        SearchIconMenu.Items.Add(new Separator());

        var aboutItem = new MenuItem { Header = LocalizationService.Get("TrayAbout") };
        aboutItem.Click += (s, e) => ToastService.Instance.ShowInfo($"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
        SearchIconMenu.Items.Add(aboutItem);

        var exitItem = new MenuItem { Header = LocalizationService.Get("TrayExit") };
        exitItem.Click += (s, e) =>
        {
            if (_recordingService != null && _recordingService.State != Services.RecordingState.Idle)
            {
                ToastService.Instance.ShowWarning(LocalizationService.Get("RecordAlreadyRecording"));
                return;
            }
            _trayService?.Dispose();
            System.Windows.Application.Current.Shutdown();
        };
        SearchIconMenu.Items.Add(exitItem);
    }

    /// <summary>
    /// 搜索图标右键点击事件处理，打开上下文菜单。
    /// </summary>
    private void SearchIcon_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        BuildSearchIconMenu();
        SearchIconMenu.IsOpen = true;
        e.Handled = true;
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
    private void ResultsList_MouseClick(object sender, MouseButtonEventArgs e)
    {
        // 确保点击的是列表项而非空白区域
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
    /// <param name="sender">事件发送者（可选）</param>
    /// <param name="e">路由事件参数（可选）</param>
    private void OpenCommandSettings(object? sender = null, RoutedEventArgs? e = null)
    {
        var win = new CommandSettingsWindow(_viewModel.SearchEngine) { Owner = this };
        win.SetDarkTheme(_viewModel.IsDarkTheme);
        win.Show();

        // After settings window closes, reload hotkey and commands
        win.Closed += (s, args) =>
        {
            var config = ConfigLoader.Load();
            var registered = _hotkeyManager.Reregister(config.Hotkey);
            _viewModel.SearchEngine.ReloadCommands();
            UpdatePlaceholderWithHotkey(); // Refresh hotkey hint in placeholder
            if (!registered)
            {
                ToastService.Instance.ShowWarning(LocalizationService.Get("HotkeyRegisterFailed"));
            }
        };
    }

    /// <summary>
    /// 同步执行当前选中的搜索结果（Fire-and-Forget 模式）。
    /// 在后台线程执行命令，成功后在 UI 线程清空搜索并隐藏窗口。
    /// </summary>
    private void ExecuteSelected()
    {
        // Execute immediately without waiting
        if (_viewModel.SelectedResult == null) 
        {
            Logger.Debug("ExecuteSelected: SelectedResult is null!");
            return;
        }

        Logger.Debug($"ExecuteSelected: IsParamMode={_viewModel.IsParamMode}, Type={_viewModel.SelectedResult.Type}, CommandConfig={_viewModel.SelectedResult.CommandConfig?.Keyword}, CommandParam='{_viewModel.CommandParam}'");

        // Fire and forget - execute in background
        Task.Run(async () =>
        {
            bool success;
            // 参数模式下执行自定义命令（支持 Shell、Program 等类型）
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

    // ── 录音命令处理 ─────────────────────────────────────────────────

    /// <summary>
    /// 从搜索结果启动录音（由 SearchEngine 通过 Dispatcher 调用）。
    /// </summary>
    public async void StartRecordingFromResult(Models.SearchResult result)
    {
        try
        {
            if (_recordingService != null && _recordingService.State != Services.RecordingState.Idle)
            {
                ToastService.Instance.ShowWarning(LocalizationService.Get("RecordAlreadyRecording"));
                return;
            }

            var recordData = result.RecordData;
            if (recordData == null) return;

            // 构建最终输出文件路径
            var outputPath = recordData.OutputFileName;
            var outputDir = System.IO.Path.GetDirectoryName(outputPath) ?? "";

            // 保存当前配置到 AppConfig
            var config = Helpers.ConfigLoader.Load();
            config.RecordingSettings.Source = recordData.Source;
            config.RecordingSettings.Format = recordData.Format;
            config.RecordingSettings.Bitrate = recordData.Bitrate;
            config.RecordingSettings.Channels = recordData.Channels;
            config.RecordingSettings.OutputPath = recordData.OutputPath;
            Helpers.ConfigLoader.Save(config);

            // 创建录音服务
            _recordingService = new Services.RecordingService();

            // 创建悬浮窗口（先创建窗口再开始录音，以便订阅事件）
            _recordingOverlay = new RecordingOverlayWindow(_recordingService, outputDir);
            _recordingOverlay.Closed += (s, e) =>
            {
                _recordingOverlay = null;
                if (_recordingService?.State != Services.RecordingState.Idle)
                {
                    _ = _recordingService?.StopAsync();
                }
                _recordingService?.Dispose();
                _recordingService = null;
            };

            // 先不显示窗口，等录音启动成功后再显示
            // _recordingOverlay.Show();  // 注释掉，立即显示

            // 开始录音
            bool started = await _recordingService.StartAsync(config.RecordingSettings, outputPath);
            if (!started)
            {
                _recordingOverlay?.Close();
                _recordingService?.Dispose();
                _recordingService = null;
                _recordingOverlay = null;
                return;
            }

            // 录音启动成功后，等待窗口加载完成再显示，然后切换到录音界面
            _recordingOverlay.Dispatcher.Invoke(() => { });
            _recordingOverlay.Show();
            _recordingOverlay.ShowRecordingUI();
        }
        catch (Exception ex)
        {
            Logger.Error($"StartRecordingFromResult failed: {ex}");
            ToastService.Instance.ShowError(LocalizationService.Get("RecordError") + ": " + ex.Message);
            
            // 清理资源
            try { _recordingOverlay?.Close(); } catch { }
            try { _recordingService?.Dispose(); } catch { }
            _recordingService = null;
            _recordingOverlay = null;
        }
    }

    /// <summary>
    /// 处理录音配置芯片的右键点击，显示上下文菜单以切换配置值。
    /// </summary>
    private void RecordChip_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var element = sender as System.Windows.FrameworkElement;
        if (element == null) return;

        // 找到 DataContext 中的 SearchResult
        var result = element.DataContext as Models.SearchResult;
        var recordData = result?.RecordData;
        if (recordData == null) return;

        var tag = element.Tag?.ToString() ?? "";

        switch (tag)
        {
            case "Source":
                CycleOption(new[] { "Mic", "Speaker", "Mic&Speaker" }, recordData.Source, val =>
                {
                    recordData.Source = val;
                    SaveRecordingSettingField("Source", val);
                });
                break;

            case "Format":
                CycleOption(new[] { "m4a", "mp3" }, recordData.Format, val =>
                {
                    recordData.Format = val;
                    SaveRecordingSettingField("Format", val);
                });
                break;

            case "Bitrate":
                CycleOption(new[] { "64", "96", "128", "160" }, recordData.Bitrate.ToString(), val =>
                {
                    recordData.Bitrate = int.Parse(val);
                    SaveRecordingSettingField("Bitrate", val);
                }, v => v + " kbps");
                break;

            case "Channels":
                CycleOption(new[] { "1", "2" }, recordData.Channels.ToString(), val =>
                {
                    recordData.Channels = int.Parse(val);
                    SaveRecordingSettingField("Channels", val);
                }, v => v == "1" ? "单声道" : "立体声");
                break;
        }

        e.Handled = true;
    }

    /// <summary>
    /// 通过右键点击循环切换选项
    /// </summary>
    private void CycleOption(string[] options, string currentValue, Action<string> onChange, Func<string, string>? displayFormatter = null)
    {
        int currentIndex = Array.IndexOf(options, currentValue);
        int nextIndex = (currentIndex + 1) % options.Length;
        string newValue = options[nextIndex];
        onChange(newValue);
    }

    /// <summary>生成配置菜单项</summary>
    private static void AddMenuItems(
        System.Windows.Controls.ContextMenu menu,
        string[] values,
        string current,
        Action<string> onSelect,
        Func<string, string>? labelFormatter = null)
    {
        foreach (var val in values)
        {
            var label = labelFormatter != null ? labelFormatter(val) : val;
            var item = new System.Windows.Controls.MenuItem
            {
                Header = label,
                IsChecked = val.Equals(current, StringComparison.OrdinalIgnoreCase)
            };
            var capturedVal = val;
            item.Click += (s, e) => onSelect(capturedVal);
            menu.Items.Add(item);
        }
    }

    /// <summary>将单个录音配置字段保存到 AppConfig</summary>
    private static void SaveRecordingSettingField(string field, string value)
    {
        var config = Helpers.ConfigLoader.Load();
        switch (field)
        {
            case "Source": config.RecordingSettings.Source = value; break;
            case "Format": config.RecordingSettings.Format = value; break;
            case "Bitrate": config.RecordingSettings.Bitrate = int.TryParse(value, out int br) ? br : 128; break;
        }
        Helpers.ConfigLoader.Save(config);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 录音进行中时阻止退出，提示用户先停止录音
        if (_recordingService != null && _recordingService.State != Services.RecordingState.Idle)
        {
            ToastService.Instance.ShowWarning(LocalizationService.Get("RecordAlreadyRecording"));
            e.Cancel = true;
            return;
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _clipboardMonitor.Stop();
        // 若有正在进行的录音，停止并释放资源
        if (_recordingService != null)
        {
            _ = _recordingService.StopAsync();
            _recordingService.Dispose();
        }
        _recordingOverlay?.Close();
        base.OnClosed(e);
    }

    // ── 颜色复制事件处理 ───────────────────────────────────────────
    private void CopyColorHex_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
        {
            var textBlock = contextMenu.PlacementTarget as TextBlock;
            if (textBlock?.DataContext is SearchResult result && result.ColorInfo != null)
            {
                System.Windows.Clipboard.SetText(result.ColorInfo.Hex);
                ToastService.Instance.ShowInfo("已复制: " + result.ColorInfo.Hex);
            }
        }
    }

    private void CopyColorRgb_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
        {
            var textBlock = contextMenu.PlacementTarget as TextBlock;
            if (textBlock?.DataContext is SearchResult result && result.ColorInfo != null)
            {
                System.Windows.Clipboard.SetText(result.ColorInfo.Rgb);
                ToastService.Instance.ShowInfo("已复制: " + result.ColorInfo.Rgb);
            }
        }
    }

    private void CopyColorHsl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
        {
            var textBlock = contextMenu.PlacementTarget as TextBlock;
            if (textBlock?.DataContext is SearchResult result && result.ColorInfo != null)
            {
                System.Windows.Clipboard.SetText(result.ColorInfo.Hsl);
                ToastService.Instance.ShowInfo("已复制: " + result.ColorInfo.Hsl);
            }
        }
    }

    // ── 右键直接复制颜色 ───────────────────────────────────────────
    private void CopyColorHex_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.DataContext is SearchResult result && result.ColorInfo != null)
        {
            System.Windows.Clipboard.SetText(result.ColorInfo.Hex);
            ToastService.Instance.ShowInfo("已复制: " + result.ColorInfo.Hex);
        }
    }

    private void CopyColorRgb_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.DataContext is SearchResult result && result.ColorInfo != null)
        {
            System.Windows.Clipboard.SetText(result.ColorInfo.Rgb);
            ToastService.Instance.ShowInfo("已复制: " + result.ColorInfo.Rgb);
        }
    }

    private void CopyColorHsl_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.DataContext is SearchResult result && result.ColorInfo != null)
        {
            System.Windows.Clipboard.SetText(result.ColorInfo.Hsl);
            ToastService.Instance.ShowInfo("已复制: " + result.ColorInfo.Hsl);
        }
    }
}
