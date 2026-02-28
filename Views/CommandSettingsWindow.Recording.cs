// ============================================================================
// 文件名：CommandSettingsWindow.Recording.cs
// 文件用途：录音设置面板的加载、保存及各下拉框事件处理。
// ============================================================================

using System;
using System.Windows;
using System.Windows.Controls;
using Quanta.Helpers;
using Quanta.Services;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace Quanta.Views;

public partial class CommandSettingsWindow
{
    private bool _suppressRecordingEvents = false;

    // ── 填充下拉框 ─────────────────────────────────────────────────────

    /// <summary>动态填充录音源 ComboBox</summary>
    private void PopulateRecordSourceCombo()
    {
        var currentSource = GetComboTag(RecordSourceCombo) ?? "Mic";
        RecordSourceCombo.Items.Clear();
        RecordSourceCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordSourceMic"), Tag = "Mic" });
        RecordSourceCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordSourceSpeaker"), Tag = "Speaker" });
        RecordSourceCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordSourceMicSpeaker"), Tag = "Mic&Speaker" });
        SelectComboByTag(RecordSourceCombo, currentSource);
    }

    /// <summary>动态填充录音格式 ComboBox</summary>
    private void PopulateRecordFormatCombo()
    {
        var currentFormat = GetComboTag(RecordFormatCombo) ?? "m4a";
        RecordFormatCombo.Items.Clear();
        RecordFormatCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordFormatM4a"), Tag = "m4a" });
        RecordFormatCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordFormatMp3"), Tag = "mp3" });
        SelectComboByTag(RecordFormatCombo, currentFormat);
    }

    /// <summary>动态填充码率 ComboBox</summary>
    private void PopulateRecordBitrateCombo()
    {
        var currentBitrate = GetComboTag(RecordBitrateCombo) ?? "128";
        RecordBitrateCombo.Items.Clear();
        RecordBitrateCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordBitrate32"), Tag = "32" });
        RecordBitrateCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordBitrate64"), Tag = "64" });
        RecordBitrateCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordBitrate96"), Tag = "96" });
        RecordBitrateCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordBitrate128"), Tag = "128" });
        RecordBitrateCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordBitrate160"), Tag = "160" });
        SelectComboByTag(RecordBitrateCombo, currentBitrate);
    }

    /// <summary>动态填充声道 ComboBox</summary>
    private void PopulateRecordChannelsCombo()
    {
        var currentChannels = GetComboTag(RecordChannelsCombo) ?? "2";
        RecordChannelsCombo.Items.Clear();
        RecordChannelsCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordChannelsStereo"), Tag = "2" });
        RecordChannelsCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordChannelsMono"), Tag = "1" });
        SelectComboByTag(RecordChannelsCombo, currentChannels);
    }

