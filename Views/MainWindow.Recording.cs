// ============================================================================
// 文件名：MainWindow.Recording.cs
// 文件用途：录音 UI 流程（启动/配置右键切换/资源清理）。
// ============================================================================

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Quanta.Helpers;
using Quanta.Models;
using Quanta.Services;
using Quanta.Core.Interfaces;

namespace Quanta.Views;

public partial class MainWindow
{
    /// <summary>
    /// 从搜索结果启动录音（由 SearchEngine 通过 Dispatcher 调用）。
    /// </summary>
    public async void StartRecordingFromResult(SearchResult result)
    {
        try
        {
            if (_recordingService.State != RecordingState.Idle)
            {
                ToastService.Instance.ShowWarning(LocalizationService.Get("RecordAlreadyRecording"));
                return;
            }

            var recordData = result.RecordData;
            if (recordData == null) return;

            var outputPath = recordData.OutputFileName;
            var outputDir = System.IO.Path.GetDirectoryName(outputPath) ?? "";

            // 保存当前配置到 AppConfig
            var config = ConfigLoader.Load();
            config.RecordingSettings.Source = recordData.Source;
            config.RecordingSettings.Format = recordData.Format;
            config.RecordingSettings.Bitrate = recordData.Bitrate;
            config.RecordingSettings.Channels = recordData.Channels;
            config.RecordingSettings.OutputPath = recordData.OutputPath;
            ConfigLoader.Save(config);



            _recordingOverlay = new RecordingOverlayWindow(_recordingService, outputDir);
            _recordingOverlay.Closed += (s, e) =>
            {
                _recordingOverlay = null;
                if (_recordingService.State != RecordingState.Idle)
                    _ = _recordingService.StopAsync();
            };

            bool started = await _recordingService.StartAsync(config.RecordingSettings, outputPath);
            if (!started)
            {
                _recordingOverlay?.Close();
                _recordingOverlay = null;
                return;
            }
            _recordingOverlay.Dispatcher.Invoke(() => { });
            _recordingOverlay.Show();
            _recordingOverlay.ShowRecordingUI();
        }
        catch (Exception ex)
        {
            Logger.Error($"StartRecordingFromResult failed: {ex}");
            ToastService.Instance.ShowError(LocalizationService.Get("RecordError") + ": " + ex.Message);
            try { _recordingOverlay?.Close(); } catch { }
            _recordingOverlay = null;
        }
    }

    /// <summary>
    /// 处理录音配置芯片的右键点击，显示上下文菜单以切换配置值。
    /// </summary>
    private void RecordChip_RightClick(object sender, MouseButtonEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element == null) return;

        var result = element.DataContext as SearchResult;
        var recordData = result?.RecordData;
        if (recordData == null) return;

        var tag = element.Tag?.ToString() ?? "";

        switch (tag)
        {
            case "Source":
                CycleOption(new[] { "Mic", "Speaker", "Mic&Speaker" }, recordData.Source, val =>
                {
                    recordData.Source = val;
                    SaveRecordingSettingField("Source", val);
                });
                break;

            case "Format":
                CycleOption(new[] { "m4a", "mp3" }, recordData.Format, val =>
                {
                    recordData.Format = val;
                    SaveRecordingSettingField("Format", val);
                });
                break;

            case "Bitrate":
                CycleOption(new[] { "64", "96", "128", "160" }, recordData.Bitrate.ToString(), val =>
                {
                    recordData.Bitrate = int.Parse(val);
                    SaveRecordingSettingField("Bitrate", val);
                }, v => v + " kbps");
                break;

            case "Channels":
                CycleOption(new[] { "1", "2" }, recordData.Channels.ToString(), val =>
                {
                    recordData.Channels = int.Parse(val);
                    SaveRecordingSettingField("Channels", val);
                }, v => v == "1" ? "单声道" : "立体声");
                break;
        }

        e.Handled = true;
    }

    /// <summary>
    /// 通过右键点击循环切换选项值。
    /// </summary>
    private void CycleOption(string[] options, string currentValue, Action<string> onChange, Func<string, string>? displayFormatter = null)
    {
        int currentIndex = Array.IndexOf(options, currentValue);
        int nextIndex = (currentIndex + 1) % options.Length;
        onChange(options[nextIndex]);
    }

    /// <summary>生成配置菜单项</summary>
    private static void AddMenuItems(
        ContextMenu menu,
        string[] values,
        string current,
        Action<string> onSelect,
        Func<string, string>? labelFormatter = null)
    {
        foreach (var val in values)
        {
            var label = labelFormatter != null ? labelFormatter(val) : val;
            var item = new MenuItem
            {
                Header = label,
                IsChecked = val.Equals(current, StringComparison.OrdinalIgnoreCase)
            };
            var capturedVal = val;
            item.Click += (s, e) => onSelect(capturedVal);
            menu.Items.Add(item);
        }
    }

    /// <summary>将单个录音配置字段保存到 AppConfig</summary>
    private static void SaveRecordingSettingField(string field, string value)
    {
        var config = ConfigLoader.Load();
        switch (field)
        {
            case "Source": config.RecordingSettings.Source = value; break;
            case "Format": config.RecordingSettings.Format = value; break;
            case "Bitrate": config.RecordingSettings.Bitrate = int.TryParse(value, out int br) ? br : 128; break;
        }
        ConfigLoader.Save(config);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_recordingService.State != RecordingState.Idle)
        {
            ToastService.Instance.ShowWarning(LocalizationService.Get("RecordAlreadyRecording"));
            e.Cancel = true;
            return;
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _clipboardMonitor.Stop();
        _hotkeyManager.Dispose();
        if (_recordingService.State != RecordingState.Idle)
        {
            _ = _recordingService.StopAsync();
        }
        _recordingOverlay?.Close();
        base.OnClosed(e);
    }
}
