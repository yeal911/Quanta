// ============================================================================
// 文件名: RecordingService.Timer.cs
// 文件用途: RecordingService 的定时器回调部分。
//          包含进度上报（每秒）和 MP3 刷盘（每30秒）。
// ============================================================================

using System;
using System.IO;
using Quanta.Helpers;

namespace Quanta.Services;

public partial class RecordingService
{
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
}
