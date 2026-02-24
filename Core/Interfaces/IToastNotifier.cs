namespace Quanta.Core.Interfaces;

/// <summary>
/// Toast 通知抽象，屏蔽静态 ToastService.Instance。
/// </summary>
public interface IToastNotifier
{
    void SetTheme(bool isDarkTheme);
    void ShowSuccess(string message, double duration = 1.5);
    void ShowError(string message, double duration = 1.5);
    void ShowWarning(string message, double duration = 1.5);
    void ShowInfo(string message, double duration = 1.5);
}
