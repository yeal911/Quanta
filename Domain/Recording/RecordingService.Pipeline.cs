// ============================================================================
// 文件名: RecordingService.Pipeline.cs
// 文件用途: RecordingService 的音频数据处理管线部分。
//          包含 DataAvailable 回调、混音写入、缓冲区排干。
// ============================================================================

using System;
using NAudio.Wave;
using Quanta.Helpers;
using Quanta.Core.Interfaces;

namespace Quanta.Services;

public partial class RecordingService
{
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
                            var mixed = AudioMixingUtils.MixPcm16(readBuf, bytesRead, micBuf, micRead);
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
                                var mixed = AudioMixingUtils.MixPcm16(readBuf, bytesRead, micBuf, micRead);
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
}
