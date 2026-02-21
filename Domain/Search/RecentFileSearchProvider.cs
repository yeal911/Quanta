// ============================================================================
// 文件名：RecentFileSearchProvider.cs
// 文件用途：最近文件搜索提供程序，维护最近使用文件列表（最多 30 条），支持模糊搜索。
//          当有新文件被添加时，通过回调通知外部更新 UI。
// ============================================================================

using System.IO;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 最近文件搜索提供程序
/// 维护一个最近使用过的文件列表（最多 30 条），支持模糊搜索。
/// 当有新文件被添加时，通过回调通知外部更新 UI。
/// </summary>
public class RecentFileSearchProvider : ISearchProvider
{
    private readonly UsageTracker _usageTracker;
    private readonly Action<List<SearchResult>> _onRecentFilesUpdated;
    private readonly List<SearchResult> _recentFiles = new();

    /// <summary>搜索提供程序名称</summary>
    public string Name => "Recent Files";

    public RecentFileSearchProvider(UsageTracker usageTracker, Action<List<SearchResult>> onRecentFilesUpdated)
    {
        _usageTracker = usageTracker;
        _onRecentFilesUpdated = onRecentFilesUpdated;
    }

    /// <summary>
    /// 在最近使用的文件列表中搜索匹配项。
    /// </summary>
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return _recentFiles
                .Select(file => { file.MatchScore = SearchEngine.CalculateFuzzyScore(query, file.Title); return file; })
                .Where(r => r.MatchScore > 0)
                .OrderByDescending(r => r.MatchScore)
                .Take(5)
                .ToList();
        }, cancellationToken);
    }

    /// <summary>
    /// 将文件添加到最近使用列表。
    /// 如果文件已存在则移动到列表顶部；列表超过 30 条时自动移除最旧的记录。
    /// </summary>
    public void AddRecentFile(string filePath)
    {
        if (!File.Exists(filePath)) return;
        var existing = _recentFiles.FirstOrDefault(f => f.Path == filePath);
        if (existing != null) _recentFiles.Remove(existing);
        _recentFiles.Insert(0, new SearchResult
        {
            Title = Path.GetFileName(filePath),
            Path = filePath,
            Subtitle = Path.GetDirectoryName(filePath) ?? "",
            Type = SearchResultType.RecentFile,
            Id = filePath
        });
        while (_recentFiles.Count > 30) _recentFiles.RemoveAt(_recentFiles.Count - 1);
        _onRecentFilesUpdated?.Invoke(_recentFiles);
    }
}
