// ============================================================================
// 文件名：FileSearchProvider.cs
// 文件用途：文件搜索提供程序，在用户桌面和下载目录中搜索文件，支持模糊匹配。
//          仅搜索顶层目录（不递归子目录），每个目录最多扫描 300 个文件。
// ============================================================================

using System.Collections.Concurrent;
using System.IO;
using Quanta.Helpers;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 文件搜索提供程序
/// 在用户配置的目录中搜索文件，支持模糊匹配。
/// </summary>
public class FileSearchProvider : ISearchProvider
{
    /// <summary>搜索提供程序名称</summary>
    public string Name => "Files";

    /// <summary>
    /// 在配置的目录中搜索匹配的文件。
    /// 仅匹配文件名（不搜索内容），使用包含匹配。
    /// </summary>
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<SearchResult>();

        if (string.IsNullOrWhiteSpace(query))
            return results.ToList();

        // 从配置读取文件搜索设置
        var config = ConfigLoader.Load();
        var fsSettings = config.FileSearchSettings;

        // 如果未启用，直接返回空结果
        if (!fsSettings.Enabled)
            return results.ToList();

        // 获取搜索目录（默认：桌面+下载）
        var searchDirs = fsSettings.Directories ?? new List<string>();
        if (searchDirs.Count == 0)
        {
            searchDirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            searchDirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads");
        }

        var maxFiles = fsSettings.MaxFiles > 0 ? fsSettings.MaxFiles : 300;
        var maxResults = fsSettings.MaxResults > 0 ? fsSettings.MaxResults : 8;
        var recursive = fsSettings.Recursive;
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var queryLower = query.ToLower();

        var tasks = searchDirs.Select(async dir =>
        {
            if (!Directory.Exists(dir)) return;
            try
            {
                var files = await Task.Run(() =>
                    Directory.EnumerateFiles(dir, "*", searchOption).Take(maxFiles), cancellationToken);

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
        return results.OrderByDescending(r => r.MatchScore).Take(maxResults).ToList();
    }
}
