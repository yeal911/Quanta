// ============================================================================
// 文件名: LoggerService.cs
// 文件用途: <see cref="Quanta.Core.Interfaces.IAppLogger"/> 的默认实现，
//          负责将日志按月份写入运行目录下 logs 文件夹的日志文件。
//          DI 容器注册本类的 Default 共享实例；静态门面 Logger 在容器
//          配置后委托容器解析，两者始终指向同一实例。
// ============================================================================

using System;
using System.IO;
using Quanta.Core.Interfaces;

namespace Quanta.Infrastructure.Logging;

/// <summary>
/// 日志服务实现类，负责将应用程序运行时的日志信息写入本地日志文件。
/// 日志文件按月份命名，存储在运行目录下的 logs 目录下。
/// 所有写入操作通过锁机制保证线程安全。
/// </summary>
public sealed class LoggerService : Quanta.Core.Interfaces.IAppLogger
{
    /// <summary>进程级共享实例：DI 注册与静态门面回退共用，保证单实例语义</summary>
    private static LoggerService? _default;

    /// <summary>获取进程级共享实例（懒加载）。DI 容器注册的即此实例。</summary>
    public static LoggerService Default => _default ??= new LoggerService();

    /// <summary>
    /// 日志文件所在的目录路径
    /// </summary>
    private readonly string _logDirectory;

    /// <summary>日志目录绝对路径（对 Quanta.Tests 可见，供单元测试创建/校验日志文件）</summary>
    internal string LogDirectory => _logDirectory;

    /// <summary>
    /// 用于保证多线程写入日志时线程安全的锁对象
    /// </summary>
    private readonly object _lockObj = new();

    /// <summary>
    /// 上一次写入日志的月份，用于检测月份变化
    /// </summary>
    private int _lastLogMonth = 0;

    /// <summary>
    /// 当前日志文件路径（按月份）
    /// </summary>
    private string _currentLogFilePath;

    /// <summary>
    /// 获取当前日志文件路径（按月份）
    /// </summary>
    private string LogFilePath
    {
        get
        {
            int currentMonth = DateTime.Now.Year * 12 + DateTime.Now.Month;

            // 如果月份变了，重新计算路径
            if (_currentLogFilePath == null || _lastLogMonth != currentMonth)
            {
                _lastLogMonth = currentMonth;
                _currentLogFilePath = Path.Combine(_logDirectory, $"quanta_{DateTime.Now:yyyyMM}.log");
            }

            return _currentLogFilePath;
        }
    }

    /// <summary>
    /// 构造函数，初始化日志目录。
    /// 如果日志目录不存在则自动创建。
    /// 日志文件存储在 exe 运行的目录下。
    /// </summary>
    private LoggerService()
    {
        // 获取 exe 所在的目录
        // 单文件发布时，需要获取实际 exe 所在目录，而不是临时解压目录
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;

        // 尝试获取实际 exe 路径（单文件发布时更准确）
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            exeDir = Path.GetDirectoryName(processPath) ?? exeDir;
        }

        _logDirectory = Path.Combine(exeDir, "logs");

        try
        {
            Directory.CreateDirectory(_logDirectory);
        }
        catch
        {
            // 如果创建失败，回退到临时目录
            _logDirectory = Path.Combine(Path.GetTempPath(), "QuantaLogs");
            Directory.CreateDirectory(_logDirectory);
        }

        // 初始化当前月份
        _lastLogMonth = DateTime.Now.Year * 12 + DateTime.Now.Month;
        _currentLogFilePath = Path.Combine(_logDirectory, $"quanta_{DateTime.Now:yyyyMM}.log");

        // 调试：输出实际路径
        try
        {
            File.AppendAllText(_currentLogFilePath, $"[INFO] Logger initialized. BaseDir={exeDir}, LogDir={_logDirectory}, ProcessPath={Environment.ProcessPath}{Environment.NewLine}");
        }
        catch { }
    }

    /// <summary>
    /// 启动时清理过期日志：删除日志目录内最后写入时间早于
    /// <paramref name="retentionMonths"/> 个月前的 quanta_*.log 文件。
    /// 只匹配本应用日志目录内的 <c>quanta_*.log</c> 模式，绝不越界删除；
    /// 清理动作（删除了哪些文件）会写一条 INFO 日志留痕。
    /// </summary>
    /// <param name="retentionMonths">日志保留月数，小于等于 0 表示不清理</param>
    /// <returns>实际删除的文件数</returns>
    public int CleanupExpiredLogs(int retentionMonths)
    {
        if (retentionMonths <= 0) return 0;

        var cutoff = DateTime.Now.AddMonths(-retentionMonths);
        var deleted = new List<string>();

        try
        {
            lock (_lockObj)
            {
                foreach (var file in Directory.EnumerateFiles(_logDirectory, "quanta_*.log"))
                {
                    try
                    {
                        if (File.GetLastWriteTime(file) < cutoff)
                        {
                            File.Delete(file);
                            deleted.Add(Path.GetFileName(file));
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        // 单个文件被占用或无权限时跳过，不影响其余文件清理
                        WriteLog($"[LogCleanup] Skip locked file '{Path.GetFileName(file)}': {ex.Message}", "WARN");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            WriteLog($"[LogCleanup] Failed to enumerate log directory: {ex.Message}", "WARN");
            return 0;
        }

        if (deleted.Count > 0)
        {
            WriteLog($"[LogCleanup] Deleted {deleted.Count} expired log file(s) older than {retentionMonths} months: {string.Join(", ", deleted)}", "INFO");
        }

        return deleted.Count;
    }

    /// <summary>
    /// 内部写入方法，始终执行
    /// </summary>
    private void WriteLog(string message, string level)
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            lock (_lockObj)
            {
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
        }
        catch
        {
            // 日志写入失败时静默忽略，避免因日志异常导致应用崩溃
        }
    }

    /// <summary>
    /// 记录一条日志信息到日志文件（仅 Debug 模式）。
    /// 静态门面上的 [Conditional("DEBUG")] 在 Release 下会擦除调用点；
    /// 此处以 #if DEBUG 保证经由接口注入的调用在 Release 下同样不落盘。
    /// </summary>
    /// <param name="message">日志消息内容</param>
    /// <param name="level">日志级别，默认为 "INFO"</param>
    public void Log(string message, string level = "INFO")
    {
#if DEBUG
        WriteLog(message, level);
#endif
    }

    /// <summary>
    /// 记录一条错误级别的日志，可附带异常信息（始终记录）。
    /// </summary>
    /// <param name="message">错误描述信息</param>
    /// <param name="ex">可选的异常对象，用于记录详细的异常信息</param>
    public void Error(string message, Exception? ex = null)
    {
        var msg = ex != null ? $"{message}: {ex.Message}\n{ex.StackTrace}" : message;
        WriteLog(msg, "ERROR");
    }

    /// <summary>
    /// 记录一条警告级别的日志（始终记录）。
    /// </summary>
    /// <param name="message">警告消息内容</param>
    public void Warn(string message)
    {
        WriteLog(message, "WARN");
    }

    /// <summary>
    /// 记录一条调试级别的日志（仅 Debug 模式）。
    /// 静态门面上的 [Conditional("DEBUG")] 在 Release 下会擦除调用点；
    /// 此处以 #if DEBUG 保证经由接口注入的调用在 Release 下同样不落盘。
    /// </summary>
    /// <param name="message">调试消息内容</param>
    public void Debug(string message)
    {
#if DEBUG
        WriteLog(message, "DEBUG");
#endif
    }
}
