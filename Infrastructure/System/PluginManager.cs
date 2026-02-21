// ============================================================================
// 文件名: PluginManager.cs
// 文件描述: 插件管理系统，定义了插件接口、插件上下文、插件元数据特性和插件管理器。
//           支持加载内置插件和外部 DLL 插件，提供插件的生命周期管理（初始化、执行、卸载、重载）。
//           还包含一个内置的网页搜索插件示例。
// ============================================================================

using System.IO;
using System.Reflection;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 插件基础接口，所有插件必须实现此接口。
/// 定义了插件的基本元数据属性和生命周期方法。
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// 插件的唯一标识符
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 插件的显示名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 插件的版本号
    /// </summary>
    string Version { get; }

    /// <summary>
    /// 插件的功能描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 插件的作者信息
    /// </summary>
    string Author { get; }

    /// <summary>
    /// 初始化插件，在插件加载时调用
    /// </summary>
    /// <param name="context">插件上下文，提供应用程序相关信息和回调</param>
    /// <returns>初始化成功返回 true，失败返回 false</returns>
    bool Initialize(PluginContext context);

    /// <summary>
    /// 根据查询字符串异步执行插件逻辑，返回搜索结果列表
    /// </summary>
    /// <param name="query">用户输入的查询字符串</param>
    /// <param name="cancellationToken">取消令牌，用于取消长时间运行的操作</param>
    /// <returns>匹配的搜索结果列表</returns>
    Task<List<SearchResult>> ExecuteAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理插件资源，在插件卸载时调用
    /// </summary>
    void Cleanup();
}

/// <summary>
/// 插件上下文类，在插件初始化时传递给插件，提供应用程序的运行时信息和回调接口。
/// </summary>
public class PluginContext
{
    /// <summary>
    /// 应用程序数据目录路径
    /// </summary>
    public string AppDataPath { get; set; } = string.Empty;

    /// <summary>
    /// 插件所在目录路径
    /// </summary>
    public string PluginDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 当前应用程序配置
    /// </summary>
    public AppConfig Config { get; set; } = new();

    /// <summary>
    /// 日志输出回调，插件可通过此回调输出普通日志
    /// </summary>
    public Action<string>? OnLog { get; set; }

    /// <summary>
    /// 错误输出回调，插件可通过此回调报告错误信息
    /// </summary>
    public Action<string>? OnError { get; set; }
}

/// <summary>
/// 插件元数据特性，用于标记插件类并声明其基本信息。
/// 可应用于实现了 <see cref="IPlugin"/> 接口的类上。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PluginAttribute : Attribute
{
    /// <summary>
    /// 插件的唯一标识符
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 插件的显示名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 插件的版本号
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// 插件的功能描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 插件的作者信息
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// 初始化插件元数据特性
    /// </summary>
    /// <param name="id">插件唯一标识符</param>
    /// <param name="name">插件显示名称</param>
    /// <param name="version">插件版本号</param>
    /// <param name="description">插件功能描述（可选）</param>
    /// <param name="author">插件作者信息（可选）</param>
    public PluginAttribute(string id, string name, string version, string description = "", string author = "")
    {
        Id = id;
        Name = name;
        Version = version;
        Description = description;
        Author = author;
    }
}

/// <summary>
/// 插件管理器，负责插件的加载、初始化、执行、卸载和重载等生命周期管理。
/// 支持加载内置插件和从指定目录加载外部 DLL 插件。
/// </summary>
public class PluginManager
{
    /// <summary>
    /// 已加载的插件实例列表
    /// </summary>
    private readonly List<IPlugin> _loadedPlugins = new();

    /// <summary>
    /// 插件上下文，包含应用运行时信息
    /// </summary>
    private PluginContext? _context;

    /// <summary>
    /// 外部插件所在的目录路径
    /// </summary>
    private readonly string _pluginDirectory;

    /// <summary>
    /// 获取已加载的插件只读列表
    /// </summary>
    public IReadOnlyList<IPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

    /// <summary>
    /// 获取插件管理器是否已完成初始化
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 初始化插件管理器实例
    /// </summary>
    /// <param name="pluginDirectory">外部插件 DLL 文件所在的目录路径</param>
    public PluginManager(string pluginDirectory)
    {
        _pluginDirectory = pluginDirectory;
    }

