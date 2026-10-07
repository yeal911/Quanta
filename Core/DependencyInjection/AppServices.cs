// ============================================================================
// 文件名：AppServices.cs
// 文件用途：静态服务定位器，桥接 DI 组合根与静态门面（Logger /
//          LocalizationService / ToastService）。
//          App.OnStartup 构建容器后调用 Configure 注入 IServiceProvider，
//          此后静态门面通过 TryGet 从容器解析实例；容器构建完成前的
//          早期调用（单实例检查、第二实例退出路径）回退到各服务自带的
//          默认实例，行为与纯静态实现完全一致。
// ============================================================================

using Microsoft.Extensions.DependencyInjection;

namespace Quanta.Core.DependencyInjection;

/// <summary>
/// 静态服务定位器。唯一的组合根是 <see cref="Quanta.App"/>（BuildServiceProvider），
/// 本类只负责把容器暴露给保留的静态门面，属于 DI 统一第一步的过渡桥接：
/// 门面签名不变、调用点不变，但解析路径统一走 DI 容器。
/// </summary>
public static class AppServices
{
    /// <summary>组合根注入的服务提供者；Configure 之前为 null</summary>
    private static volatile IServiceProvider? _provider;

    /// <summary>容器是否已配置（App 启动早期、第二实例退出路径尚未配置）</summary>
    public static bool IsConfigured => _provider != null;

    /// <summary>
    /// 由组合根调用，注入构建完成的 DI 容器。整个进程生命周期只应调用一次。
    /// </summary>
    /// <param name="provider">BuildServiceProvider 的返回值</param>
    public static void Configure(IServiceProvider provider)
        => _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <summary>
    /// 从容器解析服务；容器未配置或服务未注册时返回 null，
    /// 调用方（静态门面）自行回退到默认实例。
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>容器中的服务实例，不可用时为 null</returns>
    public static T? TryGet<T>() where T : class
        => _provider?.GetService<T>();
}
