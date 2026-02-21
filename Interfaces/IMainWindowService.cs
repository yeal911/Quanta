namespace Quanta.Interfaces;

/// <summary>
/// 主窗口服务接口，供 TrayService 依赖，避免与具体 MainWindow 类强耦合。
/// </summary>
public interface IMainWindowService
{
    /// <summary>刷新界面本地化文本</summary>
    void RefreshLocalization();

    /// <summary>显示主窗口（带淡入动画）</summary>
    void ShowWindow();

    /// <summary>从托盘显示窗口的时间戳，用于防止 Deactivated 事件误触发隐藏</summary>
    DateTime LastShownFromTray { get; set; }
}
