// ============================================================================
// 文件名: IQRCodeService.cs
// 文件描述: 二维码生成服务接口，定义二维码生成功能的抽象层
// ============================================================================

using System.Windows.Media.Imaging;

namespace Quanta.Core.Interfaces;

/// <summary>
/// 二维码生成服务接口，提供将文本内容转换为二维码图片的功能
/// </summary>
public interface IQRCodeService
{
    /// <summary>
    /// 生成二维码图片并返回 BitmapImage
    /// </summary>
    /// <param name="content">要编码的内容</param>
    /// <param name="pixelsPerModule">每个模块的像素大小</param>
    /// <returns>二维码图片的 BitmapImage</returns>
    BitmapImage GenerateQRCode(string content, int pixelsPerModule = 20);

    /// <summary>
    /// 生成二维码图片的字节数组
    /// </summary>
    /// <param name="content">要编码的内容</param>
    /// <param name="pixelsPerModule">每个模块的像素大小</param>
    /// <returns>PNG 格式的二维码图片字节数组</returns>
    byte[] GenerateQRCodeBytes(string content, int pixelsPerModule = 20);

    /// <summary>
    /// 检查文本是否适合生成二维码
    /// </summary>
    /// <param name="content">要检查的内容</param>
    /// <returns>是否适合生成二维码</returns>
    bool CanGenerateQRCode(string content);

    /// <summary>
    /// 根据文本长度计算合适的二维码大小
    /// </summary>
    /// <param name="content">要编码的内容</param>
    /// <returns>适合内容长度的像素模块大小</returns>
    int CalculatePixelsPerModule(string content);

    /// <summary>
    /// 根据文本长度自动计算大小并生成二维码
    /// </summary>
    /// <param name="content">要编码的内容</param>
    /// <returns>二维码图片的 BitmapImage</returns>
    BitmapImage GenerateQRCodeAutoSize(string content);
}
