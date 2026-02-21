// ============================================================================
// 文件名: IClipboardHistoryService.cs
// 文件描述: 剪贴板历史服务接口，定义剪贴板历史功能的抽象层
// ============================================================================

using Quanta.Models;

namespace Quanta.Core.Interfaces;

/// <summary>
/// 剪贴板历史服务接口，提供剪贴板历史记录和搜索功能
/// </summary>
public interface IClipboardHistoryService
{
    /// <summary>
    /// 添加一条剪贴板内容
    /// </summary>
    /// <param name="text">剪贴板文本内容</param>
    void Add(string text);

    /// <summary>
    /// 按关键字搜索历史记录
    /// </summary>
    /// <param name="keyword">搜索关键字</param>
    /// <returns>匹配的搜索结果列表</returns>
    List<SearchResult> Search(string keyword);
}
