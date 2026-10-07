// ============================================================================
// 文件名：MainViewModel.Recording.cs
// 文件用途：主窗口视图模型的录音业务分部类：录音启动前置校验、配置持久化、
//          录音启停、录音配置芯片的右键循环切换、退出/关闭校验。
//          录音状态机与 NAudio 设备异常重试位于 RecordingService（领域层），
//          本分部只承载 UI 侧的业务编排；悬浮窗接线留在视图层。
// ============================================================================

using System;
using System.IO;
using System.Threading.Tasks;
using Quanta.Core.Config;
using Quanta.Core.Interfaces;
using Quanta.Domain.Search;
using Quanta.Infrastructure.Logging;
using Quanta.Presentation.Helpers;

namespace Quanta.Presentation.ViewModels;

public partial class MainViewModel
{
    /// <summary>录音服务实例（DI 注入）</summary>
    private readonly IRecordingService _recordingService;

    /// <summary>当前是否有录音正在进行（含暂停状态）</summary>
    public bool IsRecordingActive => _recordingService.State != RecordingState.Idle;

    /// <summary>
    /// 录音启动请求：经校验与配置持久化后交给视图层创建悬浮窗、
    /// 再由 <see cref="StartRecordingAsync"/> 实际启动录音。
    /// </summary>
    /// <param name="Settings">录音设置（已持久化到配置）</param>
    /// <param name="OutputFilePath">输出文件完整路径</param>
    /// <param name="OutputDirectory">输出目录（悬浮窗展示用）</param>
    public sealed record RecordingStartRequest(
        RecordingSettings Settings,
        string OutputFilePath,
        string OutputDirectory);

    /// <summary>
    /// 录音启动前置处理：校验录音状态、把搜索结果携带的录音配置持久化到 AppConfig。
    /// </summary>
    /// <param name="result">录音命令搜索结果</param>
    /// <returns>录音启动请求；正在录音或结果无效时返回 null</returns>
    public RecordingStartRequest? PrepareRecording(SearchResult result)
    {
        if (_recordingService.State != RecordingState.Idle)
        {
            ToastService.Instance.ShowWarning(LocalizationService.Get("RecordAlreadyRecording"));
            return null;
        }

        var recordData = result.RecordData;
        if (recordData == null) return null;

        var outputPath = recordData.OutputFileName;
        var outputDir = Path.GetDirectoryName(outputPath) ?? "";

        // 保存当前配置到 AppConfig
        var config = _configLoader.Load();
        config.RecordingSettings.Source = recordData.Source;
        config.RecordingSettings.Format = recordData.Format;
        config.RecordingSettings.Bitrate = recordData.Bitrate;
        config.RecordingSettings.Channels = recordData.Channels;
        config.RecordingSettings.OutputPath = recordData.OutputPath;
        _configLoader.Save(config);

        return new RecordingStartRequest(config.RecordingSettings, outputPath, outputDir);
    }

    /// <summary>
    /// 实际启动录音（悬浮窗已由视图层创建）。
    /// </summary>
    /// <param name="request">录音启动请求</param>
    /// <returns>启动是否成功</returns>
    public Task<bool> StartRecordingAsync(RecordingStartRequest request) =>
        _recordingService.StartAsync(request.Settings, request.OutputFilePath);

    /// <summary>
    /// 停止当前录音。
    /// </summary>
    public Task<bool> StopRecordingAsync() => _recordingService.StopAsync();

    /// <summary>
    /// 录音启动失败时的统一错误处理：记录日志并提示用户。
    /// </summary>
    /// <param name="ex">启动过程中抛出的异常</param>
    public void HandleRecordingStartError(Exception ex)
    {
        Logger.Error($"StartRecordingFromResult failed: {ex}");
        ToastService.Instance.ShowError(LocalizationService.Get("RecordError") + ": " + ex.Message);
    }

    /// <summary>
    /// 校验当前是否允许退出/关闭窗口：录音进行中时提示并返回 false。
    /// </summary>
    public bool ConfirmCanExit()
    {
        if (IsRecordingActive)
        {
            ToastService.Instance.ShowWarning(LocalizationService.Get("RecordAlreadyRecording"));
            return false;
        }
        return true;
    }

    /// <summary>
    /// 循环切换录音配置芯片的取值并持久化（右键切换）。
    /// </summary>
    /// <param name="result">录音命令搜索结果</param>
    /// <param name="field">配置字段名：Source / Format / Bitrate / Channels</param>
    public void CycleRecordOption(SearchResult result, string field)
    {
        var recordData = result.RecordData;
        if (recordData == null) return;

        switch (field)
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
                });
                break;

            case "Channels":
                CycleOption(new[] { "1", "2" }, recordData.Channels.ToString(), val =>
                {
                    recordData.Channels = int.Parse(val);
                    SaveRecordingSettingField("Channels", val);
                });
                break;
        }
    }

    /// <summary>
    /// 在选项数组中循环切换到下一个取值。
    /// </summary>
    /// <param name="options">可选值数组</param>
    /// <param name="currentValue">当前值</param>
    /// <param name="onChange">取值变更回调</param>
    private static void CycleOption(string[] options, string currentValue, Action<string> onChange)
    {
        int currentIndex = Array.IndexOf(options, currentValue);
        int nextIndex = (currentIndex + 1) % options.Length;
        onChange(options[nextIndex]);
    }

    /// <summary>
    /// 将单个录音配置字段保存到 AppConfig。
    /// </summary>
    /// <param name="field">配置字段名</param>
    /// <param name="value">新值</param>
    private void SaveRecordingSettingField(string field, string value)
    {
        var config = _configLoader.Load();
        switch (field)
        {
            case "Source": config.RecordingSettings.Source = value; break;
            case "Format": config.RecordingSettings.Format = value; break;
            case "Bitrate": config.RecordingSettings.Bitrate = int.TryParse(value, out int br) ? br : 128; break;
            case "Channels": config.RecordingSettings.Channels = int.TryParse(value, out int ch) ? ch : 1; break;
        }
        _configLoader.Save(config);
    }
}