    /// <summary>
    /// 异步初始化插件管理器，创建插件目录（如不存在），然后依次加载内置插件和外部插件
    /// </summary>
    /// <param name="context">插件上下文，提供应用程序相关信息</param>
    /// <returns>初始化成功返回 true，失败返回 false</returns>
    public async Task<bool> InitializeAsync(PluginContext context)
    {
        _context = context;

        try
        {
            // 如果插件目录不存在则创建
            if (!Directory.Exists(_pluginDirectory))
            {
                Directory.CreateDirectory(_pluginDirectory);
                Logger.Log($"Created plugin directory: {_pluginDirectory}");
            }

            // 加载内置插件
            await LoadBuiltInPluginsAsync();

            // 从目录加载外部插件
            await LoadExternalPluginsAsync();

            IsInitialized = true;
            Logger.Log($"Plugin manager initialized with {_loadedPlugins.Count} plugins");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to initialize plugin manager", ex);
            return false;
        }
    }

    /// <summary>
    /// 异步加载内置插件。
    /// 内置插件通过 PluginAttribute 特性注册，目前计算器功能已内置于 SearchEngine 中，
    /// 因此此方法暂无实际加载操作。
    /// </summary>
    private async Task LoadBuiltInPluginsAsync()
    {
        // 内置插件通过 PluginAttribute 特性注册
        // 计算器插件已内置于 SearchEngine 中，无需单独加载
        await Task.CompletedTask;
    }

