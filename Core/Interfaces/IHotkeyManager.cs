// ============================================================================
// 文件名: IHotkeyManager.cs
// 文件描述: 全局热键管理器接口，定义热键注册和管理功能的抽象层
// ============================================================================

using Quanta.Models;

namespace Quanta.Core.Interfaces;

/// <summary>
/// 全局热键管理器接口，提供系统级全局热键的注册和监听功能
/// </summary>
public interface IHotkeyManager : IDisposable
{
    /// <summary>
    /// 当注册的全局热键被按下时触发的事件
    /// </summary>
    event EventHandler? HotkeyPressed;

    /// <summary>
    /// 初始化热键管理器，绑定窗口句柄并注册热键
    /// </summary>
    /// <param name="windowHandle">要关联的窗口句柄</param>
    /// <param name="config">热键配置信息</param>
    /// <returns>如果热键注册成功返回 true，否则返回 false</returns>
    bool Initialize(IntPtr windowHandle, HotkeyConfig config);

    /// <summary>
    /// 使用新的配置重新注册热键
    /// </summary>
    /// <param name="config">新的热键配置信息</param>
    /// <returns>如果热键重新注册成功返回 true，否则返回 false</returns>
    bool Reregister(HotkeyConfig config);
}
