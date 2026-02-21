// ============================================================================
// 文件名: IToastService.cs
// 文件描述: Toast 通知服务接口，定义通知功能的抽象层
// ============================================================================

namespace Quanta.Core.Interfaces;

/// <summary>
/// Toast 通知服务接口，提供轻量级的弹窗通知功能
/// </summary>
public interface IToastService
{
    /// <summary>
    /// Toast 通知类型枚举
    /// </summary>
    enum ToastType
    {
        /// <summary>成功通知</summary>
        Success,
        /// <summary>错误通知</summary>
        Error,
        /// <summary>警告通知</summary>
        Warning,
        /// <summary>信息通知</summary>
        Info
    }

    /// <summary>
    /// 设置主题模式
    /// </summary>
    void SetTheme(bool isDarkTheme);

    /// <summary>
    /// 设置主窗口引用
    /// </summary>
    void SetMainWindow(System.Windows.Window window);

    /// <summary>
    /// 显示 Toast 通知
    /// </summary>
    void Show(string message, ToastType type = ToastType.Info, double duration = 1.5);

    /// <summary>
    /// 显示成功通知
    /// </summary>
    void ShowSuccess(string message, double duration = 1.5);

    /// <summary>
    /// 显示错误通知
    /// </summary>
    void ShowError(string message, double duration = 1.5);

    /// <summary>
    /// 显示警告通知
    /// </summary>
    void ShowWarning(string message, double duration = 1.5);

    /// <summary>
    /// 显示信息通知
    /// </summary>
    void ShowInfo(string message, double duration = 1.5);
}
