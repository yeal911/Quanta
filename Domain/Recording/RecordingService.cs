// ============================================================================
// 文件名: RecordingService.cs
// 文件用途: 录音服务，负责音频录制的完整生命周期管理。
//          流式 MP3 录音：实时编码写入磁盘，每30秒强制刷新确保断电/崩溃安全。
//          Mic&Speaker 模式：两路独立采集，实时混音后写入 MP3。
//          停止时：MP3 直接使用；M4A 格式则由 MP3 转码。
// ============================================================================
// 文件结构（partial class 拆分）：
//   RecordingService.cs          ← 字段、事件、公共接口（StartAsync/Stop/Pause/Resume）
//   RecordingService.Capture.cs  ← WASAPI 初始化、设备停止回调、热拔插重启
//   RecordingService.Pipeline.cs ← 音频数据回调、混音写入、缓冲区排干
//   RecordingService.Timer.cs    ← 进度上报（每秒）、MP3 刷盘（每30秒）
// ============================================================================

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Lame;
using Quanta.Helpers;
using Quanta.Models;

namespace Quanta.Services;

public enum RecordingState { Idle, Recording, Paused, Stopping }

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

public partial class RecordingService : IDisposable
{
    // ── 配置 ──────────────────────────────────────────────────────────
    private RecordingSettings _settings = new();
    private string _outputFilePath = "";

    // ── MP3 流式写入器 ────────────────────────────────────────────────
    private LameMP3FileWriter? _mp3Writer;
    private FileStream? _mp3FileStream;  // 独立持有底层流，用于中间刷新（不调用 lame_encode_flush）
    private string _mp3OutputPath = "";

    /// <summary>LAME 编码器输入格式（始终为 16-bit PCM @ 用户设置的 SR/CH）</summary>
    private WaveFormat _writeFormat = new WaveFormat(44100, 16, 2);

    // ── WASAPI 采集 ───────────────────────────────────────────────────
    private WasapiCapture? _micCapture;
    private WasapiLoopbackCapture? _loopbackCapture;
    // 创建采集实例时立即读取并缓存设备友好名（避免 NotifyDeviceChanged 依赖系统默认设备注册表时序）
    private string? _micDeviceName;
    private string? _speakerDeviceName;

    // ── Loopback 格式转换链（native float → 16-bit PCM @ _writeFormat）──
    // 由 loopback DataAvailable 填充，由 OnLoopbackDataAvailable 消费
    private BufferedWaveProvider? _loopbackBuffer;
    private IWaveProvider? _loopbackConverted;       // 转换链末端，输出 _writeFormat

    // ── Mic 缓冲（Mic&Speaker 混音时使用）────────────────────────────
    // OnMicDataAvailable 填充，loopback 驱动时消费并与 loopback 混音
    private BufferedWaveProvider? _micBuffer;

    // ── 状态管理 ──────────────────────────────────────────────────────
    private RecordingState _state = RecordingState.Idle;
    private DateTime _startTime;
    private TimeSpan _pausedDuration;
    private DateTime? _pauseStartTime;
    private readonly object _writeLock = new();
    private readonly object _stateLock = new();

    // ── 重启防并发标志（Interlocked：0=空闲，1=正在重启）────────────────
    // 防止多次设备事件触发多个并发 TryRestart 调用互相覆盖字段导致竞争
    private int _loopbackRestarting;
    private int _micRestarting;

    // ── 定时器 ────────────────────────────────────────────────────────
    private System.Threading.Timer? _progressTimer;   // 每秒上报进度
    private System.Threading.Timer? _flushTimer;      // 每30秒刷新 MP3 文件到磁盘

    // ── 数据统计 ──────────────────────────────────────────────────────
    private long _totalBytesMic;
    private long _totalBytesLoopback;
    private long _totalBytesWritten;

    // ── 事件 ──────────────────────────────────────────────────────────
    public event EventHandler<RecordingProgressEventArgs>? ProgressUpdated;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<RecordingState>? StateChanged;
    public event EventHandler<string>? RecordingSaved;
    /// <summary>设备信息变更（启动时 + 每次设备重建后触发）。null 表示该路未启用。</summary>
    public event EventHandler<(string? MicName, string? SpeakerName)>? DeviceChanged;

    public RecordingState State => _state;
    public string OutputFilePath => _outputFilePath;

    // ════════════════════════════════════════════════════════════════════
    // 公共接口
    // ════════════════════════════════════════════════════════════════════

