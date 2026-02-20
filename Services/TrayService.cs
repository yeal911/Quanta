// =============================================================================
// 文件名: TrayService.cs
// 用途:   系统托盘（通知区域）服务，负责创建和管理 Windows 系统托盘图标、
//         右键上下文菜单以及相关的用户交互操作（显示窗口、设置、语言切换、
//         关于信息、退出应用等）。
// =============================================================================

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Forms;
using Quanta.Helpers;

namespace Quanta.Services;

/// <summary>
/// 系统托盘服务类，用于管理应用程序在 Windows 通知区域（系统托盘）中的图标和交互。
/// <para>主要功能包括：</para>
/// <list type="bullet">
///   <item>创建和显示系统托盘图标</item>
///   <item>构建右键上下文菜单（显示、设置、语言切换、关于、退出）</item>
///   <item>支持多语言切换并实时刷新菜单文字</item>
///   <item>双击托盘图标显示主窗口</item>
/// </list>
/// <para>实现了 <see cref="IDisposable"/> 接口，确保托盘图标资源的正确释放。</para>
/// </summary>
public class TrayService : IDisposable
{
    /// <summary>
    /// 主窗口引用，用于在托盘操作中显示、激活或刷新主窗口。
    /// </summary>
    private readonly Window _mainWindow;

    /// <summary>
    /// 系统托盘通知图标实例，为 null 表示尚未初始化或已释放。
    /// </summary>
    private NotifyIcon? _notifyIcon;

    /// <summary>
    /// 标记当前对象是否已被释放，防止重复释放资源。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 当用户在托盘菜单中点击"设置"选项时触发的事件。
    /// </summary>
    public event EventHandler? SettingsRequested;

    /// <summary>
    /// 当用户在托盘菜单中点击"退出"选项时触发的事件。
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// 退出前检查回调。返回 false 则取消退出（例如录音进行中）。
    /// </summary>
    public Func<bool>? CanExit { get; set; }

