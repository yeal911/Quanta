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
using Quanta.Services;
using Quanta.Helpers;
using Quanta.ViewModels;
using Quanta.Views;

namespace Quanta;

/// <summary>
/// 应用程序类，继承自 WPF Application。
/// 在启动时检查并确保只有一个 Quanta 实例运行。
/// 如果检测到已有实例，则激活已有窗口并退出当前进程。
/// </summary>
public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// 应用启动事件处理。检查单实例约束，建立 DI 组合根，创建主窗口。
    /// </summary>
    /// <param name="e">启动事件参数</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (!EnsureSingleInstance()) { Current.Shutdown(); return; }
        ApplyStartWithWindows();

        _serviceProvider = BuildServiceProvider();
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show(); // MainWindow_Loaded 内部会调用 Hide()
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
                Logger.Log("[App] StartWithWindows enabled - registry key set");
            }
            else
            {
                if (key.GetValue(appName) != null)
                {
                    key.DeleteValue(appName, false);
                    Logger.Log("[App] StartWithWindows disabled - registry key removed");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[App] Failed to apply StartWithWindows: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 确保应用程序单实例运行。
    /// 使用命名互斥锁（Mutex）检测是否已有实例。
    /// 如果已有实例运行，尝试将其主窗口置前并恢复显示。
    /// </summary>
    /// <returns>如果是首个实例返回 true，否则返回 false</returns>
    private bool EnsureSingleInstance()
    {
        string mutexName = "Quanta_SingleInstance_Mutex";
        var mutex = new System.Threading.Mutex(true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            var currentProcess = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {
                    var handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero) { SetForegroundWindow(handle); ShowWindow(handle, SW_RESTORE); }
                    break;
                }
            }
            return false;
        }
        return true;
    }

    /// <summary>Win32 API：将指定窗口设置为前台窗口</summary>
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>Win32 API：设置指定窗口的显示状态</summary>
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>ShowWindow 命令常量：恢复窗口（从最小化状态还原）</summary>
    private const int SW_RESTORE = 9;
}
