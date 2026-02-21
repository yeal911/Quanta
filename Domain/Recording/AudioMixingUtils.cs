// ============================================================================
// 文件名: AudioMixingUtils.cs
// 文件用途: 音频混音纯函数工具集（无状态，线程安全）。
//          提供声道降混和 PCM 逐样本混音功能，供 RecordingService 各模块共用。
// ============================================================================

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Quanta.Services;

internal static class AudioMixingUtils
{
    /// <summary>
    /// 声道降混 / 升混。将输入 ISampleProvider 调整为目标声道数。
    /// </summary>
    internal static ISampleProvider DownmixChannels(ISampleProvider sp, int targetChannels)
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

    /// <summary>
    /// 将两段 16-bit PCM 数据逐样本相加（带削波），长度不同时短的用零填充。
    /// </summary>
    internal static byte[] MixPcm16(byte[] a, int aLen, byte[] b, int bLen)
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
            result[i] = (byte)(mixed & 0xFF);
            result[i + 1] = (byte)((mixed >> 8) & 0xFF);
        }
        return result;
    }
}
