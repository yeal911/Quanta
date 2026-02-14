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
    private readonly CommandRouter _commandRouter;
    private List<CommandConfig> _customCommands = new();

    public SearchEngine(UsageTracker usageTracker, CommandRouter commandRouter)
    {
        _usageTracker = usageTracker;
        _commandRouter = commandRouter;
        
        LoadCustomCommands();
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
        }

        var finalList = results.OrderByDescending(r => r.MatchScore).ThenByDescending(r => r.UsageCount).Take(10).ToList();
        for (int i = 0; i < finalList.Count; i++) finalList[i].Index = i;
        return finalList;
    }

    private List<SearchResult> SearchCustomCommands(string query)
    {
        var results = new List<SearchResult>();
        int index = 0;
        
        // Check if user is typing a command
        foreach (var cmd in _customCommands)
        {
            if (string.IsNullOrEmpty(query))
            {
                // Show all commands when empty
                results.Add(new SearchResult
                {
                    Index = index++,
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
                    Index = index++,
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
                    Index = index++,
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
        int index = 0;
        
        // Show custom commands
        foreach (var cmd in _customCommands.Take(8))
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
                Index = index++,
                Id = $"cmd:{cmd.Keyword}",
                Title = cmd.Keyword,
                Subtitle = typeName,
                Type = SearchResultType.CustomCommand,
                CommandConfig = cmd,
                MatchScore = 0.5
            });
        }

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
            case SearchResultType.Command:
            case SearchResultType.Calculator:
            case SearchResultType.WebSearch:
            case SearchResultType.Window:
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
        
        // Check if command is enabled
        if (!cmd.Enabled)
        {
            Logger.Warn($"Command is disabled: {cmd.Keyword}");
            return false;
        }

        try
        {
            // Support multiple parameter placeholders
            var processedPath = cmd.Path
                .Replace("{param}", param)
                .Replace("{query}", param)
                .Replace("%p", param);
            
            var processedArgs = cmd.Arguments
                .Replace("{param}", param)
                .Replace("{query}", param)
                .Replace("%p", param);

            switch (cmd.Type.ToLower())
            {
                case "url":
                    var url = processedPath;
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    _usageTracker.RecordUsage(result.Id);
                    return true;

                case "program":
                    var programPath = processedPath;
                    
                    // Check if file exists
                    if (!File.Exists(programPath) && !Path.IsPathRooted(programPath))
                    {
                        // Try to find in PATH
                        programPath = FindInPath(programPath);
                    }
                    
                    var psi = new ProcessStartInfo
                    {
                        FileName = programPath,
                        Arguments = processedArgs,
                        UseShellExecute = !cmd.RunHidden,
                        WorkingDirectory = string.IsNullOrEmpty(cmd.WorkingDirectory) 
                            ? Path.GetDirectoryName(programPath) 
                            : cmd.WorkingDirectory,
                        CreateNoWindow = cmd.RunHidden
                    };
                    
                    // Handle run as admin
                    if (cmd.RunAsAdmin)
                    {
                        psi.Verb = "runas";
                    }
                    
                    Process.Start(psi);
                    _usageTracker.RecordUsage(result.Id);
                    return true;

                case "directory":
                    var dirPath = processedPath;
                    if (System.IO.Directory.Exists(dirPath))
                    {
                        Process.Start(new ProcessStartInfo 
                        { 
                            FileName = "explorer.exe", 
                            Arguments = dirPath, 
                            UseShellExecute = true,
                            WorkingDirectory = cmd.WorkingDirectory
                        });
                        _usageTracker.RecordUsage(result.Id);
                        return true;
                    }
                    Logger.Warn($"Directory not found: {dirPath}");
                    return false;

                case "shell":
                    {
                        var shellCmd = processedPath;
                        if (!string.IsNullOrEmpty(processedArgs))
                            shellCmd += " " + processedArgs;
                        
                        // Optimize: Use cmd.exe directly and don't wait for output (faster execution)
                        var shellPsi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {shellCmd}",
                            UseShellExecute = !cmd.RunHidden,
                            CreateNoWindow = cmd.RunHidden,
                            WorkingDirectory = string.IsNullOrEmpty(cmd.WorkingDirectory) 
                                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) 
                                : cmd.WorkingDirectory
                        };
                        
                        if (cmd.RunAsAdmin)
                        {
                            shellPsi.Verb = "runas";
                            shellPsi.UseShellExecute = true;
                        }
                        
                        // Start process without waiting (fire and forget for speed)
                        Process.Start(shellPsi);
                        _usageTracker.RecordUsage(result.Id);
                        return true;
                    }

                case "calculator":
                    var calcResult = CalculateInternal(processedPath);
                    Logger.Log($"Calculator result: {calcResult}");
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

    /// <summary>
    /// Find executable in PATH environment variable (with caching)
    /// </summary>
    private static readonly Dictionary<string, string?> PathCache = new(StringComparer.OrdinalIgnoreCase);
    
    private string? FindInPath(string executable)
    {
        if (string.IsNullOrEmpty(executable)) return null;
        
        // Check cache first
        if (PathCache.TryGetValue(executable, out var cached))
            return cached;
        
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv)) return null;
        
        string? result = null;
        foreach (var path in pathEnv.Split(';'))
        {
            try
            {
                if (string.IsNullOrEmpty(path)) continue;
                
                var fullPath = Path.Combine(path, executable);
                if (File.Exists(fullPath))
                {
                    result = fullPath;
                    break;
                }
                    
                // Also check with .exe extension
                fullPath = Path.Combine(path, executable + ".exe");
                if (File.Exists(fullPath))
                {
                    result = fullPath;
                    break;
                }
            }
            catch { }
        }
        
        // Cache the result
        PathCache[executable] = result;
        return result;
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
