// ============================================================================
// 文件名：MainWindow.xaml.cs
// 文件用途：主窗口核心部分：字段声明、DI 构造、窗口初始化事件。
//          业务逻辑已下沉到 MainViewModel（含 Recording 分部），
//          本文件只保留视图接线：事件订阅、窗口生命周期、Win32 互操作桥。
// ============================================================================
// 文件结构（partial class 拆分）：
//   MainWindow.xaml.cs          ← 字段、构造、Loaded、失焦/拖动事件
//   MainWindow.Window.cs        ← 窗口显示/隐藏动画、ToggleVisibility、滚轮转发
//   MainWindow.Keyboard.cs      ← 键盘事件 → ViewModel 命令接线
//   MainWindow.ParamMode.cs     ← 参数模式 UI 切换（Tab、record 模式绑定切换）
//   MainWindow.Recording.cs     ← 录音悬浮窗接线、窗口关闭清理
//   MainWindow.UI.cs            ← 主题图标、应用菜单、本地化刷新、颜色复制接线
// ============================================================================

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Quanta.Core.Interfaces;
using Quanta.Infrastructure.Logging;
using Quanta.Infrastructure.System;
using Quanta.Presentation.Helpers;
using Quanta.Presentation.ViewModels;

namespace Quanta.Views;

/// <summary>
/// 主窗口类，作为 Quanta 启动器的核心 UI 界面。
/// 业务逻辑位于 <see cref="MainViewModel"/>；此处只做视图接线：
/// 事件 → 命令、窗口生命周期、Win32 互操作桥。
/// </summary>
public partial class MainWindow : Window, IMainWindowService
{
    /// <summary>主窗口的视图模型，管理搜索/热键/录音等业务逻辑</summary>
    private readonly MainViewModel _viewModel;

    /// <summary>全局快捷键管理器，用于注销热键（注册编排在 MainViewModel）</summary>
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

    /// <summary>录音服务实例（DI 注入），供录音悬浮窗使用</summary>
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

        // 订阅语言切换事件：无论从托盘、设置窗口还是搜索关键字切换语言，
        // 主窗口都即时刷新本地化文本与布局方向（ar-SA 为 RTL）
        LocalizationService.LanguageChanged += OnLanguageChanged;
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
    /// 窗口加载完成事件处理：订阅 ViewModel 事件、恢复主题、注册全局快捷键、
    /// 初始化系统托盘与剪贴板监听，最后隐藏窗口等待快捷键唤起。
    /// </summary>
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Load language setting
        LocalizationService.LoadFromConfig();

        // ── ViewModel 事件 → 视图动作接线 ──
        _viewModel.HideRequested += (s, args) => Dispatcher.Invoke(() => HideWindow());
        _viewModel.HotkeyPressed += (s, args) => Dispatcher.Invoke(() => ToggleVisibility());
        _viewModel.LocalizationChanged += (s, args) =>
        {
            RefreshLocalization();
            _trayService?.Initialize();
        };
        _viewModel.ExitRequested += (s, args) =>
        {
            _trayService?.Dispose();
            System.Windows.Application.Current.Shutdown();
        };

        // 当 IsParamMode 变为 false 时，还原 SearchBox 绑定（record 参数模式退出时使用）
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsParamMode) && !_viewModel.IsParamMode)
                RestoreSearchBinding();
        };

        _windowHandle = new WindowInteropHelper(this).Handle;

        // 恢复上次保存的主题（Dark/Light）
        _viewModel.ApplyInitialTheme();
        UpdateThemeIcon(_viewModel.IsDarkTheme);

        // 注册全局快捷键（业务在 MainViewModel.InitializeHotkey）
        if (!_viewModel.InitializeHotkey(_windowHandle))
        {
            Dispatcher.BeginInvoke(() =>
                ToastService.Instance.ShowWarning(LocalizationService.Get("HotkeyRegisterFailed")));
        }

        // Initialize ToastService with main window
        ToastService.Instance.SetMainWindow(this);

        // Initialize system tray
        _trayService = new TrayService(this);
        _trayService.SettingsRequested += (s, args) => Dispatcher.Invoke(() => OpenCommandSettings());
        _trayService.ExitRequested += (s, args) => Dispatcher.Invoke(() => _trayService?.Dispose());
        _trayService.CanExit = () =>
        {
            if (_viewModel.IsRecordingActive)
            {
                Dispatcher.Invoke(() =>
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
