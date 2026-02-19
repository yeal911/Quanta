// ============================================================================
// 文件名: SearchResult.cs
// 文件用途: 定义搜索结果相关的模型类和枚举，包括搜索结果类型枚举、搜索结果数据模型
//          以及命令执行结果模型。这些模型用于在搜索引擎与界面层之间传递数据。
// ============================================================================

using System.Windows.Media.Imaging;

namespace Quanta.Models;

/// <summary>
/// 搜索结果类型枚举，标识搜索结果的来源类别。
/// </summary>
public enum SearchResultType
{
    /// <summary>已安装的应用程序</summary>
    Application,
    /// <summary>本地文件</summary>
    File,
    /// <summary>最近使用的文件</summary>
    RecentFile,
    /// <summary>已打开的窗口</summary>
    Window,
    /// <summary>内置命令</summary>
    Command,
    /// <summary>计算器表达式</summary>
    Calculator,
    /// <summary>网页搜索</summary>
    WebSearch,
    /// <summary>用户自定义命令</summary>
    CustomCommand,
    /// <summary>二维码生成</summary>
    QRCode,
    /// <summary>系统操作（设置、关于、切换语言）</summary>
    SystemAction
}

/// <summary>
/// 搜索结果模型类，封装单条搜索结果的所有信息，供界面展示和执行使用。
/// </summary>
public class SearchResult
{
    /// <summary>搜索结果唯一标识符（GUID）</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>搜索结果标题，通常显示命令关键字或应用名称</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>搜索结果副标题，显示补充说明信息</summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>执行路径或 URL 地址</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>搜索结果的类型分类</summary>
    public SearchResultType Type { get; set; }

    /// <summary>该结果被使用的次数，用于排序优化</summary>
    public int UsageCount { get; set; }

    /// <summary>最后一次使用时间</summary>
    public DateTime LastUsedTime { get; set; }

    /// <summary>搜索匹配得分，分数越高表示与查询越相关</summary>
    public double MatchScore { get; set; }

    /// <summary>附加数据，可携带任意类型的扩展信息</summary>
    public object? Data { get; set; }

    /// <summary>窗口句柄，当结果类型为 Window 时使用</summary>
    public IntPtr WindowHandle { get; set; }

    /// <summary>关联的自定义命令配置，当结果类型为 CustomCommand 时使用</summary>
    public CommandConfig? CommandConfig { get; set; }

    /// <summary>显示序号，用于界面列表中的排列顺序</summary>
    public int Index { get; set; }

    /// <summary>显示图标文本（emoji），根据命令类型或自定义 IconPath 生成</summary>
    public string IconText { get; set; } = "";

    /// <summary>分组标签，用于搜索结果在界面中按类型分组显示（Command/App/File/Window/Calc/Web）</summary>
    public string GroupLabel { get; set; } = "Command";

    /// <summary>分组排序权重，数值越小越靠前（Command=0, App=1, File=2, Window=3, Calc=4, Web=5）</summary>
    public int GroupOrder { get; set; } = 0;

    /// <summary>触发本次匹配的搜索词，用于在 Title 中高亮显示匹配字符</summary>
    public string QueryMatch { get; set; } = "";

    /// <summary>二维码图片，当结果类型为 QRCode 时使用</summary>
    public System.Windows.Media.Imaging.BitmapImage? QRCodeImage { get; set; }

    /// <summary>二维码对应的原始文本内容</summary>
    public string QRCodeContent { get; set; } = "";
}

/// <summary>
/// 命令执行结果类，封装命令执行后的状态、输出内容和错误信息。
/// </summary>
public class CommandResult
{
    /// <summary>命令是否执行成功</summary>
    public bool Success { get; set; }

    /// <summary>命令执行的标准输出内容</summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>命令执行的错误信息</summary>
    public string Error { get; set; } = string.Empty;
}
