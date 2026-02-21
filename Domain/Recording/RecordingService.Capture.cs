// ============================================================================
// 文件名: RecordingService.Capture.cs
// 文件用途: RecordingService 的 WASAPI 设备管理部分。
//          包含初始化采集、设备停止回调、热拔插重启逻辑。
// ============================================================================

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Lame;
using Quanta.Helpers;
using Quanta.Core.Interfaces;
using Quanta.Models;

namespace Quanta.Services;

public partial class RecordingService
{
    // ════════════════════════════════════════════════════════════════════
    // 初始化
    // ════════════════════════════════════════════════════════════════════

    private bool InitializeAndStartCapture()
    {
        var source = _settings.Source;
        int channels = Math.Min(Math.Max(_settings.Channels, 1), 2); // clamp 1-2
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
                _micCapture.DataAvailable += OnMicDataAvailable;
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
                    sp = AudioMixingUtils.DownmixChannels(sp, channels);
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

                _loopbackCapture.DataAvailable += OnLoopbackDataAvailable;
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
        _startTime = DateTime.Now;
        _pausedDuration = TimeSpan.Zero;
        _totalBytesMic = 0;
        _totalBytesLoopback = 0;
        _totalBytesWritten = 0;

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
    // 设备停止回调 & 热拔插重启
    // ════════════════════════════════════════════════════════════════════

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
                old.DataAvailable -= OnLoopbackDataAvailable;
                old.RecordingStopped -= OnCaptureStopped;
                try { old.Dispose(); } catch { }
            }

            // 2. 创建新实例（自动绑定当前默认输出设备），立即读取设备名
            var newCapture = new WasapiLoopbackCapture();
            try
            {
                using var e2 = new MMDeviceEnumerator();
                using var d2 = e2.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _speakerDeviceName = d2.FriendlyName;
            }
            catch { _speakerDeviceName = null; }
            var nativeFmt = newCapture.WaveFormat;
            int channels = _writeFormat.Channels;
            int sampleRate = _writeFormat.SampleRate;
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
                sp = AudioMixingUtils.DownmixChannels(sp, channels);
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
                _loopbackBuffer = newBuffer;
                _loopbackConverted = newConverted;
            }

            // 5. 绑定事件、启动新采集
            // 先清标志再 StartRecording：若新实例立刻失败（设备仍在重置），
            // OnCaptureStopped 能正常调度下一次重试而不被 Interlocked 阻挡。
            newCapture.DataAvailable += OnLoopbackDataAvailable;
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
                old.DataAvailable -= OnMicDataAvailable;
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
            newCapture.DataAvailable += OnMicDataAvailable;
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
    // 设备信息通知
    // ════════════════════════════════════════════════════════════════════

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
}
