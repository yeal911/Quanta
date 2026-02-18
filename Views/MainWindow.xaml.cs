// ============================================================================
// 文件名：MainWindow.xaml.cs
// 文件用途：主窗口的代码隐藏文件，负责处理窗口的显示/隐藏动画、
//          全局快捷键注册、系统托盘初始化、键盘事件分发、
//          参数模式 UI 切换以及搜索结果的交互逻辑。
// ============================================================================

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Quanta.Helpers;
using Quanta.Models;
using Quanta.Services;
using Quanta.ViewModels;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace Quanta.Views;

/// <summary>
/// 主窗口类，作为 Quanta 启动器的核心 UI 界面。
/// 负责搜索框交互、快捷键响应、窗口动画、系统托盘管理等功能。
/// </summary>
public partial class MainWindow : Window
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

    /// <summary>
    /// 构造函数，初始化组件并创建各服务实例。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        var usageTracker = new UsageTracker();
        var commandRouter = new CommandRouter(usageTracker);
        var searchEngine = new SearchEngine(usageTracker, commandRouter);

        _viewModel = new MainViewModel(searchEngine, usageTracker);
        _hotkeyManager = new HotkeyManager();

        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;

        MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
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
        if (!registered)
        {
            Dispatcher.BeginInvoke(() =>
                ToastService.Instance.ShowWarning(LocalizationService.Get("HotkeyRegisterFailed")));
        }

        // Initialize system tray
        _trayService = new TrayService(this);
        _trayService.SettingsRequested += (s, args) => Dispatcher.Invoke(() => OpenCommandSettings());
        _trayService.ExitRequested += (s, args) => Dispatcher.Invoke(() => _trayService?.Dispose());
        _trayService.Initialize();

        BuildSearchIconMenu();

        SearchBox.Focus();
        Hide();
        _isVisible = false;

        DebugLog.Log("MainWindow loaded");
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

    /// <summary>
    /// 主题切换按钮点击事件处理。
    /// 切换 ViewModel 的主题状态，更新 UI 颜色，并将新主题持久化到配置文件。
    /// </summary>
    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleThemeCommand.Execute(null);
        ApplyTheme(_viewModel.IsDarkTheme);
        ToastService.Instance.SetTheme(_viewModel.IsDarkTheme);

        // 持久化主题设置到配置文件
        var config = ConfigLoader.Load();
        config.Theme = _viewModel.IsDarkTheme ? "Dark" : "Light";
        ConfigLoader.Save(config);
    }

    /// <summary>
    /// 将指定主题的颜色方案应用到主窗口的各 UI 元素。
    /// 统一供启动时恢复主题和切换主题时使用。
    /// </summary>
    /// <param name="isDark">是否为暗色主题</param>
    private void ApplyTheme(bool isDark)
    {
        var border = FindName("MainBorder") as Border;
        var icon   = FindName("ThemeIcon") as TextBlock;

        if (border == null || icon == null) return;

        // 搜索结果图标颜色（亮色白色，暗色也用白色更显眼）
        var iconForeground = isDark 
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));

        if (isDark)
        {
            border.Background  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 51, 51));
            SearchBox.Foreground  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            SearchBox.CaretBrush  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            PlaceholderText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));
            icon.Text       = "☀";
            icon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        }
        else
        {
            border.Background  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224));
            SearchBox.Foreground  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
            SearchBox.CaretBrush  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
            PlaceholderText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170));
            icon.Text       = "🌙";
            icon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
        }
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
        
        // Language submenu
        var langItem = new System.Windows.Controls.MenuItem { Header = LocalizationService.Get("TrayLanguage") };
        var zhItem = new System.Windows.Controls.MenuItem { Header = LocalizationService.Get("TrayChinese"), IsChecked = LocalizationService.CurrentLanguage == "zh-CN" };
        zhItem.Click += (s, args) => { LocalizationService.CurrentLanguage = "zh-CN"; RefreshLocalization(); _trayService?.Initialize(); };
        var enItem = new System.Windows.Controls.MenuItem { Header = LocalizationService.Get("TrayEnglish"), IsChecked = LocalizationService.CurrentLanguage == "en-US" };
        enItem.Click += (s, args) => { LocalizationService.CurrentLanguage = "en-US"; RefreshLocalization(); _trayService?.Initialize(); };
        langItem.Items.Add(zhItem);
        langItem.Items.Add(enItem);
        menu.Items.Add(langItem);
        
        menu.Items.Add(new System.Windows.Controls.Separator());
        
        // About
        var aboutItem = new System.Windows.Controls.MenuItem { Header = LocalizationService.Get("TrayAbout") };
        aboutItem.Click += (s, args) => ToastService.Instance.ShowInfo($"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
        menu.Items.Add(aboutItem);
        
        // Exit
        var exitItem = new System.Windows.Controls.MenuItem { Header = LocalizationService.Get("TrayExit") };
        exitItem.Click += (s, args) => { _trayService?.Dispose(); System.Windows.Application.Current.Shutdown(); };
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
        
        fadeOut.Completed += (s, e) => Hide();
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
                // Execute immediately without await to reduce delay
                ExecuteSelected();
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
        foreach (var result in _viewModel.Results)
        {
            if (result.Type == SearchResultType.CustomCommand)
            {
                matchedKeyword = result.Title;
                break;
            }
        }

        if (matchedKeyword != null)
        {
            EnterParamMode(matchedKeyword);
            return;
        }

        // Normal tab behavior - select next item
        _viewModel.SelectNextCommand.Execute(null);
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

        // Language submenu
        var langItem = new MenuItem { Header = LocalizationService.Get("TrayLanguage") };
        var zhItem = new MenuItem { Header = LocalizationService.Get("TrayChinese"), IsChecked = LocalizationService.CurrentLanguage == "zh-CN" };
        zhItem.Click += (s, e) => { LocalizationService.CurrentLanguage = "zh-CN"; RefreshLocalization(); _trayService?.Initialize(); };
        var enItem = new MenuItem { Header = LocalizationService.Get("TrayEnglish"), IsChecked = LocalizationService.CurrentLanguage == "en-US" };
        enItem.Click += (s, e) => { LocalizationService.CurrentLanguage = "en-US"; RefreshLocalization(); _trayService?.Initialize(); };
        langItem.Items.Add(zhItem);
        langItem.Items.Add(enItem);
        SearchIconMenu.Items.Add(langItem);

        SearchIconMenu.Items.Add(new Separator());

        var aboutItem = new MenuItem { Header = LocalizationService.Get("TrayAbout") };
        aboutItem.Click += (s, e) => ToastService.Instance.ShowInfo($"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
        SearchIconMenu.Items.Add(aboutItem);

        var exitItem = new MenuItem { Header = LocalizationService.Get("TrayExit") };
        exitItem.Click += (s, e) => { _trayService?.Dispose(); System.Windows.Application.Current.Shutdown(); };
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
        if (_viewModel.SelectedResult == null) return;

        // Fire and forget - execute in background
        Task.Run(async () =>
        {
            bool success;
            if (_viewModel.IsParamMode && _viewModel.SelectedResult.Type == SearchResultType.CustomCommand)
            {
                success = await _viewModel.SearchEngine.ExecuteCustomCommandAsync(_viewModel.SelectedResult, _viewModel.CommandParam);
            }
            else
            {
                success = await _viewModel.SearchEngine.ExecuteResultAsync(_viewModel.SelectedResult);
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
        await _viewModel.ExecuteSelectedCommand.ExecuteAsync(null);
        if (string.IsNullOrEmpty(_viewModel.SearchText))
        {
            HideWindow();
        }
    }
}
