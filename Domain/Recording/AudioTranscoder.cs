// ============================================================================
// 文件名: AudioTranscoder.cs
// 文件用途: 音频格式转码工具（无状态，线程安全）。
//          目前支持 MP3 → M4A(AAC) 转换，使用 Windows Media Foundation。
// ============================================================================

using System.IO;
using System.Threading.Tasks;
using NAudio.MediaFoundation;
using NAudio.Wave;
using Quanta.Helpers;
using Quanta.Models;

namespace Quanta.Services;

internal static class AudioTranscoder
{
    /// <summary>
    /// 将 MP3 文件转码为 M4A(AAC) 格式。
    /// 需要在后台线程调用（内部通过 Task.Run 处理）。
    /// </summary>
    /// <param name="mp3Path">源 MP3 文件路径</param>
    /// <param name="m4aPath">目标 M4A 文件路径</param>
    /// <param name="settings">录音设置（用于读取码率）</param>
    internal static async Task ConvertMp3ToM4AAsync(string mp3Path, string m4aPath, RecordingSettings settings)
    {
        if (!File.Exists(mp3Path))
            throw new FileNotFoundException("MP3 source not found", mp3Path);

        Logger.Debug($"AudioTranscoder: ConvertMp3ToM4A {mp3Path} → {m4aPath}");

        await Task.Run(() =>
        {
            MediaFoundationApi.Startup();
            try
            {
                using var reader = new AudioFileReader(mp3Path);
                Logger.Debug($"AudioTranscoder: MP3 format: {reader.WaveFormat}, duration: {reader.TotalTime}");

                var outDir = Path.GetDirectoryName(m4aPath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                // 使用用户配置的码率（单位 bps）
                int aacBitrate = settings.Bitrate * 1000;
                MediaFoundationEncoder.EncodeToAac(reader.ToWaveProvider16(), m4aPath, aacBitrate);

                Logger.Debug($"AudioTranscoder: M4A done: {m4aPath}");
            }
            finally
            {
                MediaFoundationApi.Shutdown();
            }
        });

        if (!File.Exists(m4aPath))
            throw new InvalidOperationException("M4A encoding produced no output file");

        Logger.Debug($"AudioTranscoder: M4A size={new FileInfo(m4aPath).Length}");
    }
}
