// =============================================================================
// 文件名: QRCodeService.cs
// 用途:   二维码生成服务，负责将文本内容转换为二维码图片。
//         使用 QRCoder 库生成 PNG 格式的二维码图像。
// =============================================================================

using System;
using System.IO;
using System.Windows.Media.Imaging;
using QRCoder;

namespace Quanta.Services;

/// <summary>
/// 二维码生成服务类，提供将文本内容转换为二维码图片的功能。
/// 支持生成 PNG 格式的二维码图像，可用于显示和复制到剪贴板。
/// </summary>
public static class QRCodeService
{
    /// <summary>
    /// 生成二维码图片并返回 BitmapImage。
    /// </summary>
    /// <param name="content">要编码的内容</param>
    /// <param name="pixelsPerModule">每个模块的像素大小，值越大二维码越大</param>
    /// <returns>二维码图片的 BitmapImage</returns>
    public static BitmapImage GenerateQRCode(string content, int pixelsPerModule = 20)
    {
        if (string.IsNullOrEmpty(content))
            return null!;

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(pixelsPerModule);

            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(qrCodeImage))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
            }

            return bitmap;
        }
        catch (Exception ex)
        {
            Logger.Error($"QRCode generation failed: {ex.Message}");
            return null!;
        }
    }

    /// <summary>
    /// 生成二维码图片的字节数组。
    /// </summary>
    /// <param name="content">要编码的内容</param>
    /// <param name="pixelsPerModule">每个模块的像素大小</param>
    /// <returns>PNG 格式的二维码图片字节数组</returns>
    public static byte[] GenerateQRCodeBytes(string content, int pixelsPerModule = 20)
    {
        if (string.IsNullOrEmpty(content))
            return Array.Empty<byte>();

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(pixelsPerModule);
        }
        catch (Exception ex)
        {
            Logger.Error($"QRCode generation failed: {ex.Message}");
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// 检查文本是否适合生成二维码（二维码有容量限制）。
    /// </summary>
    /// <param name="content">要检查的内容</param>
    /// <returns>是否适合生成二维码</returns>
    public static bool CanGenerateQRCode(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        // 二维码最大容量约为 2953 字节（数字模式下）
        // 为了兼容性，限制在 2000 字符以内
        return content.Length <= 2000;
    }

    /// <summary>
    /// 根据文本长度计算合适的二维码大小（pixelsPerModule）。
    /// 文本越长，二维码越复杂，需要更大的模块来保证扫描清晰度。
    /// </summary>
    /// <param name="content">要编码的内容</param>
    /// <returns>适合内容长度的像素模块大小</returns>
    public static int CalculatePixelsPerModule(string content)
    {
        int length = content?.Length ?? 0;

        // 分档计算二维码大小
        // 档位越低（内容越短），二维码越小；档位越高（内容越长），二维码越大
        return length switch
        {
            <= 100   => 15,   // 短文本：小尺寸
            <= 300   => 20,   // 中短文本：中等尺寸
            <= 600   => 25,   // 中等文本：较大尺寸
            <= 1000  => 30,   // 较长文本：大尺寸
            _        => 35    // 长文本：最大尺寸
        };
    }

    /// <summary>
    /// 根据文本长度自动计算大小并生成二维码。
    /// </summary>
    /// <param name="content">要编码的内容</param>
    /// <returns>二维码图片的 BitmapImage</returns>
    public static BitmapImage GenerateQRCodeAutoSize(string content)
    {
        int pixelsPerModule = CalculatePixelsPerModule(content);
        return GenerateQRCode(content, pixelsPerModule);
    }
}
