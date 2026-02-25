using System.Diagnostics;

namespace Quanta.WinFormsLite;

public sealed class SearchService
{
    private readonly AppConfig _config;

    public SearchService(AppConfig config)
    {
        _config = config;
    }

    public List<SearchResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var q = query.Trim();
        var parts = q.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var keyword = parts[0].ToLowerInvariant();
        var param = parts.Length > 1 ? parts[1] : string.Empty;

        var results = new List<SearchResult>();

        foreach (var cmd in _config.Commands)
        {
            var key = cmd.Keyword.ToLowerInvariant();
            if (key.Contains(keyword) || keyword.Contains(key))
            {
                results.Add(new SearchResult
                {
                    Title = string.IsNullOrWhiteSpace(cmd.Name) ? cmd.Keyword : cmd.Name,
                    Subtitle = cmd.Keyword,
                    Payload = BuildCommandPayload(cmd, param),
                    Kind = SearchKind.Command,
                    Score = key == keyword ? 1.0 : 0.8
                });
            }
        }

        if (_config.AppSettings.EnableFileSearch)
        {
            var roots = _config.AppSettings.FileSearchRoots.Count > 0
                ? _config.AppSettings.FileSearchRoots
                :
                [
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                ];

            var scanLimit = _config.AppSettings.FileScanLimit > 0 ? _config.AppSettings.FileScanLimit : 300;
            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly).Take(scanLimit))
                    {
                        var name = Path.GetFileName(file);
                        if (!name.Contains(q, StringComparison.OrdinalIgnoreCase)) continue;

                        results.Add(new SearchResult
                        {
                            Title = name,
                            Subtitle = Path.GetDirectoryName(file) ?? string.Empty,
                            Payload = file,
                            Kind = SearchKind.File,
                            Score = name.Equals(q, StringComparison.OrdinalIgnoreCase) ? 0.9 : 0.7
                        });
                    }
                }
                catch
                {
                    // 保持轻量：搜索目录失败时忽略，避免中断主流程
                }
            }
        }

        var maxResults = _config.AppSettings.MaxResults > 0 ? _config.AppSettings.MaxResults : 8;
        return results.OrderByDescending(r => r.Score).Take(maxResults).ToList();
    }

    public static void Execute(SearchResult result)
    {
        if (result.Kind == SearchKind.File)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = result.Payload,
                UseShellExecute = true
            });
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = result.Payload,
            UseShellExecute = true
        });
    }

    private static string BuildCommandPayload(CommandItem cmd, string param)
    {
        if (cmd.Type.Equals("Url", StringComparison.OrdinalIgnoreCase))
        {
            return cmd.Path.Replace("{param}", Uri.EscapeDataString(param));
        }

        return cmd.Path.Replace("{param}", param);
    }
}
