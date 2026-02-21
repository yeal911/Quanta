using System.Text.Json;

namespace Quanta.Core.Constants;

/// <summary>
/// 公共 JSON 序列化默认选项
/// </summary>
public static class JsonDefaults
{
    /// <summary>
    /// 标准读写选项：缩进输出 + 大小写不敏感
    /// 用于配置文件、命令导入导出
    /// </summary>
    public static readonly JsonSerializerOptions Standard = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 精简写入选项：缩进输出，大小写敏感
    /// 用于 API 响应解析
    /// </summary>
    public static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true
    };
}
