using Quanta.Core.Interfaces;
using Quanta.Services;

namespace Quanta.Infrastructure.System;

public sealed class ToastNotifier : IToastNotifier
{
    public void SetTheme(bool isDarkTheme) => ToastService.Instance.SetTheme(isDarkTheme);
    public void ShowSuccess(string message, double duration = 1.5) => ToastService.Instance.ShowSuccess(message, duration);
    public void ShowError(string message, double duration = 1.5) => ToastService.Instance.ShowError(message, duration);
    public void ShowWarning(string message, double duration = 1.5) => ToastService.Instance.ShowWarning(message, duration);
    public void ShowInfo(string message, double duration = 1.5) => ToastService.Instance.ShowInfo(message, duration);
}
