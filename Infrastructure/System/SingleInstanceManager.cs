// ============================================================================
// 文件名：SingleInstanceManager.cs
// 文件用途：单实例管理器。通过命名 Mutex 保证应用只有一个实例运行，
//          并通过命名管道（NamedPipeServerStream）实现跨进程通知：
//          第二实例启动时向首实例发送"显示主窗口"消息后退出。
//          解决主窗口隐藏在托盘时 Process.MainWindowHandle 为 IntPtr.Zero、
//          第二实例无法唤醒首实例的问题。
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Quanta.Services;

/// <summary>
/// 单实例管理器：命名 Mutex 负责互斥检测，命名管道负责唤醒通知。
/// 首实例持有 Mutex 并监听管道；第二实例通过管道通知首实例显示主窗口后退出。
/// 随 Dispose 释放 Mutex 与管道监听，不残留系统句柄。
/// </summary>
public sealed class SingleInstanceManager : IDisposable
{
    /// <summary>单实例互斥锁名称</summary>
    private const string MutexName = "Quanta_SingleInstance_Mutex";

    /// <summary>跨进程通知管道名称</summary>
    private const string PipeName = "Quanta_SingleInstance_Pipe";

    /// <summary>第二实例 → 首实例的激活命令</summary>
    private const string ShowCommand = "SHOW";

    /// <summary>首实例 → 第二实例的进程 ID 回执前缀（用于前台权限转交）</summary>
    private const string PidPrefix = "PID:";

    /// <summary>第二实例连接首实例管道的总超时（毫秒），覆盖首实例启动中的竞态窗口</summary>
    private const int ConnectTimeoutMs = 2000;

    /// <summary>首实例等待第二实例发送命令的超时（毫秒），防止恶意连接卡死监听循环</summary>
    private const int ReadTimeoutMs = 5000;

    /// <summary>命名互斥锁，null 表示本实例不是首个实例或已释放</summary>
    private Mutex? _mutex;

    /// <summary>管道监听循环的取消令牌</summary>
    private CancellationTokenSource? _listenerCts;

    /// <summary>管道监听后台任务</summary>
    private Task? _listenerTask;

    /// <summary>标记对象是否已被释放</summary>
    private bool _disposed;

    /// <summary>Win32 API：允许指定进程抢占前台窗口（前台锁处理）</summary>
    [DllImport("user32.dll")] private static extern bool AllowSetForegroundWindow(int dwProcessId);

    /// <summary>Win32 API：将指定窗口设置为前台窗口（旧路径兜底用）</summary>
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>Win32 API：设置指定窗口的显示状态（旧路径兜底用）</summary>
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>ShowWindow 命令常量：恢复窗口（从最小化状态还原）</summary>
    private const int SW_RESTORE = 9;

