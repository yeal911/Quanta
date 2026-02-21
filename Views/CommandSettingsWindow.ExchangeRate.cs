// ============================================================================
// 文件名：CommandSettingsWindow.ExchangeRate.cs
// 文件用途：汇率设置面板的加载与保存逻辑。
// ============================================================================

using System.Windows;
using Quanta.Helpers;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.Views;

public partial class CommandSettingsWindow
{
    /// <summary>
    /// 从配置文件加载汇率设置到界面控件。
    /// </summary>
    private void LoadExchangeRateSettings()
    {
        var config = ConfigLoader.Load();
        var exchangeSettings = config.ExchangeRateSettings ?? new ExchangeRateSettings();
        ExchangeRateApiKeyBox.Text = exchangeSettings.ApiKey;
    }

    /// <summary>
    /// 保存汇率设置到配置文件。
    /// </summary>
    private void SaveExchangeRateSettings(bool showToast = false)
    {
        var config = ConfigLoader.Load();
        if (config.ExchangeRateSettings == null)
            config.ExchangeRateSettings = new ExchangeRateSettings();
        config.ExchangeRateSettings.ApiKey = ExchangeRateApiKeyBox.Text.Trim();
        ConfigLoader.Save(config);

        if (showToast)
            ShowAutoSaveToast();
    }

    /// <summary>
    /// 汇率 API Key 输入框失焦时自动保存。
    /// </summary>
    private void ExchangeRateApiKeyBox_LostFocus(object sender, RoutedEventArgs e)
    {
        SaveExchangeRateSettings(showToast: true);
    }
}
