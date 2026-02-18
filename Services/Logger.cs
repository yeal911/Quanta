// ============================================================================
// 文件名: Logger.cs
// 文件用途: 提供应用程序全局日志记录服务，支持按日期生成日志文件，
//          包含不同日志级别（INFO、ERROR、WARN、DEBUG）的记录功能。
//          日志文件存储在运行目录下的 logs 文件夹中。
// ============================================================================

using System.IO;

namespace Quanta.Services;

/// <summary>
/// 全局静态日志服务类，负责将应用程序运行时的日志信息写入本地日志文件。
/// 日志文件按日期命名，存储在运行目录下的 logs 目录下。
/// 所有写入操作通过锁机制保证线程安全。
/// </summary>
public static class Logger
{
    /// <summary>
    /// 日志文件所在的目录路径
    /// </summary>
    private static readonly string LogDirectory;

    /// <summary>
    /// 当前日志文件的完整路径（按当天日期命名）
    /// </summary>
    private static readonly string LogFilePath;

    /// <summary>
    /// 用于保证多线程写入日志时线程安全的锁对象
    /// </summary>
    private static readonly object LockObj = new();

    /// <summary>
    /// 静态构造函数，初始化日志目录和日志文件路径。
    /// 如果日志目录不存在则自动创建。
    /// 日志文件存储在 exe 运行的目录下。
    /// </summary>
    static Logger()
    {
        // 获取 exe 运行的目录
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        LogDirectory = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(LogDirectory);
        LogFilePath = Path.Combine(LogDirectory, $"quanta_{DateTime.Now:yyyyMMdd}.log");
    }

    /// <summary>
    /// 记录一条日志信息到日志文件中。
    /// </summary>
    /// <param name="message">日志消息内容</param>
    /// <param name="level">日志级别，默认为 "INFO"，可选值包括 "INFO"、"ERROR"、"WARN"、"DEBUG"</param>
    public static void Log(string message, string level = "INFO")
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            lock (LockObj)
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
    /// 记录一条错误级别的日志，可附带异常信息。
    /// 如果提供了异常对象，会自动将异常消息和堆栈信息一并记录。
    /// </summary>
    /// <param name="message">错误描述信息</param>
    /// <param name="ex">可选的异常对象，用于记录详细的异常信息</param>
    public static void Error(string message, Exception? ex = null)
    {
        var msg = ex != null ? $"{message}: {ex.Message}\n{ex.StackTrace}" : message;
        Log(msg, "ERROR");
    }

    /// <summary>
    /// 记录一条警告级别的日志。
    /// </summary>
    /// <param name="message">警告消息内容</param>
    public static void Warn(string message) => Log(message, "WARN");

    /// <summary>
    /// 记录一条调试级别的日志。
    /// </summary>
    /// <param name="message">调试消息内容</param>
    public static void Debug(string message) => Log(message, "DEBUG");
}
