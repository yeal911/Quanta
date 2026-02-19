// ============================================================================
// 文件名: DebugLog.cs
// 文件描述: 调试日志帮助类，仅在 Debug 编译模式下输出日志
// ============================================================================

using System.Diagnostics;

namespace Quanta.Services;

/// <summary>
/// 调试日志帮助类
/// 使用 [Conditional("DEBUG")] 确保该方法仅在 Debug 模式下编译和执行
/// Release 编译时所有调用会被完全移除，无任何性能影响
/// </summary>
public static class DebugLog
{
    [Conditional("DEBUG")]
    public static void Log(string message)
    {
        Logger.Log(message, "DEBUG");
    }

    [Conditional("DEBUG")]
    public static void Log(string format, params object[] args)
    {
        Logger.Log(string.Format(format, args), "DEBUG");
    }

    [Conditional("DEBUG")]
    public static void Info(string message)
    {
        Logger.Log(message, "INFO");
    }

    [Conditional("DEBUG")]
    public static void Info(string format, params object[] args)
    {
        Logger.Log(string.Format(format, args), "INFO");
    }
}
