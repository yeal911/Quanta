// ============================================================================
// 文件名：MainWindow.xaml.cs
// 文件用途：主窗口核心部分：字段声明、DI 构造、窗口初始化事件。
// ============================================================================
// 文件结构（partial class 拆分）：
//   MainWindow.xaml.cs          ← 字段、构造、Loaded、SearchBox事件
//   MainWindow.Window.cs        ← 窗口显示/隐藏动画、ToggleVisibility
//   MainWindow.Keyboard.cs      ← 键盘处理、结果执行、列表交互、设置窗口
//   MainWindow.ParamMode.cs     ← 参数模式 UI 切换（Tab、record 模式）
//   MainWindow.Recording.cs     ← 录音 UI 流程（启动/配置/关闭）
//   MainWindow.UI.cs            ← 主题/本地化/颜色复制事件
// ============================================================================

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Quanta.Helpers;
using Quanta.Services;
using Quanta.ViewModels;
using Quanta.Interfaces;
using Quanta.Core.Interfaces;

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

    /// <summary>录音服务实例（DI 注入）</summary>
    private readonly IRecordingService _recordingService;

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
        ClipboardMonitor clipboardMonitor,
        IRecordingService recordingService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _hotkeyManager = hotkeyManager;
        _clipboardMonitor = clipboardMonitor;
        _recordingService = recordingService;

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
        Logger.Debug($"[Hotkey] Config loaded: Modifier={config.Hotkey?.Modifier}, Key={config.Hotkey?.Key}");

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
            if (_recordingService != null && _recordingService.State != RecordingState.Idle)
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
}