    /// <summary>
    /// 尝试获取单实例互斥锁。
    /// </summary>
    /// <returns>如果是首个实例返回 true；已有实例运行返回 false</returns>
    public bool TryAcquireExclusiveLock()
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        return createdNew;
    }

    /// <summary>
    /// 启动管道监听：首实例在此接收第二实例的"显示主窗口"通知。
    /// onActivated 在后台线程触发，调用方负责切换到 UI 线程执行窗口操作。
    /// </summary>
    /// <param name="onActivated">收到激活命令时触发的回调</param>
    public void StartActivationListener(Action onActivated)
    {
        if (_listenerCts != null) return;

        _listenerCts = new CancellationTokenSource();
        var token = _listenerCts.Token;

        _listenerTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                NamedPipeServerStream? server = null;
                try
                {
                    server = new NamedPipeServerStream(PipeName, PipeDirection.InOut,
                        1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly);
                    await server.WaitForConnectionAsync(token);

                    using var reader = new StreamReader(server, Encoding.UTF8, false, 1024, true);
                    using var writer = new StreamWriter(server, Encoding.UTF8, 1024, true)
                    {
                        AutoFlush = true
                    };

                    // 回传本进程 ID：第二实例据此调用 AllowSetForegroundWindow
                    // 把前台权限转交给本实例，避免 ShowWindow/Activate 被前台锁拦截
                    await writer.WriteLineAsync(PidPrefix + Environment.ProcessId);

                    var command = await ReadLineWithTimeoutAsync(reader, token);
                    if (string.Equals(command, ShowCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Debug("[SingleInstance] Activation request received from second instance");
                        onActivated();
                    }

                    server.Disconnect();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // 单个连接异常不终止监听循环
                    Logger.Warn($"[SingleInstance] Pipe listener error: {ex.Message}");
                    try { await Task.Delay(200, token); }
                    catch (OperationCanceledException) { break; }
                }
                finally
                {
                    server?.Dispose();
                }
            }
        }, token);
    }

    /// <summary>
    /// 第二实例调用：通知首实例显示主窗口。
    /// 首实例在连接建立后先回传自己的进程 ID，本实例刚由用户启动、
    /// 持有前台激活权限，随即通过 AllowSetForegroundWindow 转交给首实例，
    /// 确保首实例的窗口能真正获得焦点。
    /// </summary>
    public void SignalFirstInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut,
                PipeOptions.CurrentUserOnly);
            client.Connect(ConnectTimeoutMs);

            using var reader = new StreamReader(client, Encoding.UTF8, false, 1024, true);
            using var writer = new StreamWriter(client, Encoding.UTF8, 1024, true)
            {
                AutoFlush = true
            };

            var greeting = reader.ReadLine();
            if (greeting != null && greeting.StartsWith(PidPrefix, StringComparison.Ordinal)
                && int.TryParse(greeting.Substring(PidPrefix.Length), out var pid) && pid > 0)
            {
                AllowSetForegroundWindow(pid);
            }

            writer.WriteLine(ShowCommand);
        }
        catch (Exception ex)
        {
            Logger.Warn($"[SingleInstance] Failed to signal first instance: {ex.Message}");
            // 兜底：首实例无管道监听（如旧版本仍在运行）时退回 Win32 句柄激活。
            // 窗口隐藏时 MainWindowHandle 为零、此路径无效，但可见窗口仍可被置前。
            TryLegacyWindowActivation();
        }
    }

    /// <summary>
    /// 释放单实例资源：停止管道监听、释放命名 Mutex。
    /// 命名管道与 Mutex 均为内核对象，进程退出即销毁，此处主动释放确保不残留。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _listenerCts?.Cancel();
            _listenerTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // 监听任务收尾异常可忽略
        }
        catch (Exception)
        {
            // 任务尚未启动或已结束
        }
        finally
        {
            _listenerCts?.Dispose();
            _listenerCts = null;
            _listenerTask = null;
        }

        if (_mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // 非拥有线程/已释放时忽略
            }
            finally
            {
                _mutex.Dispose();
                _mutex = null;
            }
        }
    }

    /// <summary>
    /// 带超时地读取一行命令：超时返回 null，让监听循环断开当前连接继续服务。
    /// </summary>
    private static async Task<string?> ReadLineWithTimeoutAsync(StreamReader reader, CancellationToken token)
    {
        Task<string?> readTask = reader.ReadLineAsync(token).AsTask();
        var completed = await Task.WhenAny(readTask, Task.Delay(ReadTimeoutMs, token)) == readTask;
        token.ThrowIfCancellationRequested();
        return completed ? await readTask : null;
    }

    /// <summary>
    /// 旧路径兜底：通过进程枚举找到首实例窗口句柄并置前。
    /// </summary>
    private static void TryLegacyWindowActivation()
    {
        try
        {
            var current = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(current.ProcessName))
            {
                if (process.Id == current.Id) continue;
                var handle = process.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    ShowWindow(handle, SW_RESTORE);
                    SetForegroundWindow(handle);
                }
                break;
            }
        }
        catch
        {
            // 兜底路径失败时静默退出，第二实例照常结束
        }
    }
}
