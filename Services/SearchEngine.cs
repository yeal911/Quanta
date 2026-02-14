using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Quanta.Helpers;
using Quanta.Models;

namespace Quanta.Services;

public interface ISearchProvider
{
    Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
    string Name { get; }
}

public class SearchEngine
{
    private readonly UsageTracker _usageTracker;
    private readonly WindowManager _windowManager;
    private readonly CommandRouter _commandRouter;
    private readonly List<ISearchProvider> _providers;
    private readonly List<SearchResult> _cachedRecentFiles = new();
    private readonly object _recentFilesLock = new();
    private List<CommandConfig> _customCommands = new();

    public SearchEngine(UsageTracker usageTracker, WindowManager windowManager, CommandRouter commandRouter)
    {
        _usageTracker = usageTracker;
        _windowManager = windowManager;
        _commandRouter = commandRouter;
        
        LoadCustomCommands();

        _providers = new List<ISearchProvider>
        {
            new ApplicationSearchProvider(),
            new FileSearchProvider(),
            new RecentFileSearchProvider(_usageTracker, UpdateRecentFiles),
            new WindowSearchProvider(_windowManager)
        };
    }

    private void LoadCustomCommands()
    {
        var config = ConfigLoader.Load();
        _customCommands = config.Commands;
    }

    public void ReloadCommands()
    {
        LoadCustomCommands();
    }

    private void UpdateRecentFiles(List<SearchResult> recentFiles)
    {
        lock (_recentFilesLock)
        {
            _cachedRecentFiles.Clear();
            _cachedRecentFiles.AddRange(recentFiles);
        }
    }

    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetDefaultResultsAsync(cancellationToken);

        var results = new ConcurrentBag<SearchResult>();

        // Check for custom commands first
        var customResults = SearchCustomCommands(query);
        foreach (var r in customResults)
            results.Add(r);

        // Check for built-in commands
        var commandResult = await _commandRouter.TryHandleCommandAsync(query);
        if (commandResult != null)
        {
            results.Add(commandResult);
            return results.OrderByDescending(r => r.MatchScore).ToList();
        }

        var tasks = _providers.Select(provider => Task.Run(async () =>
        {
            try
            {
                var providerResults = await provider.SearchAsync(query, cancellationToken);
                foreach (var result in providerResults)
                {
                    result.MatchScore = _usageTracker.CalculateUsageScore(result.Id, result.MatchScore);
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Provider {provider.Name} search failed", ex);
            }
        }, cancellationToken));

        await Task.WhenAll(tasks);
        return results.OrderByDescending(r => r.MatchScore).ThenByDescending(r => r.UsageCount).Take(15).ToList();
    }

    private List<SearchResult> SearchCustomCommands(string query)
    {
        var results = new List<SearchResult>();
        
        // Check if user is typing a command
        foreach (var cmd in _customCommands)
        {
            if (string.IsNullOrEmpty(query))
            {
                // Show all commands when empty
                results.Add(new SearchResult
                {
                    Id = $"cmd:{cmd.Keyword}",
                    Title = cmd.Keyword,
                    Subtitle = cmd.Name,
                    Type = SearchResultType.CustomCommand,
                    CommandConfig = cmd,
                    MatchScore = 1.0
                });
            }
            else if (query.StartsWith(cmd.Keyword, StringComparison.OrdinalIgnoreCase))
            {
                // Exact or partial match
                double score = query == cmd.Keyword ? 1.0 : 0.8;
                results.Add(new SearchResult
                {
                    Id = $"cmd:{cmd.Keyword}",
                    Title = cmd.Keyword,
                    Subtitle = cmd.Name,
                    Type = SearchResultType.CustomCommand,
                    CommandConfig = cmd,
                    MatchScore = score
                });
            }
            else if (cmd.Keyword.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                // Prefix match
                results.Add(new SearchResult
                {
                    Id = $"cmd:{cmd.Keyword}",
                    Title = cmd.Keyword,
                    Subtitle = cmd.Name,
                    Type = SearchResultType.CustomCommand,
                    CommandConfig = cmd,
                    MatchScore = 0.9
                });
            }
        }

        return results;
    }

    private async Task<List<SearchResult>> GetDefaultResultsAsync(CancellationToken cancellationToken)
    {
        var results = new ConcurrentBag<SearchResult>();
        
        // Show custom commands first
        foreach (var cmd in _customCommands.Take(5))
        {
            var typeName = cmd.Type.ToLower() switch
            {
                "url" => "🌐 " + cmd.Name,
                "program" => "📦 " + cmd.Name,
                "directory" => "📁 " + cmd.Name,
                "shell" => "⚡ " + cmd.Name,
                "calculator" => "🔢 " + cmd.Name,
                _ => cmd.Name
            };
            
            results.Add(new SearchResult
            {
                Id = $"cmd:{cmd.Keyword}",
                Title = cmd.Keyword,
                Subtitle = typeName,
                Type = SearchResultType.CustomCommand,
                CommandConfig = cmd,
                MatchScore = 0.5
            });
        }

        lock (_recentFilesLock)
        {
            foreach (var file in _cachedRecentFiles.Take(5))
                results.Add(file);
        }

        var windows = await Task.Run(() => _windowManager.GetVisibleWindows(), cancellationToken);
        foreach (var window in windows.Take(3))
            results.Add(window);

        return results.ToList();
    }