    public async Task<bool> StartAsync(RecordingSettings settings, string outputFilePath)
    {
        lock (_stateLock)
        {
            if (_state != RecordingState.Idle)
            {
                Logger.Warn("RecordingService.StartAsync: already recording, state=" + _state);
                return false;
            }
        }

        _settings = settings;
        _outputFilePath = outputFilePath;

        Logger.Debug($"RecordingService.StartAsync: source={settings.Source}, format={settings.Format}, " +
                     $"sampleRate={settings.SampleRate}, bitrate={settings.Bitrate}, channels={settings.Channels}");
        Logger.Debug($"RecordingService.StartAsync: outputFilePath={outputFilePath}");

        var outputDir = Path.GetDirectoryName(outputFilePath);
        if (string.IsNullOrEmpty(outputDir))
            outputDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        if (!Directory.Exists(outputDir))
        {
            try { Directory.CreateDirectory(outputDir); }
            catch (Exception ex)
            {
                Logger.Error($"RecordingService: Cannot create output dir: {ex.Message}");
                OnError(LocalizationService.Get("RecordNoPermission"));
                return false;
            }
        }

        if (!RecordingFileUtils.CheckWritePermission(outputDir)) { OnError(LocalizationService.Get("RecordNoPermission")); return false; }
        if (!RecordingFileUtils.CheckDiskSpace(outputDir))       { OnError(LocalizationService.Get("RecordDiskFull"));     return false; }

        // 内部总是先写 MP3，停止时再按需转换
        _mp3OutputPath = Path.ChangeExtension(outputFilePath, ".mp3");
        Logger.Debug($"RecordingService: streaming MP3 path={_mp3OutputPath}");

        try
        {
            bool ok = await Task.Run(() => InitializeAndStartCapture());
            Logger.Debug($"RecordingService.StartAsync: init result={ok}");
            return ok;
        }
        catch (Exception ex)
        {
            Logger.Error($"RecordingService.StartAsync failed: {ex}");
            Cleanup();
            OnError(LocalizationService.Get("RecordError") + ": " + ex.Message);
            return false;
        }
    }

    public void Pause()
    {
        lock (_stateLock) { if (_state != RecordingState.Recording) { Logger.Warn("Pause: invalid state"); return; } }
        Logger.Debug("RecordingService.Pause");
        // 先设状态再调 StopRecording：防止 OnCaptureStopped 回调在状态设置前触发，
        // 误判为"设备意外停止"并触发不必要的重启。
        SetState(RecordingState.Paused);
        _pauseStartTime = DateTime.Now;
        _micCapture?.StopRecording();
        _loopbackCapture?.StopRecording();
    }

    public void Resume()
    {
        lock (_stateLock) { if (_state != RecordingState.Paused) { Logger.Warn("Resume: invalid state"); return; } }
        if (_pauseStartTime.HasValue) { _pausedDuration += DateTime.Now - _pauseStartTime.Value; _pauseStartTime = null; }
        Logger.Debug($"RecordingService.Resume: pausedDuration={_pausedDuration}");
        try { _micCapture?.StartRecording(); _loopbackCapture?.StartRecording(); }
        catch (Exception ex) { Logger.Error($"RecordingService.Resume failed: {ex.Message}"); }
        SetState(RecordingState.Recording);
    }