    /// <summary>
    /// 从配置文件加载录音设置到界面控件。
    /// </summary>
    private void LoadRecordingSettings()
    {
        var config = ConfigLoader.Load();
        var rec = config.RecordingSettings;

        // 先填充 ComboBox，传入配置中的值
        PopulateRecordSourceCombo(rec.Source);
        PopulateRecordFormatCombo(rec.Format);
        PopulateRecordBitrateCombo(rec.Bitrate.ToString());
        PopulateRecordChannelsCombo(rec.Channels.ToString());

        RecordOutputPathBox.Text = string.IsNullOrEmpty(rec.OutputPath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : rec.OutputPath;

        UpdateEstimatedSize();
    }

    /// <summary>
    /// 带参数的 Populate 方法，用于初始化时传入配置值
    /// </summary>
    private void PopulateRecordSourceCombo(string? selectedValue)
    {
        var currentSource = selectedValue ?? GetComboTag(RecordSourceCombo) ?? "Mic";
        RecordSourceCombo.Items.Clear();
        RecordSourceCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordSourceMic"), Tag = "Mic" });
        RecordSourceCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordSourceSpeaker"), Tag = "Speaker" });
        RecordSourceCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordSourceMicSpeaker"), Tag = "Mic&Speaker" });
        SelectComboByTag(RecordSourceCombo, currentSource);
    }

    private void PopulateRecordFormatCombo(string? selectedValue)
    {
        var currentFormat = selectedValue ?? GetComboTag(RecordFormatCombo) ?? "m4a";
        RecordFormatCombo.Items.Clear();
        RecordFormatCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordFormatM4a"), Tag = "m4a" });
        RecordFormatCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordFormatMp3"), Tag = "mp3" });
        SelectComboByTag(RecordFormatCombo, currentFormat);
    }

    private void PopulateRecordBitrateCombo(string? selectedValue)
    {
        var currentBitrate = selectedValue ?? GetComboTag(RecordBitrateCombo) ?? "128";
        RecordBitrateCombo.Items.Clear();
        RecordBitrateCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordBitrate32"), Tag = "32" });
        RecordBitrateCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordBitrate64"), Tag = "64" });
        RecordBitrateCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordBitrate96"), Tag = "96" });
        RecordBitrateCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordBitrate128"), Tag = "128" });
        RecordBitrateCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordBitrate160"), Tag = "160" });
        SelectComboByTag(RecordBitrateCombo, currentBitrate);
    }

    private void PopulateRecordChannelsCombo(string? selectedValue)
    {
        var currentChannels = selectedValue ?? GetComboTag(RecordChannelsCombo) ?? "2";
        RecordChannelsCombo.Items.Clear();
        RecordChannelsCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordChannelsStereo"), Tag = "2" });
        RecordChannelsCombo.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("RecordChannelsMono"), Tag = "1" });
        SelectComboByTag(RecordChannelsCombo, currentChannels);
    }

    private void SaveRecordingSettings(bool showToast = false)
    {
        var config = ConfigLoader.Load();
        config.RecordingSettings.Source = GetComboTag(RecordSourceCombo) ?? "Mic";
        config.RecordingSettings.Format = GetComboTag(RecordFormatCombo) ?? "m4a";
        config.RecordingSettings.Bitrate = int.TryParse(GetComboTag(RecordBitrateCombo), out int br) ? br : 32;
        config.RecordingSettings.Channels = int.TryParse(GetComboTag(RecordChannelsCombo), out int ch) ? ch : 1;
        config.RecordingSettings.OutputPath = RecordOutputPathBox.Text.Trim();
        ConfigLoader.Save(config);

        if (showToast)
            ShowAutoSaveToast();
    }

    // ── 事件处理 ───────────────────────────────────────────────────────

    private void RecordSourceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_suppressRecordingEvents) SaveRecordingSettings(showToast: true);
    }

    private void RecordFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_suppressRecordingEvents) SaveRecordingSettings(showToast: true);
    }

    private void RecordBitrateCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_suppressRecordingEvents)
        {
            SaveRecordingSettings(showToast: true);
            UpdateEstimatedSize();
        }
    }

    private void RecordChannelsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_suppressRecordingEvents) SaveRecordingSettings(showToast: true);
    }

    private void RecordOutputPathBox_LostFocus(object sender, RoutedEventArgs e)
    {
        SaveRecordingSettings(showToast: true);
    }

    private void RecordBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = LocalizationService.Get("RecordOutputPath"),
            SelectedPath = RecordOutputPathBox.Text,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            RecordOutputPathBox.Text = dialog.SelectedPath;
            SaveRecordingSettings();
        }
    }

    // ── 预估大小 ───────────────────────────────────────────────────────

    /// <summary>
    /// 更新录音预估文件大小显示。
    /// 计算公式：bitrate(kbps) / 8 * 60 = KB/分钟
    /// </summary>
    private void UpdateEstimatedSize()
    {
        var bitrateStr = GetComboTag(RecordBitrateCombo) ?? "32";
        if (int.TryParse(bitrateStr, out int bitrate))
        {
            int kbPerMin = (bitrate / 8) * 60;
            if (kbPerMin >= 1024)
            {
                double mbPerMin = kbPerMin / 1024.0;
                var mbUnit = LocalizationService.Get("RecordEstimatedSizeUnitMb");
                RecordEstimatedSizeValue.Text = $"{mbPerMin:F1} {mbUnit}";
            }
            else
            {
                var unit = LocalizationService.Get("RecordEstimatedSizeUnit");
                RecordEstimatedSizeValue.Text = $"{kbPerMin} {unit}";
            }
        }
        else
        {
            RecordEstimatedSizeValue.Text = "-- " + LocalizationService.Get("RecordEstimatedSizeUnit");
        }
    }

    // ── 工具方法 ───────────────────────────────────────────────────────

    private static void SelectComboByTag(WpfComboBox combo, string tagValue)
    {
        foreach (WpfComboBoxItem item in combo.Items)
        {
            if (item.Tag?.ToString()?.Equals(tagValue, StringComparison.OrdinalIgnoreCase) == true)
            {
                combo.SelectedItem = item;
                return;
            }
        }
        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
    }

    private static string? GetComboTag(WpfComboBox combo)
        => (combo.SelectedItem as WpfComboBoxItem)?.Tag?.ToString();
}
