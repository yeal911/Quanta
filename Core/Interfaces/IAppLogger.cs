namespace Quanta.Interfaces;

/// <summary>
/// 应用日志服务接口，供依赖注入使用。
/// 默认实现由 <see cref="Services.LoggerService"/> 提供，委托到静态 <see cref="Services.Logger"/>。
/// </summary>
public interface IAppLogger
{
    void Log(string message, string level = "INFO");
    void Error(string message, Exception? ex = null);
    void Warn(string message);
    void Debug(string message);
}
