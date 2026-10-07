// ============================================================================
// 文件名: Logger.cs
// 文件用途: 全局日志静态门面（DI 统一第一步的兼容层）。
//          签名与原静态 Logger 完全一致，现有调用点无需修改；
//          内部委托容器解析的 LoggerService 实例（AppServices.Configure
//          之后），容器可用前回退到 LoggerService.Default，两者为同一实例。
// ============================================================================

using System;
using System.Diagnostics;
using Quanta.Core.DependencyInjection;

namespace Quanta.Services;

/// <summary>
/// 全局日志静态门面。保留原静态 Logger 的全部签名以兼容既有调用点；
/// 实际写入逻辑位于 DI 注册的 <see cref="LoggerService"/> 实例中，
/// 本类只负责把调用转发给容器解析的实例。新代码请改为构造函数注入
/// <see cref="Quanta.Interfaces.IAppLogger"/>。
/// </summary>
public static class Logger
{
    /// <summary>
    /// 当前日志实现：容器配置后从 DI 解析，早期启动阶段回退到共享默认实例。
    /// 两者始终是同一对象，静态路径与注入路径行为完全一致。
    /// </summary>
    private static LoggerService Current
        => AppServices.TryGet<LoggerService>() ?? LoggerService.Default;

    /// <summary>
    /// 记录一条日志信息到日志文件（仅 Debug 模式）。
    /// </summary>
    /// <param name="message">日志消息内容</param>
    /// <param name="level">日志级别，默认为 "INFO"</param>
    [Conditional("DEBUG")]
    public static void Log(string message, string level = "INFO")
    {
        Current.Log(message, level);
    }

    /// <summary>
    /// 记录一条错误级别的日志，可附带异常信息（始终记录）。
    /// </summary>
    /// <param name="message">错误描述信息</param>
    /// <param name="ex">可选的异常对象，用于记录详细的异常信息</param>
    public static void Error(string message, Exception? ex = null)
    {
        Current.Error(message, ex);
    }

    /// <summary>
    /// 记录一条警告级别的日志（始终记录）。
    /// </summary>
    /// <param name="message">警告消息内容</param>
    public static void Warn(string message)
    {
        Current.Warn(message);
    }

    /// <summary>
    /// 记录一条调试级别的日志（仅 Debug 模式）。
    /// </summary>
    /// <param name="message">调试消息内容</param>
    [Conditional("DEBUG")]
    public static void Debug(string message)
    {
        Current.Debug(message);
    }
}
