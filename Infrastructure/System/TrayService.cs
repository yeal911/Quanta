// =============================================================================
// 文件名: TrayService.cs
// 用途:   系统托盘（通知区域）服务，负责创建和管理 Windows 系统托盘图标、
//         右键上下文菜单以及相关的用户交互操作（显示窗口、设置、语言切换、
//         关于信息、退出应用等）。
// =============================================================================

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Quanta.Core.Interfaces;
using Quanta.Helpers;
using Quanta.Interfaces;

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
public class TrayService : ITrayService
{
    /// <summary>
    /// 主窗口服务接口，用于在托盘操作中显示、激活或刷新主窗口。
    /// </summary>
    private readonly IMainWindowService _mainWindowService;

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
    /// <param name="mainWindowService">主窗口服务接口，用于托盘交互时操作主窗口。</param>
    public TrayService(IMainWindowService mainWindowService)
    {
        _mainWindowService = mainWindowService;
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
            Text = LocalizationService.Get("TrayTooltip"),
        };

        // 构建右键上下文菜单
        BuildContextMenu();

        // 双击托盘图标时显示主窗口
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

        Logger.Debug("TrayService initialized");
    }

    /// <summary>
    /// 构建系统托盘图标的右键上下文菜单。
    /// 菜单项包括：设置、语言切换（动态生成）、关于、退出。
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

        // "语言"子菜单 - 动态生成所有支持的语言
        var langMenu = new ToolStripMenuItem(LocalizationService.Get("TrayLanguage"));

        foreach (var lang in LocalizationService.GetSupportedLanguages())
        {
            var langItem = new ToolStripMenuItem(LocalizationService.Get(LocalizationService.GetLanguageDisplayKey(lang.Code)));
            langItem.Click += (s, e) => SetLanguage(lang.Code);
            langItem.Checked = LocalizationService.CurrentLanguage == lang.Code;
            langMenu.DropDownItems.Add(langItem);
        }

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

        // 通过接口刷新主窗口的本地化内容
        _mainWindowService.RefreshLocalization();
    }

    /// <summary>
    /// 加载应用程序图标，优先从程序目录加载 quanta.ico 文件。
    /// 如果文件不存在或加载失败，则使用代码动态绘制一个备用图标。
    /// </summary>
    /// <returns>加载成功的 <see cref="Icon"/> 实例。</returns>
    private Icon LoadAppIcon()
    {
        var baseDir = AppContext.BaseDirectory;

        // 尝试多个可能的路径
        string[] tryPaths = new[]
        {
            // 方案1: Resources/imgs 目录 (非单文件)
            Path.Combine(baseDir, "Resources", "imgs", "quanta.ico"),
            // 方案2: 程序目录 (单文件发布时资源解压到这里)
            Path.Combine(baseDir, "quanta.ico"),
            // 方案3: Resources 目录
            Path.Combine(baseDir, "Resources", "quanta.ico"),
        };

        foreach (var icoPath in tryPaths)
        {
            try
            {
                if (File.Exists(icoPath))
                {
                    Logger.Debug($"[TrayService] Loading icon from: {icoPath}");
                    return new Icon(icoPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to load icon from {icoPath}: {ex.Message}");
            }
        }

        // 备用方案：动态绘制简易图标
        Logger.Warn("[TrayService] Icon not found, using fallback");
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
        // 先设置时间戳，防止 Window_Deactivated 在动画期间误触发隐藏
        _mainWindowService.LastShownFromTray = DateTime.Now;
        // 使用接口显示主窗口，确保 Opacity / _isVisible / 动画全部正确初始化
        _mainWindowService.ShowWindow();
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

        Logger.Debug("TrayService disposed");
    }
}
