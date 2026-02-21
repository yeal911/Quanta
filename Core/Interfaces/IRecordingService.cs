// ============================================================================
// 文件名: IRecordingService.cs
// 文件描述: 录音服务接口，定义录音功能的公共契约
// ============================================================================

using System;
using System.Threading.Tasks;
using Quanta.Models;

namespace Quanta.Core.Interfaces;

/// <summary>
/// 录音服务接口，定义录音功能的公共契约
/// </summary>
public interface IRecordingService : IDisposable
{
    /// <summary>当前录音状态</summary>
    RecordingState State { get; }

    /// <summary>当前输出文件路径</summary>
    string OutputFilePath { get; }

    /// <summary>录音进度更新事件</summary>
    event EventHandler<RecordingProgressEventArgs>? ProgressUpdated;

    /// <summary>录音错误事件</summary>
    event EventHandler<string>? ErrorOccurred;

    /// <summary>录音状态变更事件</summary>
    event EventHandler<RecordingState>? StateChanged;

    /// <summary>录音保存完成事件</summary>
    event EventHandler<string>? RecordingSaved;

    /// <summary>设备信息变更事件。null 表示该路未启用。</summary>
    event EventHandler<(string? MicName, string? SpeakerName)>? DeviceChanged;

    /// <summary>
    /// 异步启动录音
    /// </summary>
    /// <param name="settings">录音设置</param>
    /// <param name="outputFilePath">输出文件路径</param>
    /// <returns>启动是否成功</returns>
    Task<bool> StartAsync(RecordingSettings settings, string outputFilePath);

    /// <summary>暂停录音</summary>
    void Pause();

    /// <summary>恢复录音</summary>
    void Resume();

    /// <summary>异步停止录音</summary>
    /// <returns>停止是否成功</returns>
    Task<bool> StopAsync();

    /// <summary>异步丢弃录音（删除文件）</summary>
    /// <returns>丢弃是否成功</returns>
    Task<bool> DiscardAsync();
}

/// <summary>
/// 录音状态枚举
/// </summary>
public enum RecordingState { Idle, Recording, Paused, Stopping }

/// <summary>
/// 录音进度事件参数
/// </summary>
public class RecordingProgressEventArgs : EventArgs
{
    public TimeSpan Duration { get; init; }
    public long FileSizeBytes { get; init; }
    public string FileSizeDisplay => FormatSize(FileSizeBytes);
    public long EstimatedCompressedBytes { get; init; }
    public string EstimatedCompressedDisplay => FormatSize(EstimatedCompressedBytes);

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024):F2} MB";
    }
}