    public static double CalculateFuzzyScore(string query, string target)
    {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target))
            return 0;

        query = query.ToLower();
        target = target.ToLower();

        // Exact contains match
        if (target.Contains(query)) return 1.0;
        
        // Starts with match
        if (target.StartsWith(query)) return 0.9;

        // Character-by-character fuzzy match
        int matchedChars = 0;
        int targetIndex = 0;
        foreach (char c in query)
        {
            int foundIndex = target.IndexOf(c, targetIndex);
            if (foundIndex >= 0)
            {
                matchedChars++;
                targetIndex = foundIndex + 1;
            }
        }

        return matchedChars > 0 ? (double)matchedChars / query.Length * 0.7 : 0;
    }

    public async Task<bool> ExecuteResultAsync(SearchResult result)
    {
        switch (result.Type)
        {
            case SearchResultType.Application:
            case SearchResultType.File:
            case SearchResultType.RecentFile:
                return await LaunchFileAsync(result);
            case SearchResultType.Window:
                return _windowManager.ActivateWindow(result);
            case SearchResultType.Command:
            case SearchResultType.Calculator:
            case SearchResultType.WebSearch:
                return true;
            case SearchResultType.CustomCommand:
                return await ExecuteCustomCommandAsync(result, "");
            default:
                return false;
        }
    }

    public async Task<bool> ExecuteCustomCommandAsync(SearchResult result, string param)
    {
        if (result.CommandConfig == null) return false;

        var cmd = result.CommandConfig;
        try
        {
            switch (cmd.Type.ToLower())
            {
                case "url":
                    var url = cmd.Path + Uri.EscapeDataString(param);
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    _usageTracker.RecordUsage(result.Id);
                    return true;

                case "program":
                    var programPath = cmd.Path;
                    var args = cmd.Arguments.Replace("{param}", param).Replace("{query}", param);
                    var psi = new ProcessStartInfo
                    {
                        FileName = programPath,
                        Arguments = args,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    _usageTracker.RecordUsage(result.Id);
                    return true;

                case "directory":
                    var dirPath = cmd.Path.Replace("{param}", param).Replace("{query}", param);
                    if (System.IO.Directory.Exists(dirPath))
                    {
                        Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = dirPath, UseShellExecute = true });
                        _usageTracker.RecordUsage(result.Id);
                        return true;
                    }
                    Logger.Warn($"Directory not found: {dirPath}");
                    return false;

                case "shell":
                    {
                        var shellCmd = cmd.Arguments.Replace("{param}", param).Replace("{query}", param);
                        var shellPsi = new ProcessStartInfo
                        {
                            FileName = cmd.Path,
                            Arguments = shellCmd,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        using var process = Process.Start(shellPsi);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                        }
                        _usageTracker.RecordUsage(result.Id);
                        return true;
                    }

                case "calculator":
                    var calcResult = CalculateInternal(cmd.Arguments.Replace("{param}", param).Replace("{query}", param));
                    return true;

                default:
                    Logger.Warn($"Unknown command type: {cmd.Type}");
                    return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to execute command: {cmd.Keyword}", ex);
            return false;
        }
    }

    private string CalculateInternal(string expression)
    {
        try
        {
            string sanitized = Regex.Replace(expression, @"[^0-9+\-*/().%^]", "");
            var computed = new System.Data.DataTable().Compute(sanitized, null);
            return computed.ToString() ?? "Error";
        }
        catch (Exception ex)
        {
            Logger.Warn($"Calculation error: {ex.Message}");
            return "Error";
        }
    }

    private async Task<bool> LaunchFileAsync(SearchResult result)
    {
        try
        {
            var psi = new ProcessStartInfo { FileName = result.Path, UseShellExecute = true };
            Process.Start(psi);
            _usageTracker.RecordUsage(result.Id);
            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }
}

public class ApplicationSearchProvider : ISearchProvider
{
    private List<SearchResult>? _cachedApps;
    private DateTime _cacheTime;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public string Name => "Applications";

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

public class FileSearchProvider : ISearchProvider
{
    private readonly List<string> _searchDirectories = new()
    {
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads"
    };

    public string Name => "Files";

    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<SearchResult>();

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
                    var score = SearchEngine.CalculateFuzzyScore(query, fileName);
                    if (score > 0)
                    {
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
                Logger.Error($"Failed to search directory: {dir}", ex);
            }
        });

        await Task.WhenAll(tasks);
        return results.OrderByDescending(r => r.MatchScore).Take(8).ToList();
    }
}

public class RecentFileSearchProvider : ISearchProvider
{
    private readonly UsageTracker _usageTracker;
    private readonly Action<List<SearchResult>> _onRecentFilesUpdated;
    private readonly List<SearchResult> _recentFiles = new();

    public string Name => "Recent Files";

    public RecentFileSearchProvider(UsageTracker usageTracker, Action<List<SearchResult>> onRecentFilesUpdated)
    {
        _usageTracker = usageTracker;
        _onRecentFilesUpdated = onRecentFilesUpdated;
    }

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

public class WindowSearchProvider : ISearchProvider
{
    private readonly WindowManager _windowManager;

    public string Name => "Windows";

    public WindowSearchProvider(WindowManager windowManager) => _windowManager = windowManager;

    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return _windowManager.GetVisibleWindows()
                .Select(window => { window.MatchScore = SearchEngine.CalculateFuzzyScore(query, window.Title); return window; })
                .Where(r => r.MatchScore > 0)
                .OrderByDescending(r => r.MatchScore)
                .Take(5)
                .ToList();
        }, cancellationToken);
    }
}