    public async Task StopAsync()
    {
        lock (_stateLock)
        {
            if (_state == RecordingState.Idle || _state == RecordingState.Stopping)
            { Logger.Warn("StopAsync: already stopped"); return; }
        }
        Logger.Debug("RecordingService.StopAsync");
        SetState(RecordingState.Stopping);

        // 停止采集
        try { _micCapture?.StopRecording(); } catch { }
        try { _loopbackCapture?.StopRecording(); } catch { }

        // 停止定时器
        _progressTimer?.Dispose(); _progressTimer = null;
        _flushTimer?.Dispose();    _flushTimer = null;

        Logger.Debug($"RecordingService: stats mic={_totalBytesMic}, loopback={_totalBytesLoopback}, written={_totalBytesWritten}");

        // 排干残留的音频数据（转换链中可能还有缓冲的样本）
        await Task.Delay(100); // 等待最后一批 DataAvailable 回调处理完
        DrainRemainingBuffers();

        // 关闭 MP3 写入器
        lock (_writeLock)
        {
            if (_mp3Writer != null)
            {
                Logger.Debug("RecordingService: flushing and closing MP3 writer");
                // 此处调用 mp3Writer.Flush() 是安全的（仅在停止时执行一次）：
                // 它会终结 LAME 编码器，写入最终帧和 VBR tag，然后置 _outStream=null。
                try { _mp3Writer.Flush(); } catch { }
                try { _mp3Writer.Dispose(); } catch { }
                _mp3Writer = null;
            }
            // 确保底层 FileStream 已关闭（mp3Writer 用 Stream 构造，不自动关闭）
            try { _mp3FileStream?.Flush(); _mp3FileStream?.Dispose(); } catch { }
            _mp3FileStream = null;
        }

        await Task.Delay(100);

        // 按格式处理最终文件
        string fmt = _settings.Format.ToLowerInvariant();

        if ((fmt == "m4a" || fmt == "aac") && File.Exists(_mp3OutputPath))
        {
            Logger.Debug($"RecordingService: converting MP3 → M4A: {_mp3OutputPath} → {_outputFilePath}");
            try
            {
                await AudioTranscoder.ConvertMp3ToM4AAsync(_mp3OutputPath, _outputFilePath, _settings);
                RecordingFileUtils.TryDeleteFile(_mp3OutputPath);
                Logger.Debug("RecordingService: M4A conversion done");
            }
            catch (Exception ex)
            {
                Logger.Warn($"RecordingService: M4A conversion failed, keeping MP3: {ex.Message}");
                _outputFilePath = _mp3OutputPath; // 退回到 MP3
            }
        }
        else if (fmt == "mp3" && File.Exists(_mp3OutputPath) && _mp3OutputPath != _outputFilePath)
        {
            try
            {
                if (File.Exists(_outputFilePath)) File.Delete(_outputFilePath);
                File.Move(_mp3OutputPath, _outputFilePath);
                Logger.Debug($"RecordingService: MP3 moved to {_outputFilePath}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"RecordingService: move MP3 failed: {ex.Message}");
                _outputFilePath = _mp3OutputPath;
            }
        }
        else if (!File.Exists(_outputFilePath) && File.Exists(_mp3OutputPath))
        {
            _outputFilePath = _mp3OutputPath;
        }

        if (File.Exists(_outputFilePath))
        {
            var size = new FileInfo(_outputFilePath).Length;
            Logger.Debug($"RecordingService: saved {_outputFilePath} ({size} bytes)");
            RecordingSaved?.Invoke(this, _outputFilePath);
        }
        else
        {
            Logger.Error($"RecordingService: output file missing: {_outputFilePath}");
            OnError(LocalizationService.Get("RecordSaveFailed"));
        }

        Cleanup();
        SetState(RecordingState.Idle);
    }

    public async Task DiscardAsync()
    {
        lock (_stateLock)
        {
            if (_state == RecordingState.Idle || _state == RecordingState.Stopping)
            { Logger.Warn("DiscardAsync: already stopped"); return; }
        }
        Logger.Debug("RecordingService.DiscardAsync");
        SetState(RecordingState.Stopping);

        try { _micCapture?.StopRecording(); } catch { }
        try { _loopbackCapture?.StopRecording(); } catch { }
        _progressTimer?.Dispose(); _progressTimer = null;
        _flushTimer?.Dispose();    _flushTimer = null;

        await Task.Delay(200);

        lock (_writeLock)
        {
            _mp3Writer?.Dispose(); _mp3Writer = null;
            _mp3FileStream?.Dispose(); _mp3FileStream = null;
        }

        RecordingFileUtils.TryDeleteFile(_mp3OutputPath);
        RecordingFileUtils.TryDeleteFile(_outputFilePath);

        Cleanup();
        SetState(RecordingState.Idle);
        Logger.Debug("RecordingService.DiscardAsync: done");
    }

    // ════════════════════════════════════════════════════════════════════
    // 辅助方法
    // ════════════════════════════════════════════════════════════════════

    private void SetState(RecordingState newState)
    {
        lock (_stateLock) { _state = newState; }
        Logger.Debug($"RecordingService: State → {newState}");
        StateChanged?.Invoke(this, newState);
    }

    private void OnError(string message)
    {
        Logger.Error($"RecordingService.OnError: {message}");
        ErrorOccurred?.Invoke(this, message);
    }

    private void Cleanup()
    {
        Logger.Debug("RecordingService.Cleanup");
        _micCapture?.Dispose();     _micCapture = null;
        _loopbackCapture?.Dispose(); _loopbackCapture = null;
        _loopbackBuffer   = null;
        _loopbackConverted = null;
        _micBuffer        = null;
        lock (_writeLock)
        {
            _mp3Writer?.Dispose(); _mp3Writer = null;
            _mp3FileStream?.Dispose(); _mp3FileStream = null;
        }
        _progressTimer?.Dispose(); _progressTimer = null;
        _flushTimer?.Dispose();    _flushTimer = null;
    }

    public void Dispose()
    {
        Logger.Debug($"RecordingService.Dispose: state={_state}");
        if (_state != RecordingState.Idle)
        {
            try { _micCapture?.StopRecording(); } catch { }
            try { _loopbackCapture?.StopRecording(); } catch { }
        }
        Cleanup();
        if (!string.IsNullOrEmpty(_mp3OutputPath) && _mp3OutputPath != _outputFilePath)
            RecordingFileUtils.TryDeleteFile(_mp3OutputPath);
    }
}
