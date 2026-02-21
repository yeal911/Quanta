// ============================================================================
// 文件名：FileSearchProvider.cs
// 文件用途：文件搜索提供程序，在用户桌面和下载目录中搜索文件，支持模糊匹配。
//          仅搜索顶层目录（不递归子目录），每个目录最多扫描 300 个文件。
// ============================================================================

using System.Collections.Concurrent;
using System.IO;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 文件搜索提供程序
/// 在用户桌面和下载目录中搜索文件，支持模糊匹配。
/// 仅搜索顶层目录（不递归子目录），每个目录最多扫描 300 个文件。
/// </summary>
public class FileSearchProvider : ISearchProvider
{
    /// <summary>默认搜索目录列表：桌面和下载文件夹</summary>
    private readonly List<string> _searchDirectories = new()
    {
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads"
    };

    /// <summary>搜索提供程序名称</summary>
    public string Name => "Files";

    /// <summary>
    /// 在桌面和下载目录中搜索匹配的文件。
    /// 仅匹配文件名（不搜索内容），使用包含匹配（%keyword%）。
    /// </summary>
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<SearchResult>();

        if (string.IsNullOrWhiteSpace(query))
            return results.ToList();

        var queryLower = query.ToLower();

        var tasks = _searchDirectories.Select(async dir =>
        {
            if (!Directory.Exists(dir)) return;
            try
            {
                var files = await Task.Run(() =>
                    Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly).Take(300), cancellationToken);

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var fileName = Path.GetFileName(file);
                    var fileNameLower = fileName.ToLower();

                    if (fileNameLower.Contains(queryLower))
                    {
                        double score = fileNameLower == queryLower ? 1.0
                            : fileNameLower.StartsWith(queryLower) ? 0.9
                            : 0.8;

                        results.Add(new SearchResult
                        {
                            Title = fileName,
                            Path = file,
                            Subtitle = Path.GetDirectoryName(file) ?? "",
                            Type = SearchResultType.File,
                            Id = file,
                            MatchScore = score
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Failed to search directory: {dir} - {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
        return results.OrderByDescending(r => r.MatchScore).Take(8).ToList();
    }
}
