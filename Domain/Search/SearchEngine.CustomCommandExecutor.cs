// ============================================================================
// 文件名：SearchEngine.CustomCommandExecutor.cs
// 文件用途：SearchEngine 自定义命令执行职责拆分。
// ============================================================================

using Quanta.Helpers;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Quanta.Models;

namespace Quanta.Services;

public partial class SearchEngine
{
    public async Task<bool> ExecuteCustomCommandAsync(SearchResult result, string param)
    {
        Logger.Debug($"ExecuteCustomCommandAsync called: Keyword={result.CommandConfig?.Keyword}, Type={result.CommandConfig?.Type}, Param='{param}'");

        if (result.CommandConfig == null)
        {
            Logger.Debug("ExecuteCustomCommandAsync: CommandConfig is null!");
            return false;
        }

        var cmd = result.CommandConfig;

        // 检查命令是否已启用
        if (!cmd.Enabled)
        {
            Logger.Warn($"Command is disabled: {cmd.Keyword}");
            return false;
        }

        Logger.Debug($"ExecuteCustomCommandAsync: Executing {cmd.Type} with Path='{cmd.Path}', Arguments='{cmd.Arguments}'");

        try
        {
            // 替换路径和参数中的参数占位符（支持自定义占位符和内置占位符）
            var placeholder = !string.IsNullOrEmpty(cmd.ParamPlaceholder) ? cmd.ParamPlaceholder : "{param}";
            var processedPath = cmd.Path
                .Replace(placeholder, param)
                .Replace("{param}", param)
                .Replace("{query}", param)
                .Replace("{%p}", param);

            var processedArgs = cmd.Arguments
                .Replace(placeholder, param)
                .Replace("{param}", param)
                .Replace("{query}", param)
                .Replace("{%p}", param);

            Logger.Debug($"ExecuteCustomCommandAsync: After replace - processedPath='{processedPath}', processedArgs='{processedArgs}'");

            switch (cmd.Type.ToLower())
            {
                // URL 类型：使用默认浏览器打开网址
                case "url":
                    var url = processedPath;
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    _usageTracker.RecordUsage(result.Id);
                    return true;

                // 程序类型：启动可执行程序
                case "program":
                    var programPath = processedPath;

                    // 如果文件不存在且不是绝对路径，尝试在 PATH 环境变量中查找
                    if (!File.Exists(programPath) && !Path.IsPathRooted(programPath))
                    {
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

                    // 以管理员身份运行
                    if (cmd.RunAsAdmin)
                    {
                        psi.Verb = "runas";
                    }

                    Process.Start(psi);
                    _usageTracker.RecordUsage(result.Id);
                    return true;

                // 目录类型：使用资源管理器打开文件夹
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

                // Shell 类型：通过 PowerShell 执行命令行命令
                case "shell":
                    {
                        var shellCmd = processedPath;
                        if (!string.IsNullOrEmpty(processedArgs))
                            shellCmd += " " + processedArgs;

                        Logger.Debug($"ExecuteCustomCommandAsync Shell: shellCmd='{shellCmd}', RunHidden={cmd.RunHidden}");

                        ProcessStartInfo shellPsi;
                        if (cmd.RunHidden)
                        {
                            // 隐藏窗口执行
                            shellPsi = new ProcessStartInfo
                            {
                                FileName = "powershell.exe",
                                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{shellCmd}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                WorkingDirectory = string.IsNullOrEmpty(cmd.WorkingDirectory)
                                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                    : cmd.WorkingDirectory
                            };
                        }
                        else
                        {
                            // 显示窗口执行（以便查看输出）
                            shellPsi = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c {shellCmd}",
                                UseShellExecute = true,
                                CreateNoWindow = false,
                                WorkingDirectory = string.IsNullOrEmpty(cmd.WorkingDirectory)
                                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                    : cmd.WorkingDirectory
                            };
                        }

                        Logger.Debug($"ExecuteCustomCommandAsync Shell: Starting process with FileName='{shellPsi.FileName}', Arguments='{shellPsi.Arguments}'");

                        // 启动进程后不等待完成（即发即忘，提升响应速度）
                        var process = Process.Start(shellPsi);
                        Logger.Debug($"ExecuteCustomCommandAsync Shell: Process started, Id={process?.Id}");
                        _usageTracker.RecordUsage(result.Id);
                        return true;
                    }

                // 计算器类型：对表达式求值
                case "calculator":
                    var calcResult = CalculateInternal(processedPath);
                    Logger.Debug($"Calculator result: {calcResult}");
                    return true;

                // 系统操作类型：设置、关于、切换语言等
                case "systemaction":
                    return ExecuteSystemAction(processedPath);

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
    /// 在 PATH 环境变量中查找可执行文件（带缓存）
    /// </summary>
    private static readonly Dictionary<string, string?> PathCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 在系统 PATH 环境变量的各个目录中搜索指定的可执行文件
    /// 搜索结果会被缓存，避免重复遍历文件系统。
    /// </summary>
    /// <param name="executable">可执行文件名（如 "notepad.exe" 或 "notepad"）</param>
    /// <returns>找到的完整文件路径；未找到则返回 null</returns>
    private string? FindInPath(string executable)
    {
        if (string.IsNullOrEmpty(executable)) return null;

        // 优先从缓存中查找
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

                // 直接拼接路径检查是否存在
                var fullPath = Path.Combine(path, executable);
                if (File.Exists(fullPath))
                {
                    result = fullPath;
                    break;
                }

                // 自动补充 .exe 扩展名再次查找
                fullPath = Path.Combine(path, executable + ".exe");
                if (File.Exists(fullPath))
                {
                    result = fullPath;
                    break;
                }
            }
            catch { }
        }

        // 将结果写入缓存（包括未找到的情况，避免重复搜索）
        PathCache[executable] = result;
        return result;
    }

    /// <summary>
    /// 内部计算器方法，对数学表达式进行求值
    /// 先通过正则过滤非法字符（仅保留数字和运算符），然后使用 DataTable.Compute 求值。
    /// </summary>
    /// <param name="expression">待计算的数学表达式字符串</param>
    /// <returns>计算结果的字符串表示；计算失败时返回 "Error"</returns>
    private string CalculateInternal(string expression)
    {
        try
        {
            // 过滤掉非数学字符，仅保留数字和运算符
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

    /// <summary>
    /// 启动文件或应用程序
    /// 使用系统默认程序打开指定路径的文件，并记录使用次数。
    /// </summary>
    /// <param name="result">包含文件路径的搜索结果</param>
    /// <returns>启动是否成功</returns>
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
