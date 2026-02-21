// ============================================================================
// 文件名: ITrayService.cs
// 文件描述: 系统托盘服务接口，定义托盘图标和菜单管理的抽象层
// ============================================================================

namespace Quanta.Core.Interfaces;

/// <summary>
/// 系统托盘服务接口，提供系统托盘图标和右键菜单的管理功能
/// </summary>
public interface ITrayService : IDisposable
{
    /// <summary>
    /// 当用户在托盘菜单中点击"设置"选项时触发的事件
    /// </summary>
    event EventHandler? SettingsRequested;

    /// <summary>
    /// 当用户在托盘菜单中点击"退出"选项时触发的事件
    /// </summary>
    event EventHandler? ExitRequested;

    /// <summary>
    /// 退出前检查回调。返回 false 则取消退出（例如录音进行中）
    /// </summary>
    Func<bool>? CanExit { get; set; }

    /// <summary>
    /// 初始化系统托盘图标和上下文菜单
    /// </summary>
    void Initialize();
}
