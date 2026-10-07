// ============================================================================
// 文件名：App.xaml.cs
// 文件用途：WPF 应用程序入口类，负责应用启动时的初始化工作。
//          实现单实例运行机制，防止同一时间运行多个 Quanta 实例。
//          使用 Microsoft.Extensions.DependencyInjection 实现依赖注入。
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Quanta.Interfaces;
using Quanta.Core.Interfaces;
using Quanta.Services;
using Quanta.Helpers;
using Quanta.ViewModels;
using Quanta.Views;

namespace Quanta;

/// <summary>
/// 应用程序类，继承自 WPF Application。
/// 在启动时检查并确保只有一个 Quanta 实例运行。
/// 如果检测到已有实例，则通过命名管道通知其显示主窗口并退出当前进程。
/// </summary>
public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;
    private SingleInstanceManager? _singleInstance;

    /// <summary>
    /// 应用启动事件处理。检查单实例约束，建立 DI 组合根，创建主窗口。
    /// </summary>
    /// <param name="e">启动事件参数</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstance = new SingleInstanceManager();
        if (!_singleInstance.TryAcquireExclusiveLock())
        {
            // 已有实例：通知其显示主窗口后释放资源并退出
            _singleInstance.SignalFirstInstance();
            _singleInstance.Dispose();
            Current.Shutdown();
            return;
        }
        ApplyStartWithWindows();

        _serviceProvider = BuildServiceProvider();
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show(); // MainWindow_Loaded 内部会调用 Hide()

        // 首实例监听第二实例的激活通知，在 UI 线程复用既有显示路径弹出主窗口
        _singleInstance.StartActivationListener(OnSingleInstanceActivated);
    }

    /// <summary>
    /// 收到第二实例的激活通知：在 UI 线程复用 MainWindow.Window.cs 的
    /// ShowWindow 显示路径弹出主窗口。第二实例已通过 AllowSetForegroundWindow
    /// 转交前台权限，Activate 不受前台锁限制。
    /// </summary>
    private void OnSingleInstanceActivated()
    {
        Current.Dispatcher.BeginInvoke(() =>
        {
            var mainWindow = _serviceProvider?.GetRequiredService<MainWindow>();
            if (mainWindow == null) return;
            // 与托盘显示路径一致：记录时间戳，防止激活动画期间 Deactivated 误触发隐藏
            mainWindow.LastShownFromTray = DateTime.Now;
            mainWindow.ShowWindow();
        });
    }

    /// <summary>
    /// 构建 DI 服务容器，注册所有应用服务。
    /// App.xaml.cs 是唯一的组合根，所有依赖关系在此声明。
    /// </summary>
    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // ── 基础设施（接口 → 包装实现，保留原静态类向后兼容） ──
        services.AddSingleton<IAppLogger, LoggerService>();
        services.AddSingleton<IConfigLoader, ConfigLoaderService>();
        services.AddSingleton<ILocalizationService, LocalizationServiceWrapper>();
        services.AddSingleton<IThemeService, ThemeServiceWrapper>();

        // ── 搜索提供者 ──
        services.AddSingleton<FileSearchProvider>();

        // ── 领域服务 ──
        services.AddSingleton<UsageTracker>();
        services.AddSingleton<CommandRouter>();
        services.AddSingleton<SearchEngine>();
        services.AddSingleton<HotkeyManager>();
        services.AddSingleton<ClipboardMonitor>();
        // ── 录音服务 ──
        services.AddSingleton<IRecordingService, RecordingService>();

        // ── 已有单例（通过工厂桥接，不改变其单例语义） ──
        services.AddSingleton(_ => ToastService.Instance);
        services.AddSingleton(_ => ClipboardHistoryService.Instance);

        // ── UI 层 ──
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 根据配置同步开机自启注册表项。
    /// StartWithWindows=true 时写入 Run 注册表键；=false 时移除。
    /// </summary>
    private void ApplyStartWithWindows()
    {
        try
        {
            var config = ConfigLoader.Load();
            var startWithWindows = config.AppSettings?.StartWithWindows ?? false;
            const string appName = "Quanta";
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (startWithWindows)
            {
                key.SetValue(appName, $"\"{exePath}\"");
                Logger.Debug("[App] StartWithWindows enabled - registry key set");
            }
            else
            {
                if (key.GetValue(appName) != null)
                {
                    key.DeleteValue(appName, false);
                    Logger.Debug("[App] StartWithWindows disabled - registry key removed");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[App] Failed to apply StartWithWindows: {ex.Message}", ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 释放单实例资源：停止管道监听、释放命名 Mutex，不残留系统句柄
        _singleInstance?.Dispose();
        _singleInstance = null;

        base.OnExit(e);
    }
}
