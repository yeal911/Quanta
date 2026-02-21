/// <summary>
/// 可执行文件路径缓存
/// 负责在 PATH 环境变量中搜索可执行文件，并缓存搜索结果以提升性能。
/// </summary>

using System.IO;

namespace Quanta.Services;

/// <summary>
/// 可执行文件路径缓存接口
/// </summary>
public interface IExecutablePathCache
{
    /// <summary>
    /// 在系统 PATH 环境变量的各个目录中搜索指定的可执行文件
    /// </summary>
    /// <param name="executable">可执行文件名</param>
    /// <returns>找到的完整文件路径；未找到则返回 null</returns>
    string? FindInPath(string executable);
}

/// <summary>
/// 可执行文件路径缓存默认实现
/// </summary>
public sealed class ExecutablePathCache : IExecutablePathCache
{
    private static readonly Dictionary<string, string?> PathCache = new(StringComparer.OrdinalIgnoreCase);

    public static readonly ExecutablePathCache Instance = new();

    private ExecutablePathCache() { }

    /// <summary>
    /// 在 PATH 环境变量中查找可执行文件（带缓存）
    /// </summary>
    public string? FindInPath(string executable)
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
}
