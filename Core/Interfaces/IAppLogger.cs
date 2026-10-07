namespace Quanta.Interfaces;

/// <summary>
/// 应用日志服务接口，供依赖注入使用。
/// 默认实现为 <see cref="Services.LoggerService"/>；
/// 静态门面 <see cref="Services.Logger"/> 委托到容器解析的本接口实例。
/// </summary>
public interface IAppLogger
{
    void Log(string message, string level = "INFO");
    void Error(string message, Exception? ex = null);
    void Warn(string message);
    void Debug(string message);
}
