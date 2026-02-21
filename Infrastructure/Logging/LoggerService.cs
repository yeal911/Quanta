using Quanta.Interfaces;

namespace Quanta.Services;

/// <summary>
/// <see cref="IAppLogger"/> 的默认实现，委托到静态 <see cref="Logger"/>。
/// 静态 Logger 的所有调用站点无需修改，此类仅为 DI 提供可注入的实例。
/// </summary>
public sealed class LoggerService : IAppLogger
{
    public void Log(string message, string level = "INFO") => Logger.Log(message, level);
    public void Error(string message, Exception? ex = null) => Logger.Error(message, ex);
    public void Warn(string message) => Logger.Warn(message);
    public void Debug(string message) => Logger.Debug(message);
}