    /// <summary>
    /// 初始化 <see cref="TrayService"/> 类的新实例。
    /// </summary>
    /// <param name="mainWindow">应用程序主窗口的引用，用于托盘交互时操作主窗口。</param>
    public TrayService(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    /// <summary>
    /// 初始化系统托盘图标和上下文菜单。
    /// 每次调用都会重建菜单（用于语言切换时刷新菜单文字）。
    /// 如果托盘图标已存在，会先销毁旧实例再重新创建。
    /// </summary>
    public void Initialize()
    {
        // 如果托盘图标已存在，先销毁再重建（用于语言切换等场景）
        if (_notifyIcon != null)
        {
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        // 从配置加载当前语言设置
        LocalizationService.LoadFromConfig();

        // 加载应用程序图标（优先从程序目录加载 quanta.ico）
        Icon icon = LoadAppIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = "Quanta - Launcher",
        };

        // 构建右键上下文菜单
        BuildContextMenu();

        // 双击托盘图标时显示主窗口
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

        DebugLog.Log("TrayService initialized");
    }

    /// <summary>
    /// 构建系统托盘图标的右键上下文菜单。
    /// 菜单项包括：设置、语言切换（中文/英文）、关于、退出。
    /// 所有菜单项文字均通过 <see cref="LocalizationService"/> 获取本地化字符串。
    /// </summary>
    private void BuildContextMenu()
    {
        if (_notifyIcon == null) return;

        var menu = new ContextMenuStrip();

        // "设置"菜单项 - 点击后触发设置事件
        var settingsItem = new ToolStripMenuItem(LocalizationService.Get("TraySettings"));
        settingsItem.Click += (s, e) => OnSettingsRequested();
        menu.Items.Add(settingsItem);

        // "语言"子菜单 - 包含中文和英文两个选项
        var langMenu = new ToolStripMenuItem(LocalizationService.Get("TrayLanguage"));

        // 中文选项，当前为中文时显示勾选状态
        var zhItem = new ToolStripMenuItem(LocalizationService.Get("TrayChinese"));
        zhItem.Click += (s, e) => SetLanguage("zh-CN");
        zhItem.Checked = LocalizationService.CurrentLanguage == "zh-CN";
        langMenu.DropDownItems.Add(zhItem);

        // 英文选项，当前为英文时显示勾选状态
        var enItem = new ToolStripMenuItem(LocalizationService.Get("TrayEnglish"));
        enItem.Click += (s, e) => SetLanguage("en-US");
        enItem.Checked = LocalizationService.CurrentLanguage == "en-US";
        langMenu.DropDownItems.Add(enItem);

        menu.Items.Add(langMenu);

        // 分隔线
        menu.Items.Add(new ToolStripSeparator());

        // "关于"菜单项 - 点击后显示关于信息
        var aboutItem = new ToolStripMenuItem(LocalizationService.Get("TrayAbout"));
        aboutItem.Click += (s, e) => ShowAbout();
        menu.Items.Add(aboutItem);

        // "退出"菜单项 - 点击后退出应用程序
        var exitItem = new ToolStripMenuItem(LocalizationService.Get("TrayExit"));
        exitItem.Click += (s, e) => OnExitRequested();
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = menu;
    }

    /// <summary>
    /// 切换应用程序的界面语言，并立即刷新托盘菜单和主窗口的本地化文字。
    /// </summary>
    /// <param name="lang">目标语言代码，例如 "zh-CN"（中文）或 "en-US"（英文）。</param>
    private void SetLanguage(string lang)
    {
        LocalizationService.CurrentLanguage = lang;
        // 重建右键菜单以更新语言文字
        BuildContextMenu();

        // 在 UI 线程上刷新主窗口的本地化内容
        _mainWindow.Dispatcher.Invoke(() =>
        {
            if (_mainWindow is Quanta.Views.MainWindow mainWin)
            {
                mainWin.RefreshLocalization();
            }
        });
    }

    /// <summary>
    /// 加载应用程序图标，优先从程序目录加载 quanta.ico 文件。
    /// 如果文件不存在或加载失败，则使用代码动态绘制一个备用图标。
    /// </summary>
    /// <returns>加载成功的 <see cref="Icon"/> 实例。</returns>
    private Icon LoadAppIcon()
    {
        try
        {
            // 优先从程序运行目录的 Resources/img 子目录加载 quanta.ico
            var icoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "imgs", "quanta.ico");
            if (System.IO.File.Exists(icoPath))
            {
                return new Icon(icoPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to load quanta.ico: {ex.Message}");
        }

        // 备用方案：动态绘制简易图标
        return CreateFallbackIcon();
    }

    /// <summary>
    /// 创建备用图标：当 quanta.ico 文件不存在时，动态绘制一个 16x16 像素的简易 Q 形图标。
    /// 图标使用蓝色（RGB: 0, 120, 212）绘制一个圆环和中心圆点。
    /// </summary>
    /// <returns>动态生成的备用 <see cref="Icon"/> 实例。</returns>
    private Icon CreateFallbackIcon()
    {
        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var brush = new SolidBrush(Color.FromArgb(0, 120, 212));
        using var pen = new Pen(brush, 2);

        // 绘制外圈圆环
        g.DrawEllipse(pen, 2, 2, 12, 12);
        // 绘制中心实心圆点
        g.FillEllipse(brush, 6, 6, 4, 4);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>
    /// 显示并激活主窗口。如果主窗口处于最小化状态，先恢复为正常状态，
    /// 然后显示窗口、激活窗口并设置焦点。
    /// 所有操作通过 Dispatcher 在 UI 线程上执行。
    /// </summary>
    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;

        _mainWindow.Dispatcher.Invoke(() =>
        {
            if (_mainWindow is Quanta.Views.MainWindow mainWin)
            {
                // 先设置时间戳，防止 Window_Deactivated 在动画期间误触发隐藏
                mainWin.LastShownFromTray = DateTime.Now;
                // 使用 MainWindow 自己的 ShowWindow()，确保 Opacity / _isVisible / 动画全部正确初始化
                mainWin.ShowWindow();
            }
            else
            {
                if (_mainWindow.WindowState == WindowState.Minimized)
                    _mainWindow.WindowState = WindowState.Normal;

                _mainWindow.Show();
                _mainWindow.Activate();
                _mainWindow.Focus();
            }
        });
    }

    /// <summary>
    /// 处理"设置"菜单项的点击事件。
    /// 先显示主窗口，然后触发 <see cref="SettingsRequested"/> 事件通知订阅者。
    /// </summary>
    private void OnSettingsRequested()
    {
        ShowMainWindow();
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 显示"关于"信息，通过 Toast 通知的方式展示作者和联系邮箱。
    /// 通知持续 3 秒后自动消失。
    /// </summary>
    private void ShowAbout()
    {
        // 通过 Toast 通知显示关于信息
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ToastService.Instance.ShowInfo($"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
        });
    }

    /// <summary>
    /// 处理"退出"菜单项的点击事件。
    /// 先检查 <see cref="CanExit"/> 回调，允许退出后才依次触发事件、释放资源并关闭应用。
    /// </summary>
    private void OnExitRequested()
    {
        // 检查是否允许退出（录音进行中返回 false 则取消）
        if (CanExit != null && !CanExit())
            return;

        ExitRequested?.Invoke(this, EventArgs.Empty);
        Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// 释放系统托盘服务占用的资源，包括隐藏和销毁托盘图标。
    /// 该方法可以安全地多次调用，内部通过 <see cref="_disposed"/> 标志防止重复释放。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        DebugLog.Log("TrayService disposed");
    }
}
