// ============================================================================
// 文件名：CommandSettingsWindow.Settings.cs
// 文件用途：通用设置面板逻辑：主题切换、开机启动、语言选择、数值验证等。
// ============================================================================

using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Quanta.Helpers;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.Views;

public partial class CommandSettingsWindow
{
    private const string StartupRegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppRegistryName = "Quanta";

    /// <summary>
    /// 设置当前主题（明/暗）。
    /// 颜色由 DynamicResource + ThemeService 统一处理，此处只记录状态。
    /// </summary>
    public void SetDarkTheme(bool isDark)
    {
        _isDarkTheme = isDark;
    }

    /// <summary>
    /// 从配置和注册表加载通用设置，填充对应控件。
    /// StartWithWindows 以注册表为准（真实状态），MaxResults 从配置读取。
    /// </summary>
    private void LoadAppSettings()
    {
        var config = ConfigLoader.Load();
        StartWithWindowsCheck.IsChecked = IsStartWithWindowsEnabled();
        MaxResultsBox.Text = config.AppSettings.MaxResults.ToString();
        QRCodeThresholdBox.Text = config.AppSettings.QRCodeThreshold.ToString();
        DarkThemeCheck.IsChecked = config.Theme?.Equals("Dark", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>查询注册表，判断 Quanta 是否已设置为开机启动。</summary>
    private bool IsStartWithWindowsEnabled()
    {
        using var key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
                                   .OpenSubKey(StartupRegistryKeyPath);
        return key?.GetValue(AppRegistryName) != null;
    }

    /// <summary>暗色主题 CheckBox 状态变更。</summary>
    private void DarkThemeCheck_Changed(object sender, RoutedEventArgs e)
    {
        bool isDark = DarkThemeCheck.IsChecked == true;

        var config = ConfigLoader.Load();
        config.Theme = isDark ? "Dark" : "Light";
        ConfigLoader.Save(config);

        ThemeService.ApplyTheme(isDark ? "Dark" : "Light");
        SetDarkTheme(isDark);
        ToastService.Instance.SetTheme(isDark);

        if (Owner is MainWindow mainWindow)
            mainWindow.UpdateThemeIcon(isDark);

        ShowAutoSaveToast();
    }

    /// <summary>
    /// 向注册表写入或删除开机启动项，并同步保存到配置文件。
    /// </summary>
    private void ApplyStartWithWindows(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKeyPath, writable: true);
            if (key == null) return;

            if (enable)
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(AppRegistryName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppRegistryName, throwOnMissingValue: false);
            }

            var config = ConfigLoader.Load();
            config.AppSettings.StartWithWindows = enable;
            ConfigLoader.Save(config);
            ShowAutoSaveToast();
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError($"开机启动设置失败: {ex.Message}");
        }
    }

    /// <summary>开机启动 CheckBox 状态变更，立即写入注册表。</summary>
    private void StartWithWindowsCheck_Changed(object sender, RoutedEventArgs e)
    {
        ApplyStartWithWindows(StartWithWindowsCheck.IsChecked == true);
    }

    /// <summary>
    /// 动态填充语言选择 ComboBox
    /// </summary>
    private void PopulateLanguageComboBox()
    {
        var currentLang = LocalizationService.CurrentLanguage;
        LanguageComboBox.Items.Clear();

        foreach (var lang in LanguageManager.GetEnabledLanguages())
        {
            var item = new ComboBoxItem
            {
                Content = lang.NativeName,
                Tag = lang.Code
            };
            LanguageComboBox.Items.Add(item);

            if (lang.Code.Equals(currentLang, StringComparison.OrdinalIgnoreCase))
                LanguageComboBox.SelectedItem = item;
        }
    }

    /// <summary>语言切换 ComboBox 选择变更，立即切换语言并刷新界面。</summary>
    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string langCode)
        {
            if (LocalizationService.CurrentLanguage != langCode)
            {
                LocalizationService.CurrentLanguage = langCode;
                ApplyLocalization();

                if (Owner is MainWindow mainWindow)
                    mainWindow.RefreshLocalization();
            }
        }
    }

    /// <summary>最多显示条数输入框：只允许输入数字。</summary>
    private void MaxResultsBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    /// <summary>最多显示条数输入框失焦时保存（范围 1-50，非法值还原）。</summary>
    private void MaxResultsBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(MaxResultsBox.Text, out int val) && val >= 1 && val <= 50)
        {
            var config = ConfigLoader.Load();
            config.AppSettings.MaxResults = val;
            ConfigLoader.Save(config);
            ShowAutoSaveToast();
        }
        else
        {
            var config = ConfigLoader.Load();
            MaxResultsBox.Text = config.AppSettings.MaxResults.ToString();
        }
    }

    /// <summary>二维码阈值输入框：只允许输入数字。</summary>
    private void QRCodeThresholdBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    /// <summary>二维码阈值输入框失焦时保存（范围 5-100，非法值还原）。</summary>
    private void QRCodeThresholdBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(QRCodeThresholdBox.Text, out int val) && val >= 5 && val <= 100)
        {
            var config = ConfigLoader.Load();
            config.AppSettings.QRCodeThreshold = val;
            ConfigLoader.Save(config);
            ShowAutoSaveToast();
        }
        else
        {
            var config = ConfigLoader.Load();
            QRCodeThresholdBox.Text = config.AppSettings.QRCodeThreshold.ToString();
        }
    }
}