    /// <summary>
    /// 异步加载外部插件。
    /// 扫描插件目录中的所有 DLL 文件，查找实现了 <see cref="IPlugin"/> 接口的类，
    /// 创建实例并初始化。
    /// </summary>
    private async Task LoadExternalPluginsAsync()
    {
        if (!Directory.Exists(_pluginDirectory))
            return;

        var pluginFiles = Directory.GetFiles(_pluginDirectory, "*.dll");

        foreach (var file in pluginFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in pluginTypes)
                {
                    var plugin = CreatePluginInstance(type);
                    if (plugin != null)
                    {
                        if (plugin.Initialize(_context!))
                        {
                            _loadedPlugins.Add(plugin);
                            Logger.Log($"Loaded plugin: {plugin.Name} v{plugin.Version}");
                        }
                        else
                        {
                            Logger.Warn($"Plugin initialization failed: {plugin.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load plugin from {file}: {ex.Message}");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 通过反射创建插件实例
    /// </summary>
    /// <param name="pluginType">插件的类型信息</param>
    /// <returns>创建成功返回插件实例，失败返回 null</returns>
    private IPlugin? CreatePluginInstance(Type pluginType)
    {
        try
        {
            return Activator.CreateInstance(pluginType) as IPlugin;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create plugin instance: {pluginType.Name}", ex);
            return null;
        }
    }

    /// <summary>
    /// 并行执行所有已加载的插件，将查询字符串传递给每个插件并汇总返回结果。
    /// 使用锁机制确保结果列表的线程安全。
    /// </summary>
    /// <param name="query">用户输入的查询字符串</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有插件返回的搜索结果汇总列表</returns>
    public async Task<List<SearchResult>> ExecutePluginsAsync(string query, CancellationToken cancellationToken = default)
    {
        var results = new List<SearchResult>();

        if (!IsInitialized || _context == null)
            return results;

        var tasks = _loadedPlugins.Select(async plugin =>
        {
            try
            {
                var pluginResults = await plugin.ExecuteAsync(query, cancellationToken);
                lock (results)
                {
                    results.AddRange(pluginResults);
                }
            }
            catch (Exception ex)
            {
                _context.OnError?.Invoke($"Plugin {plugin.Name} error: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// 根据插件 ID 查找并返回对应的插件实例
    /// </summary>
    /// <param name="id">插件唯一标识符</param>
    /// <returns>找到则返回插件实例，未找到返回 null</returns>
    public IPlugin? GetPlugin(string id)
    {
        return _loadedPlugins.FirstOrDefault(p => p.Id == id);
    }

    /// <summary>
    /// 卸载指定 ID 的插件，调用其清理方法并从已加载列表中移除
    /// </summary>
    /// <param name="id">要卸载的插件唯一标识符</param>
    /// <returns>卸载成功返回 true，插件不存在或卸载失败返回 false</returns>
    public bool UnloadPlugin(string id)
    {
        var plugin = GetPlugin(id);
        if (plugin == null)
            return false;

        try
        {
            plugin.Cleanup();
            _loadedPlugins.Remove(plugin);
            Logger.Log($"Unloaded plugin: {plugin.Name}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to unload plugin {id}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 异步重新加载所有插件。先清理并移除所有已加载的插件，然后重新执行初始化流程。
    /// </summary>
    public async Task ReloadAsync()
    {
        // 清理所有已加载的插件
        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                plugin.Cleanup();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error cleaning up plugin {plugin.Name}", ex);
            }
        }

        _loadedPlugins.Clear();

        // 重新初始化
        if (_context != null)
        {
            await InitializeAsync(_context);
        }
    }

    /// <summary>
    /// 清理所有插件并重置插件管理器状态。
    /// 依次调用每个插件的 Cleanup 方法，清空插件列表并将初始化标志设为 false。
    /// </summary>
    public void Cleanup()
    {
        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                plugin.Cleanup();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error cleaning up plugin {plugin.Name}", ex);
            }
        }

        _loadedPlugins.Clear();
        IsInitialized = false;
        Logger.Log("Plugin manager cleaned up");
    }
}

/// <summary>
/// 内置网页搜索插件示例，支持通过关键字触发不同搜索引擎的网页搜索。
/// 支持的搜索引擎：Google、Bing、Baidu、DuckDuckGo。
/// </summary>
[Plugin("websearch", "Web Search", "1.0", "Search the web", "Quanta")]
public class WebSearchPlugin : IPlugin
{
    /// <summary>
    /// 插件唯一标识符
    /// </summary>
    public string Id => "websearch";

    /// <summary>
    /// 插件显示名称
    /// </summary>
    public string Name => "Web Search";

    /// <summary>
    /// 插件版本号
    /// </summary>
    public string Version => "1.0";

    /// <summary>
    /// 插件功能描述
    /// </summary>
    public string Description => "Search the web";

    /// <summary>
    /// 插件作者
    /// </summary>
    public string Author => "Quanta";

    /// <summary>
    /// 搜索引擎配置列表，每项包含：(触发关键字, 引擎名称, URL 模板, 图标)
    /// </summary>
    private readonly List<(string Keyword, string Name, string Url, string Icon)> _engines = new()
    {
        ("google", "Google", "https://www.google.com/search?q={query}", "🔍"),
        ("bing", "Bing", "https://www.bing.com/search?q={query}", "🌐"),
        ("baidu", "Baidu", "https://www.baidu.com/s?wd={query}", "🔎"),
        ("duckduckgo", "DuckDuckGo", "https://duckduckgo.com/?q={query}", "🦆"),
    };

    /// <summary>
    /// 插件上下文引用
    /// </summary>
    private PluginContext? _context;

    /// <summary>
    /// 初始化网页搜索插件
    /// </summary>
    /// <param name="context">插件上下文</param>
    /// <returns>始终返回 true</returns>
    public bool Initialize(PluginContext context)
    {
        _context = context;
        return true;
    }

    /// <summary>
    /// 根据查询字符串匹配搜索引擎并生成搜索结果。
    /// 如果查询以某个引擎关键字开头，则生成该引擎的搜索链接（匹配度 1.0）；
    /// 如果某个引擎关键字以查询开头，则作为补全建议返回（匹配度 0.8）。
    /// </summary>
    /// <param name="query">用户输入的查询字符串</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的搜索结果列表</returns>
    public Task<List<SearchResult>> ExecuteAsync(string query, CancellationToken cancellationToken = default)
    {
        var results = new List<SearchResult>();

        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult(results);

        foreach (var engine in _engines)
        {
            if (query.StartsWith(engine.Keyword, StringComparison.OrdinalIgnoreCase))
            {
                var searchQuery = query.Length > engine.Keyword.Length
                    ? query.Substring(engine.Keyword.Length).Trim()
                    : "";

                results.Add(new SearchResult
                {
                    Id = $"plugin:{Id}:{engine.Keyword}",
                    Title = engine.Keyword,
                    Subtitle = $"Search {engine.Name}: {searchQuery}",
                    Path = engine.Url.Replace("{query}", Uri.EscapeDataString(searchQuery)),
                    Type = SearchResultType.WebSearch,
                    MatchScore = 1.0
                });
            }
            else if (engine.Keyword.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new SearchResult
                {
                    Id = $"plugin:{Id}:{engine.Keyword}",
                    Title = engine.Keyword,
                    Subtitle = $"Search {engine.Name}",
                    Path = engine.Url.Replace("{query}", ""),
                    Type = SearchResultType.WebSearch,
                    MatchScore = 0.8
                });
            }
        }

        return Task.FromResult(results);
    }

    /// <summary>
    /// 清理插件资源，释放上下文引用
    /// </summary>
    public void Cleanup()
    {
        _context = null;
    }
}
