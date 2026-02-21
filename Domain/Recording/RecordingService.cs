// ============================================================================
// 文件名: RecordingService.cs
// 文件用途: 录音服务，负责音频录制的完整生命周期管理。
//          流式 MP3 录音：实时编码写入磁盘，每30秒强制刷新确保断电/崩溃安全。
//          Mic&Speaker 模式：两路独立采集，实时混音后写入 MP3。
//          停止时：MP3 直接使用；M4A 格式则由 MP3 转码。
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Lame;
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

public class RecordingService : IDisposable
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

        if (!CheckWritePermission(outputDir)) { OnError(LocalizationService.Get("RecordNoPermission")); return false; }
        if (!CheckDiskSpace(outputDir))       { OnError(LocalizationService.Get("RecordDiskFull"));     return false; }

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
                await ConvertMp3ToM4AAsync(_mp3OutputPath, _outputFilePath);
                TryDeleteFile(_mp3OutputPath);
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

        TryDeleteFile(_mp3OutputPath);
        TryDeleteFile(_outputFilePath);

        Cleanup();
        SetState(RecordingState.Idle);
        Logger.Debug("RecordingService.DiscardAsync: done");
    }

    // ════════════════════════════════════════════════════════════════════
    // 初始化
    // ════════════════════════════════════════════════════════════════════

    private bool InitializeAndStartCapture()
    {
        var source = _settings.Source;
        int channels   = Math.Min(Math.Max(_settings.Channels, 1), 2); // clamp 1-2
        int sampleRate = _settings.SampleRate;

        // LameMP3FileWriter 只支持 16-bit PCM 输入
        _writeFormat = new WaveFormat(sampleRate, 16, channels);
        Logger.Debug($"RecordingService: write format for LAME: {_writeFormat}");

        try
        {
            // ── 初始化麦克风采集 ──────────────────────────────────────
            if (source == "Mic" || source == "Mic&Speaker")
            {
                _micCapture = new WasapiCapture();
                // 在 new WasapiCapture() 后立即读取默认 Capture 设备名（此时系统注册表已确定）
                try
                {
                    using var e2 = new MMDeviceEnumerator();
                    using var d2 = e2.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
                    _micDeviceName = d2.FriendlyName;
                }
                catch { _micDeviceName = null; }
                try
                {
                    _micCapture.WaveFormat = _writeFormat;
                    Logger.Debug($"RecordingService: mic format set to {_micCapture.WaveFormat}");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"RecordingService: cannot set mic format ({ex.Message}), using native: {_micCapture.WaveFormat}");
                }
                _micCapture.DataAvailable    += OnMicDataAvailable;
                _micCapture.RecordingStopped += OnCaptureStopped;
            }

            // ── 初始化系统声音采集 + 格式转换链 ──────────────────────
            if (source == "Speaker" || source == "Mic&Speaker")
            {
                _loopbackCapture = new WasapiLoopbackCapture();
                // 在 new WasapiLoopbackCapture() 后立即读取默认 Render 设备名
                try
                {
                    using var e2 = new MMDeviceEnumerator();
                    using var d2 = e2.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    _speakerDeviceName = d2.FriendlyName;
                }
                catch { _speakerDeviceName = null; }
                var nativeFmt = _loopbackCapture.WaveFormat;
                Logger.Debug($"RecordingService: loopback native format: {nativeFmt}");

                // BufferedWaveProvider 接收原生 loopback 数据
                // ★ ReadFully=false 是关键：缓冲区为空时返回 0 而不是静音
                //   若为 true（默认），Read() 永远不返回 0，导致 while 无限循环
                _loopbackBuffer = new BufferedWaveProvider(nativeFmt)
                {
                    DiscardOnBufferOverflow = true,
                    BufferDuration = TimeSpan.FromSeconds(5),
                    ReadFully = false
                };

                // 构建转换链：native float → 16-bit PCM @ _writeFormat
                ISampleProvider sp = _loopbackBuffer.ToSampleProvider();

                // 降声道（如 4ch → 2ch 或 2ch → 1ch）
                if (nativeFmt.Channels != channels)
                {
                    sp = DownmixChannels(sp, channels);
                    Logger.Debug($"RecordingService: loopback channels {nativeFmt.Channels} → {channels}");
                }

                // 重采样（如 48000 → 44100）
                if (nativeFmt.SampleRate != sampleRate)
                {
                    sp = new WdlResamplingSampleProvider(sp, sampleRate);
                    Logger.Debug($"RecordingService: loopback resampled {nativeFmt.SampleRate} → {sampleRate}");
                }

                // 最终声道数对齐（重采样可能改变声道数）
                if (sp.WaveFormat.Channels != channels)
                {
                    sp = channels == 1
                        ? new StereoToMonoSampleProvider(sp)
                        : new MonoToStereoSampleProvider(sp);
                    Logger.Debug($"RecordingService: loopback post-resample channels adjusted to {channels}");
                }

                _loopbackConverted = sp.ToWaveProvider16();
                Logger.Debug("RecordingService: loopback conversion chain ready");

                _loopbackCapture.DataAvailable    += OnLoopbackDataAvailable;
                _loopbackCapture.RecordingStopped += OnCaptureStopped;
            }

            // ── Mic&Speaker：初始化 mic 缓冲（供混音使用）──────────────
            if (source == "Mic&Speaker" && _micCapture != null)
            {
                // mic 已设置为 _writeFormat（16-bit PCM @ targetSR/CH）
                _micBuffer = new BufferedWaveProvider(_micCapture.WaveFormat)
                {
                    DiscardOnBufferOverflow = true,
                    BufferDuration = TimeSpan.FromSeconds(10)
                };
                Logger.Debug($"RecordingService: mic buffer created, format={_micCapture.WaveFormat}");
            }
        }
        catch (NAudio.MmException ex) when (ex.Message.Contains("no device", StringComparison.OrdinalIgnoreCase))
        {
            Logger.Error($"RecordingService: no audio device: {ex.Message}");
            OnError(LocalizationService.Get("RecordNoDevice"));
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Error($"RecordingService: device busy: {ex.Message}");
            OnError(LocalizationService.Get("RecordDeviceBusy"));
            return false;
        }

        // ── 创建 MP3 写入器（流式编码）────────────────────────────────
        try
        {
            // 若最终输出为 m4a，中间 MP3 使用最高质量（EXTREME）：
            // 停止时需要 MP3 → AAC 二次有损转码，高质量中间文件可最小化双重压缩损耗。
            // 若最终输出为 mp3，按用户码率选择 preset。
            bool isFinalMp3 = _settings.Format.Equals("mp3", StringComparison.OrdinalIgnoreCase);
            var preset = isFinalMp3
                ? (_settings.Bitrate <= 96 ? LAMEPreset.STANDARD : LAMEPreset.EXTREME)
                : LAMEPreset.EXTREME;

            // ★ 关键：自己创建并持有 FileStream，用于中间 Flush（只刷 OS 缓冲区）。
            //   LameMP3FileWriter.Flush() 会调用 lame_encode_flush() 终结编码器并置 _outStream=null，
            //   导致后续所有 Write() 抛 "Output stream closed"。
            //   解决：中间 Flush 只调 _mp3FileStream.Flush()（OS 缓冲→磁盘），不调 mp3Writer.Flush()。
            //   停止时才调 mp3Writer.Flush() 一次，正确终结 LAME 并写入 VBR tag。
            _mp3FileStream = new FileStream(_mp3OutputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _mp3Writer = new LameMP3FileWriter(_mp3FileStream, _writeFormat, preset);
            Logger.Debug($"RecordingService: MP3 writer created: {_mp3OutputPath}, format={_writeFormat}, preset={preset}");
        }
        catch (Exception ex)
        {
            Logger.Error($"RecordingService: failed to create MP3 writer: {ex.Message}");
            OnError(LocalizationService.Get("RecordError") + ": " + ex.Message);
            return false;
        }

        // ── 启动采集 ──────────────────────────────────────────────────
        _startTime       = DateTime.Now;
        _pausedDuration  = TimeSpan.Zero;
        _totalBytesMic   = 0;
        _totalBytesLoopback = 0;
        _totalBytesWritten  = 0;

        try { _micCapture?.StartRecording(); Logger.Debug("RecordingService: mic started"); }
        catch (NAudio.MmException ex)
        {
            Logger.Error($"RecordingService: mic start failed: {ex.Message}");
            OnError(LocalizationService.Get("RecordDeviceBusy"));
            Cleanup(); return false;
        }

        try { _loopbackCapture?.StartRecording(); Logger.Debug("RecordingService: loopback started"); }
        catch (NAudio.MmException ex)
        {
            Logger.Error($"RecordingService: loopback start failed: {ex.Message}");
            OnError(LocalizationService.Get("RecordDeviceBusy"));
            Cleanup(); return false;
        }

        SetState(RecordingState.Recording);

        // 进度定时器（每秒）
        _progressTimer = new System.Threading.Timer(ReportProgress, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        // 刷新定时器（每30秒强制写入磁盘，崩溃安全）
        _flushTimer = new System.Threading.Timer(FlushMp3, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        Logger.Debug("RecordingService: timers started");

        // 通知初始设备信息（已在后台线程中，可直接调用）
        NotifyDeviceChanged();

        return true;
    }

    // ════════════════════════════════════════════════════════════════════
    // 声道降混工具
    // ════════════════════════════════════════════════════════════════════

    private static ISampleProvider DownmixChannels(ISampleProvider sp, int targetChannels)
    {
        int inputCh = sp.WaveFormat.Channels;
        if (inputCh == targetChannels) return sp;

        if (inputCh > targetChannels)
        {
            if (inputCh == 2 && targetChannels == 1)
                return new StereoToMonoSampleProvider(sp);

            // 多声道（如 4ch）→ 取前 targetChannels 个声道
            var mux = new MultiplexingSampleProvider(new[] { sp }, targetChannels);
            for (int c = 0; c < targetChannels; c++)
                mux.ConnectInputToOutput(c, c);
            return mux;
        }

        // 升声道（mono → stereo）
        if (inputCh == 1 && targetChannels == 2)
            return new MonoToStereoSampleProvider(sp);

        return sp;
    }

    // ════════════════════════════════════════════════════════════════════
    // 音频数据处理
    // ════════════════════════════════════════════════════════════════════

    private void OnMicDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0) return;
        lock (_stateLock) { if (_state != RecordingState.Recording) return; }

        _totalBytesMic += e.BytesRecorded;

        if (_settings.Source == "Mic&Speaker" && _micBuffer != null)
        {
            // Mic&Speaker：mic 先入缓冲
            _micBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);

            // 当 mic 缓冲积压超过 0.5 秒（可能是 loopback 静音），主动排空避免延迟
            // 0.5s = SampleRate × Channels × 2bytes × 0.5
            int halfSecBytes = _writeFormat.AverageBytesPerSecond / 2;
            if (_micBuffer.BufferedBytes >= halfSecBytes)
            {
                lock (_writeLock)
                {
                    if (_mp3Writer != null)
                        DrainMicAndLoopbackToMp3();
                }
            }
        }
        else
        {
            // 仅 Mic：直接写 MP3（mic 已设置为 _writeFormat，16-bit PCM）
            lock (_writeLock)
            {
                try
                {
                    _mp3Writer?.Write(e.Buffer, 0, e.BytesRecorded);
                    _totalBytesWritten += e.BytesRecorded;
                }
                catch (Exception ex) { Logger.Warn($"RecordingService: mic write failed: {ex.Message}"); }
            }
        }
    }

    private void OnLoopbackDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0) return;
        lock (_stateLock) { if (_state != RecordingState.Recording) return; }

        _totalBytesLoopback += e.BytesRecorded;

        if (_loopbackBuffer == null) return;

        // 将原始 loopback 数据送入转换链输入缓冲（BufferedWaveProvider 线程安全）
        _loopbackBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);

        // 排空转换链，写入 MP3
        lock (_writeLock)
        {
            if (_mp3Writer != null)
                DrainMicAndLoopbackToMp3();
        }
    }

    /// <summary>
    /// 排空 loopback 转换链（已转为 16-bit PCM）并与 mic 缓冲混音，写入 MP3 。
    /// 必须在 _writeLock 保护下调用。
    /// 关键前提：_loopbackBuffer.ReadFully=false，保证缓冲区空时 Read() 返回 0 而非死循环。
    /// </summary>
    private void DrainMicAndLoopbackToMp3()
    {
        // ── 排空 loopback 转换链 ──────────────────────────────────────
        if (_loopbackConverted != null)
        {
            var readBuf = new byte[16384];
            int bytesRead;

            // ReadFully=false 保证缓冲区空时返回 0，循环安全退出
            while ((bytesRead = _loopbackConverted.Read(readBuf, 0, readBuf.Length)) > 0)
            {
                try
                {
                    if (_settings.Source == "Mic&Speaker" && _micBuffer != null)
                    {
                        int micAvail = _micBuffer.BufferedBytes;
                        if (micAvail > 0)
                        {
                            // 有 mic 数据：逐样本混音
                            var micBuf = new byte[bytesRead];
                            int micRead = _micBuffer.Read(micBuf, 0, Math.Min(bytesRead, micAvail));
                            var mixed = MixPcm16(readBuf, bytesRead, micBuf, micRead);
                            _mp3Writer!.Write(mixed, 0, mixed.Length);
                            _totalBytesWritten += mixed.Length;
                        }
                        else
                        {
                            // loopback 有数据但 mic 静音，直接写
                            _mp3Writer!.Write(readBuf, 0, bytesRead);
                            _totalBytesWritten += bytesRead;
                        }
                    }
                    else
                    {
                        // 仅 Speaker
                        _mp3Writer!.Write(readBuf, 0, bytesRead);
                        _totalBytesWritten += bytesRead;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"RecordingService: mp3 write failed: {ex.Message}");
                    break; // 写入失败停止本轮 drain，等下次事件再试
                }
            }
        }

        // ── Mic&Speaker：写入 loopback 没有消费完的 mic 积压（loopback 静音时）──
        if (_settings.Source == "Mic&Speaker" && _micBuffer != null)
        {
            int remaining = _micBuffer.BufferedBytes;
            // 只在积压超过 200ms 时才写（避免和 loopback 事件产生时序竞争导致的小碎片）
            int threshold200ms = _writeFormat.AverageBytesPerSecond / 5;
            if (remaining >= threshold200ms)
            {
                var micBuf = new byte[remaining];
                _micBuffer.Read(micBuf, 0, remaining);
                try
                {
                    _mp3Writer!.Write(micBuf, 0, remaining);
                    _totalBytesWritten += remaining;
                    Logger.Debug($"RecordingService: mic-only drain {remaining} bytes (loopback silent)");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"RecordingService: mic-only drain write failed: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 停止前排干所有剩余的缓冲数据（转换链中可能积压的样本 + mic 残留）。
    /// </summary>
    private void DrainRemainingBuffers()
    {
        lock (_writeLock)
        {
            if (_mp3Writer == null) return;

            // 1. 排干 loopback 转换链中剩余的数据
            if (_loopbackConverted != null)
            {
                var readBuf = new byte[16384];
                int bytesRead;
                while ((bytesRead = _loopbackConverted.Read(readBuf, 0, readBuf.Length)) > 0)
                {
                    try
                    {
                        if (_settings.Source == "Mic&Speaker" && _micBuffer != null)
                        {
                            int micAvail = _micBuffer.BufferedBytes;
                            if (micAvail > 0)
                            {
                                var micBuf = new byte[bytesRead];
                                int micRead = _micBuffer.Read(micBuf, 0, Math.Min(bytesRead, micAvail));
                                var mixed = MixPcm16(readBuf, bytesRead, micBuf, micRead);
                                _mp3Writer.Write(mixed, 0, mixed.Length);
                            }
                            else
                            {
                                _mp3Writer.Write(readBuf, 0, bytesRead);
                            }
                        }
                        else
                        {
                            _mp3Writer.Write(readBuf, 0, bytesRead);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"RecordingService: DrainRemainingBuffers loopback write failed: {ex.Message}");
                        break;
                    }
                }
            }

            // 2. Mic&Speaker 模式下，若 loopback 静音导致 mic 缓冲有残留，直接写入（不混音）
            if (_settings.Source == "Mic&Speaker" && _micBuffer != null)
            {
                int remaining = _micBuffer.BufferedBytes;
                if (remaining > 0)
                {
                    var micBuf = new byte[remaining];
                    _micBuffer.Read(micBuf, 0, remaining);
                    try
                    {
                        _mp3Writer.Write(micBuf, 0, remaining);
                        Logger.Debug($"RecordingService: drained {remaining} bytes of mic residual");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"RecordingService: DrainRemainingBuffers mic residual write failed: {ex.Message}");
                    }
                }
            }

            Logger.Debug("RecordingService: DrainRemainingBuffers done");
        }
    }

    /// <summary>
    /// 将两段 16-bit PCM 数据逐样本相加（带削波），长度不同时短的用零填充。
    /// </summary>
    private static byte[] MixPcm16(byte[] a, int aLen, byte[] b, int bLen)
    {
        int outLen = Math.Max(aLen, bLen);
        // 对齐到 2 字节（每个 16-bit 样本）
        if (outLen % 2 != 0) outLen++;
        var result = new byte[outLen];

        for (int i = 0; i < outLen - 1; i += 2)
        {
            short sA = (i + 1 < aLen) ? BitConverter.ToInt16(a, i) : (short)0;
            short sB = (i + 1 < bLen) ? BitConverter.ToInt16(b, i) : (short)0;
            int mixed = Math.Clamp(sA + sB, short.MinValue, short.MaxValue);
            result[i]     = (byte)(mixed & 0xFF);
            result[i + 1] = (byte)((mixed >> 8) & 0xFF);
        }
        return result;
    }

    private void OnCaptureStopped(object? sender, StoppedEventArgs e)
    {
        // 用类型判断而非引用比较：_loopbackCapture 可能已被替换为新实例，
        // 旧实例回调时 sender != _loopbackCapture 会被误判为 mic 错误。
        bool isLoopback = sender is WasapiLoopbackCapture;
        string tag = isLoopback ? "loopback" : "mic";

        RecordingState cur;
        lock (_stateLock) { cur = _state; }

        if (e.Exception != null)
            Logger.Error($"RecordingService.OnCaptureStopped [{tag}]: exception={e.Exception.Message}, state={cur}");
        else
            Logger.Debug($"RecordingService.OnCaptureStopped [{tag}]: normal stop, state={cur}");

        // 只有录音进行中，设备意外停止（无论有无异常）才重启。
        // Pause()/StopAsync()/DiscardAsync() 会先将 state 设为 Paused/Stopping/Idle 再调 StopRecording()，
        // 所以那些情况下 cur != Recording，直接跳过。
        // Windows 切换默认音频设备时有时以"无异常的正常 stop"方式通知 loopback——
        // 旧逻辑只在 e.Exception != null 时重启，导致 loopback 无声但不重建。
        if (cur != RecordingState.Recording) return;

        if (isLoopback)
        {
            // Interlocked 防止多个事件并发触发多次重建（设备风暴期间常见）
            if (Interlocked.CompareExchange(ref _loopbackRestarting, 1, 0) == 0)
            {
                Logger.Warn($"RecordingService: loopback stopped unexpectedly (ex={e.Exception?.Message ?? "none"}), retrying in 500ms...");
                Task.Delay(500).ContinueWith(_ => TryRestartLoopback());
            }
            else
            {
                Logger.Debug("RecordingService: loopback restart in progress, ignoring duplicate");
            }
        }
        else
        {
            if (Interlocked.CompareExchange(ref _micRestarting, 1, 0) == 0)
            {
                Logger.Warn($"RecordingService: mic stopped unexpectedly (ex={e.Exception?.Message ?? "none"}), retrying in 500ms...");
                Task.Delay(500).ContinueWith(_ => TryRestartMic());
            }
            else
            {
                Logger.Debug("RecordingService: mic restart in progress, ignoring duplicate");
            }
        }
    }

    /// <summary>
    /// 重建并重启 loopback 采集。
    /// WASAPI AudioClient 失效后（Output stream closed）无法直接重启，
    /// 必须 dispose 旧实例、重建新实例和转换链，才能继续捕获系统音频。
    /// 失败时静默降级为仅麦克风录音，不中断录音流程。
    /// </summary>
    private void TryRestartLoopback()
    {
        RecordingState cur;
        lock (_stateLock) { cur = _state; }
        if (cur != RecordingState.Recording) return;

        Logger.Debug("RecordingService: rebuilding loopback capture...");

        try
        {
            // 1. 卸载旧实例
            var old = _loopbackCapture;
            _loopbackCapture = null;
            if (old != null)
            {
                old.DataAvailable    -= OnLoopbackDataAvailable;
                old.RecordingStopped -= OnCaptureStopped;
                try { old.Dispose(); } catch { }
            }

            // 2. 创建新实例（自动绑定当前默认输出设备），立即读取设备名
            var newCapture  = new WasapiLoopbackCapture();
            try
            {
                using var e2 = new MMDeviceEnumerator();
                using var d2 = e2.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _speakerDeviceName = d2.FriendlyName;
            }
            catch { _speakerDeviceName = null; }
            var nativeFmt   = newCapture.WaveFormat;
            int channels    = _writeFormat.Channels;
            int sampleRate  = _writeFormat.SampleRate;
            Logger.Debug($"RecordingService: new loopback native format: {nativeFmt}");

            // 3. 重建格式转换链
            var newBuffer = new BufferedWaveProvider(nativeFmt)
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromSeconds(5),
                ReadFully = false   // 必须 false，否则空缓冲区导致无限循环
            };

            ISampleProvider sp = newBuffer.ToSampleProvider();
            if (nativeFmt.Channels != channels)
                sp = DownmixChannels(sp, channels);
            if (nativeFmt.SampleRate != sampleRate)
                sp = new WdlResamplingSampleProvider(sp, sampleRate);
            if (sp.WaveFormat.Channels < channels)
                sp = new MonoToStereoSampleProvider(sp);
            else if (sp.WaveFormat.Channels > channels)
                sp = new StereoToMonoSampleProvider(sp);

            var newConverted = sp.ToWaveProvider16();

            // 4. 在 _writeLock 内原子替换转换链（避免 DrainMicAndLoopbackToMp3 读到半初始化状态）
            lock (_writeLock)
            {
                _loopbackBuffer    = newBuffer;
                _loopbackConverted = newConverted;
            }

            // 5. 绑定事件、启动新采集
            // 先清标志再 StartRecording：若新实例立刻失败（设备仍在重置），
            // OnCaptureStopped 能正常调度下一次重试而不被 Interlocked 阻挡。
            newCapture.DataAvailable    += OnLoopbackDataAvailable;
            newCapture.RecordingStopped += OnCaptureStopped;
            _loopbackCapture = newCapture;
            Interlocked.Exchange(ref _loopbackRestarting, 0); // 清标志（StartRecording 前）
            newCapture.StartRecording();

            Logger.Debug("RecordingService: loopback rebuild done, capture resumed");
            NotifyDeviceChanged(); // 通知界面刷新设备名称
        }
        catch (Exception ex)
        {
            // 重建失败也不中断录音——mic 会继续录，loopback 部分为静音
            Logger.Warn($"RecordingService: loopback rebuild failed ({ex.Message}), mic-only mode");
            Interlocked.Exchange(ref _loopbackRestarting, 0);
        }
    }

    /// <summary>
    /// 重建并重启麦克风采集。
    /// WASAPI AudioClient 失效（如蓝牙耳机断开重连、音频设备切换）后必须重建实例。
    /// 失败时停止录音，因为 mic 是主要音频源。
    /// </summary>
    private void TryRestartMic()
    {
        RecordingState cur;
        lock (_stateLock) { cur = _state; }
        if (cur != RecordingState.Recording) return;

        Logger.Debug("RecordingService: rebuilding mic capture...");

        try
        {
            // 1. 卸载旧实例
            var old = _micCapture;
            _micCapture = null;
            if (old != null)
            {
                old.DataAvailable    -= OnMicDataAvailable;
                old.RecordingStopped -= OnCaptureStopped;
                try { old.Dispose(); } catch { }
            }

            // 2. 创建新实例，立即读取设备名
            var newCapture = new WasapiCapture();
            try
            {
                using var e2 = new MMDeviceEnumerator();
                using var d2 = e2.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
                _micDeviceName = d2.FriendlyName;
            }
            catch { _micDeviceName = null; }
            try
            {
                newCapture.WaveFormat = _writeFormat;
                Logger.Debug($"RecordingService: new mic format set to {newCapture.WaveFormat}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"RecordingService: cannot set new mic format ({ex.Message}), using native: {newCapture.WaveFormat}");
            }

            // 3. Mic&Speaker 模式：清空 mic 缓冲区的旧残留数据（不重新分配对象）
            if (_settings.Source == "Mic&Speaker")
            {
                _micBuffer?.ClearBuffer();
                Logger.Debug("RecordingService: mic buffer cleared");
            }

            // 4. 绑定事件、启动新采集
            // 先清标志再 StartRecording：若新实例立刻失败，OnCaptureStopped 能正常调度下一次重试。
            newCapture.DataAvailable    += OnMicDataAvailable;
            newCapture.RecordingStopped += OnCaptureStopped;
            _micCapture = newCapture;
            Interlocked.Exchange(ref _micRestarting, 0); // 清标志（StartRecording 前）
            newCapture.StartRecording();

            Logger.Debug("RecordingService: mic rebuild done, capture resumed");
            NotifyDeviceChanged(); // 通知界面刷新设备名称
        }
        catch (Exception ex)
        {
            Logger.Error($"RecordingService: mic rebuild failed ({ex.Message}), stopping recording");
            Interlocked.Exchange(ref _micRestarting, 0);
            _ = StopAsync();
            OnError(LocalizationService.Get("RecordError") + ": " + ex.Message);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // 进度 & 刷新定时器
    // ════════════════════════════════════════════════════════════════════

    private void ReportProgress(object? state)
    {
        RecordingState cur;
        lock (_stateLock) { cur = _state; }
        if (cur != RecordingState.Recording) return;

        var elapsed = DateTime.Now - _startTime - _pausedDuration;
        long fileSize = File.Exists(_mp3OutputPath) ? new FileInfo(_mp3OutputPath).Length : 0;
        long estimatedCompressed = (long)_settings.Bitrate * 1000 / 8 * (long)elapsed.TotalSeconds;

        if (elapsed.TotalSeconds % 5 < 1)
            Logger.Debug($"RecordingService: elapsed={elapsed.TotalSeconds:F0}s, mp3Size={fileSize}, written={_totalBytesWritten}");

        ProgressUpdated?.Invoke(this, new RecordingProgressEventArgs
        {
            Duration               = elapsed,
            FileSizeBytes          = fileSize,
            EstimatedCompressedBytes = estimatedCompressed
        });
    }

    private void FlushMp3(object? state)
    {
        RecordingState cur;
        lock (_stateLock) { cur = _state; }
        if (cur != RecordingState.Recording && cur != RecordingState.Paused) return;

        lock (_writeLock)
        {
            if (_mp3Writer == null) return;
            try
            {
                // 兜底：排空所有积压的 mic 数据（应对 loopback 长时间静音）
                if (_settings.Source == "Mic&Speaker" && _micBuffer != null)
                {
                    int micAvail = _micBuffer.BufferedBytes;
                    if (micAvail > 0)
                    {
                        var micBuf = new byte[micAvail];
                        _micBuffer.Read(micBuf, 0, micAvail);
                        _mp3Writer.Write(micBuf, 0, micAvail);
                        _totalBytesWritten += micAvail;
                        Logger.Debug($"RecordingService.FlushMp3: drained {micAvail} mic bytes");
                    }
                }

                // 只刷 OS 文件缓冲区，不调 _mp3Writer.Flush()！
                // _mp3Writer.Flush() 内部调用 lame_encode_flush()，这是终结操作：
                //   ① 终结 LAME 编码器状态  ② 置 _outStream = null
                //   之后所有 Write() 调用均抛 "Output stream closed"，导致 DataAvailable 线程崩溃循环。
                _mp3FileStream?.Flush();
                long size = File.Exists(_mp3OutputPath) ? new FileInfo(_mp3OutputPath).Length : 0;
                Logger.Debug($"RecordingService.FlushMp3: flushed, mp3Size={size}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"RecordingService.FlushMp3 failed: {ex.Message}");
            }
        }
    }

    private void SetState(RecordingState newState)
    {
        lock (_stateLock) { _state = newState; }
        Logger.Debug($"RecordingService: State → {newState}");
        StateChanged?.Invoke(this, newState);
    }

    /// <summary>
    /// 读取当前默认麦克风 / 扬声器的友好名称，触发 DeviceChanged 事件。
    /// 在初始化完成后及每次设备重建后调用（均在后台线程，可安全访问 COM）。
    /// </summary>
    private void NotifyDeviceChanged()
    {
        // 使用在 new WasapiCapture/WasapiLoopbackCapture() 后立即读取并缓存的设备名。
        // 比此处再调 GetDefaultAudioEndpoint() 更准确：创建实例时系统已确定目标设备，
        // 而 GetDefaultAudioEndpoint 在切换后短暂延迟内可能仍返回旧设备。
        string? micName = (_settings.Source == "Mic" || _settings.Source == "Mic&Speaker")
            ? _micDeviceName : null;
        string? spkName = (_settings.Source == "Speaker" || _settings.Source == "Mic&Speaker")
            ? _speakerDeviceName : null;

        Logger.Debug($"RecordingService: DeviceChanged mic={micName}, spk={spkName}");
        DeviceChanged?.Invoke(this, (micName, spkName));
    }

    // ════════════════════════════════════════════════════════════════════
    // M4A 转换
    // ════════════════════════════════════════════════════════════════════

    private async Task ConvertMp3ToM4AAsync(string mp3Path, string m4aPath)
    {
        if (!File.Exists(mp3Path))
            throw new FileNotFoundException("MP3 source not found", mp3Path);

        Logger.Debug($"RecordingService: ConvertMp3ToM4A {mp3Path} → {m4aPath}");

        await Task.Run(() =>
        {
            MediaFoundationApi.Startup();
            try
            {
                using var reader = new AudioFileReader(mp3Path);
                Logger.Debug($"RecordingService: MP3 format: {reader.WaveFormat}, duration: {reader.TotalTime}");

                var outDir = Path.GetDirectoryName(m4aPath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                // 使用用户配置的码率（单位 bps）
                int aacBitrate = _settings.Bitrate * 1000;
                MediaFoundationEncoder.EncodeToAac(reader.ToWaveProvider16(), m4aPath, aacBitrate);

                Logger.Debug($"RecordingService: M4A done: {m4aPath}");
            }
            finally
            {
                MediaFoundationApi.Shutdown();
            }
        });

        if (!File.Exists(m4aPath))
            throw new InvalidOperationException("M4A encoding produced no output file");

        Logger.Debug($"RecordingService: M4A size={new FileInfo(m4aPath).Length}");
    }

    // ════════════════════════════════════════════════════════════════════
    // 辅助方法
    // ════════════════════════════════════════════════════════════════════

    private void OnError(string message)
    {
        Logger.Error($"RecordingService.OnError: {message}");
        ErrorOccurred?.Invoke(this, message);
    }

    private static bool CheckWritePermission(string directory)
    {
        try
        {
            var test = Path.Combine(directory, $".quanta_{Guid.NewGuid():N}");
            File.WriteAllText(test, "x");
            File.Delete(test);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"RecordingService.CheckWritePermission: {ex.Message}");
            return false;
        }
    }

    private static bool CheckDiskSpace(string directory)
    {
        try
        {
            var root = Path.GetPathRoot(directory);
            if (string.IsNullOrEmpty(root)) return true;
            var drive = new DriveInfo(root);
            bool ok = drive.AvailableFreeSpace > 100L * 1024 * 1024;
            Logger.Debug($"RecordingService.CheckDiskSpace: free={drive.AvailableFreeSpace / (1024 * 1024)}MB, ok={ok}");
            return ok;
        }
        catch (Exception ex) { Logger.Warn($"RecordingService.CheckDiskSpace: {ex.Message}"); return true; }
    }

    private static void TryDeleteFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        try { File.Delete(path); Logger.Debug($"RecordingService: deleted {path}"); }
        catch (Exception ex) { Logger.Warn($"RecordingService: delete failed {path}: {ex.Message}"); }
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
            TryDeleteFile(_mp3OutputPath);
    }
}
