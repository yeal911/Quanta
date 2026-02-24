// ============================================================================
// 文件名: CommandService.cs
// 文件描述: 命令服务类，提供命令配置的导入、导出功能，以及生成默认命令和分组。
//           支持单独导出/导入命令、分组，也支持导出/导入完整配置。
// ============================================================================

using System.IO;
using System.Text.Json;
using Quanta.Core.Constants;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 命令服务类，提供命令和命令分组的导入导出功能。
/// 支持将命令配置序列化为 JSON 文件进行持久化存储，
/// 也支持从 JSON 文件中反序列化恢复命令配置。
/// </summary>
public class CommandService
{
    /// <summary>
    /// JSON 序列化/反序列化选项，启用缩进格式化和属性名大小写不敏感
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = JsonDefaults.Standard;

    /// <summary>
    /// 将命令列表导出到指定的 JSON 文件。
    /// 导出数据包含版本号、导出时间和命令列表。
    /// </summary>
    /// <param name="filePath">目标文件路径</param>
    /// <param name="commands">要导出的命令配置列表</param>
    /// <returns>导出成功返回 true，失败返回 false</returns>
    public static async Task<bool> ExportCommandsAsync(string filePath, List<CommandConfig> commands)
    {
        try
        {
            var exportData = new CommandExportData
            {
                Version = "1.0",
                ExportedAt = DateTime.Now,
                Commands = commands
            };

            var json = JsonSerializer.Serialize(exportData, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            Logger.Debug($"Commands exported to: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to export commands: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 从指定的 JSON 文件导入命令列表。
    /// 导入时会为每个命令分配新的唯一 ID，以避免与已有命令冲突。
    /// </summary>
    /// <param name="filePath">源文件路径</param>
    /// <returns>返回元组：(是否成功, 命令列表, 错误信息)</returns>
    public static async Task<(bool Success, List<CommandConfig>? Commands, string? Error)> ImportCommandsAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return (false, null, "File not found");

            var json = await File.ReadAllTextAsync(filePath);
            var exportData = JsonSerializer.Deserialize<CommandExportData>(json, JsonOptions);

            if (exportData?.Commands == null)
                return (false, null, "Invalid file format");

            // 为导入的命令分配新 ID，避免与现有命令冲突
            foreach (var cmd in exportData.Commands)
            {
                cmd.Id = Guid.NewGuid().ToString();
            }

            Logger.Debug($"Imported {exportData.Commands.Count} commands from: {filePath}");
            return (true, exportData.Commands, null);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to import commands: {ex.Message}", ex);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// 将命令分组列表导出到指定的 JSON 文件
    /// </summary>
    /// <param name="filePath">目标文件路径</param>
    /// <param name="groups">要导出的命令分组列表</param>
    /// <returns>导出成功返回 true，失败返回 false</returns>
    public static async Task<bool> ExportGroupsAsync(string filePath, List<CommandGroup> groups)
    {
        try
        {
            var json = JsonSerializer.Serialize(groups, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            Logger.Debug($"Groups exported to: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to export groups: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 从指定的 JSON 文件导入命令分组列表。
    /// 导入时会为每个分组分配新的唯一 ID，以避免与已有分组冲突。
    /// </summary>
    /// <param name="filePath">源文件路径</param>
    /// <returns>返回元组：(是否成功, 分组列表, 错误信息)</returns>
    public static async Task<(bool Success, List<CommandGroup>? Groups, string? Error)> ImportGroupsAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return (false, null, "File not found");

            var json = await File.ReadAllTextAsync(filePath);
            var groups = JsonSerializer.Deserialize<List<CommandGroup>>(json, JsonOptions);

            if (groups == null)
                return (false, null, "Invalid file format");

            // 为导入的分组分配新 ID，避免与现有分组冲突
            foreach (var group in groups)
            {
                group.Id = Guid.NewGuid().ToString();
            }

            Logger.Debug($"Imported {groups.Count} groups from: {filePath}");
            return (true, groups, null);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to import groups: {ex.Message}", ex);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// 将完整的应用配置（包含所有命令和分组）导出到单个 JSON 文件
    /// </summary>
    /// <param name="filePath">目标文件路径</param>
    /// <param name="config">要导出的应用配置对象</param>
    /// <returns>导出成功返回 true，失败返回 false</returns>
    public static async Task<bool> ExportAllAsync(string filePath, AppConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            Logger.Debug($"All configuration exported to: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to export configuration: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 从单个 JSON 文件导入完整的应用配置（包含所有命令和分组）
    /// </summary>
    /// <param name="filePath">源文件路径</param>
    /// <returns>返回元组：(是否成功, 应用配置对象, 错误信息)</returns>
    public static async Task<(bool Success, AppConfig? Config, string? Error)> ImportAllAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return (false, null, "File not found");

            var json = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);

            if (config == null)
                return (false, null, "Invalid file format");

            Logger.Debug($"Configuration imported from: {filePath}");
            return (true, config, null);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to import configuration: {ex.Message}", ex);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// 生成用户自定义命令的示例列表（仅包含网页URL等用户级别命令）。
    /// 注意：Windows 系统内置命令（cmd、ping 等）已硬编码在 SearchEngine.BuiltInCommands 中，
    /// 不需要在此生成，也不会出现在用户的配置文件和设置界面中。
    /// </summary>
    /// <returns>预置的示例命令配置列表</returns>
    public static List<CommandConfig> GenerateSampleCommands()
    {
        return new List<CommandConfig>
        {
            // 网页搜索命令（预置命令，不会被保存到配置文件）
            new() { Keyword = "gh", Name = "GitHub", Type = "Url", Path = "https://github.com/{param}", Description = "打开GitHub", IsBuiltIn = true },
            new() { Keyword = "bing", Name = "Bing搜索", Type = "Url", Path = "https://www.bing.com/search?q={param}", Description = "搜索Bing", IsBuiltIn = true },
            new() { Keyword = "youtube", Name = "YouTube", Type = "Url", Path = "https://www.youtube.com/results?search_query={param}", Description = "搜索YouTube", IsBuiltIn = true },
            new() { Keyword = "baidu", Name = "百度", Type = "Url", Path = "https://www.baidu.com/s?wd={param}", Description = "搜索百度", IsBuiltIn = true },
        };
    }

    /// <summary>
    /// 生成默认的命令分组列表，每个分组包含名称、图标、颜色和排序顺序
    /// </summary>
    /// <returns>默认命令分组列表</returns>
    public static List<CommandGroup> GenerateDefaultGroups()
    {
        return new List<CommandGroup>
        {
            new() { Name = "Web", Icon = "🌐", Color = "#0078D4", SortOrder = 1 },
            new() { Name = "Tools", Icon = "🛠️", Color = "#107C10", SortOrder = 2 },
            new() { Name = "System", Icon = "⚙️", Color = "#5C2D91", SortOrder = 3 },
            new() { Name = "Custom", Icon = "📌", Color = "#E74856", SortOrder = 4 }
        };
    }
}

/// <summary>
/// 命令导出数据模型，用于序列化/反序列化命令导出文件。
/// 包含版本号、导出时间和命令列表。
/// </summary>
public class CommandExportData
{
    /// <summary>
    /// 导出文件的版本号
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 导出时间戳
    /// </summary>
    public DateTime ExportedAt { get; set; }

    /// <summary>
    /// 导出的命令配置列表
    /// </summary>
    public List<CommandConfig> Commands { get; set; } = new();
}
