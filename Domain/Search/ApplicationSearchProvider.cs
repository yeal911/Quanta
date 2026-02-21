// ============================================================================
// 文件名：ApplicationSearchProvider.cs
// 文件用途：应用程序搜索提供程序，从 Windows 开始菜单扫描已安装应用（.lnk 快捷方式）。
//          带缓存（5 分钟有效期）的模糊搜索功能。
// ============================================================================

using System.IO;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 应用程序搜索提供程序
/// 从 Windows 开始菜单目录中扫描已安装的应用程序（.lnk 快捷方式），
/// 并提供带缓存的模糊搜索功能。缓存有效期为 5 分钟。
/// </summary>
public class ApplicationSearchProvider : ISearchProvider
{
    /// <summary>已安装应用程序的缓存列表</summary>
    private List<SearchResult>? _cachedApps;

    /// <summary>缓存创建时间</summary>
    private DateTime _cacheTime;

    /// <summary>缓存有效时长（默认 5 分钟）</summary>
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>搜索提供程序名称</summary>
    public string Name => "Applications";

    /// <summary>
    /// 根据查询关键词搜索已安装的应用程序。
    /// 如果缓存过期或为空，会重新扫描开始菜单目录加载应用列表。
    /// </summary>
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (_cachedApps == null || DateTime.Now - _cacheTime > _cacheDuration)
        {
            _cachedApps = await Task.Run(LoadInstalledApplications, cancellationToken);
            _cacheTime = DateTime.Now;
        }

        return _cachedApps
            .Select(app => { app.MatchScore = SearchEngine.CalculateFuzzyScore(query, app.Title); return app; })
            .Where(r => r.MatchScore > 0)
            .OrderByDescending(r => r.MatchScore)
            .Take(8)
            .ToList();
    }

    /// <summary>
    /// 从系统开始菜单目录加载已安装的应用程序。
    /// 扫描公共开始菜单和用户开始菜单中的 .lnk 快捷方式文件，每个目录最多 500 个。
    /// </summary>
    private List<SearchResult> LoadInstalledApplications()
    {
        var apps = new List<SearchResult>();

        var startMenuPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
        };

        foreach (var path in startMenuPaths)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    foreach (var file in Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories).Take(500))
                    {
                        try
                        {
                            apps.Add(new SearchResult
                            {
                                Title = Path.GetFileNameWithoutExtension(file),
                                Path = file,
                                Type = SearchResultType.Application,
                                Id = file
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug($"Failed to load app: {file} - {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to scan start menu: {path}", ex);
                }
            }
        }

        return apps;
    }
}
