// ============================================================================
// 文件名: WindowManager.cs
// 文件用途: 提供 Windows 窗口管理服务，通过调用 Win32 API 枚举系统中所有可见窗口，
//          并支持将指定窗口激活（切换到前台）。用于实现窗口搜索和快速切换功能。
// ============================================================================

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 窗口管理器，负责枚举系统中所有可见的应用程序窗口并提供窗口激活功能。
/// 通过 Win32 API（user32.dll）实现底层窗口操作，过滤掉工具窗口、无标题窗口
/// 以及 Quanta 自身的窗口，为用户提供可切换的窗口列表。
/// </summary>
public class WindowManager
{
    /// <summary>
    /// Win32 API：枚举所有顶层窗口
    /// </summary>
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    /// <summary>
    /// Win32 API：判断窗口是否可见
    /// </summary>
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>
    /// Win32 API：获取窗口标题文本
    /// </summary>
    [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    /// <summary>
    /// Win32 API：获取窗口标题文本的长度
    /// </summary>
    [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hWnd);

    /// <summary>
    /// Win32 API：将指定窗口设置为前台窗口
    /// </summary>
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// Win32 API：控制窗口的显示状态（显示、隐藏、最大化、最小化等）
    /// </summary>
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// Win32 API：判断窗口是否处于最小化状态
    /// </summary>
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);

    /// <summary>
    /// Win32 API：获取 Shell 窗口（桌面窗口）的句柄
    /// </summary>
    [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();

    /// <summary>
    /// Win32 API：获取窗口的扩展样式等属性信息
    /// </summary>
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    /// <summary>
    /// Win32 API：获取与指定窗口有特定关系的窗口（如所有者窗口）
    /// </summary>
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    /// <summary>
    /// Win32 API：获取创建指定窗口的线程和进程 ID
    /// </summary>
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// 窗口显示命令常量：SW_RESTORE（恢复窗口）、扩展样式索引和样式标志位
    /// </summary>
    private const int SW_RESTORE = 9, GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x00000080, WS_EX_APPWINDOW = 0x00040000;

    /// <summary>
    /// GetWindow 函数的关系常量：GW_OWNER 表示获取所有者窗口
    /// </summary>
    private const uint GW_OWNER = 4;

    /// <summary>
    /// EnumWindows 回调函数的委托类型定义
    /// </summary>
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    /// <summary>
    /// 当前进程的名称，用于在枚举窗口时过滤掉自身
    /// </summary>
    private static readonly string CurrentProcessName = Process.GetCurrentProcess().ProcessName;

    /// <summary>
    /// 获取系统中所有可见的应用程序窗口列表。
    /// 过滤规则：
    /// 1. 排除不可见的窗口和 Shell 窗口（桌面）
    /// 2. 排除 Quanta 和 OpenCode 进程的窗口
    /// 3. 排除没有标题的窗口
    /// 4. 排除工具窗口（WS_EX_TOOLWINDOW 样式但非 WS_EX_APPWINDOW）
    /// 5. 排除没有所有者且不具有 WS_EX_APPWINDOW 样式的窗口
    /// </summary>
    /// <returns>包含所有符合条件的可见窗口的搜索结果列表</returns>
    public List<SearchResult> GetVisibleWindows()
    {
        var windows = new List<SearchResult>();
        var shellWindow = GetShellWindow();
        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd) || hWnd == shellWindow) return true;

            // 获取窗口所属的进程 ID，用于过滤本应用和特定进程的窗口
            GetWindowThreadProcessId(hWnd, out uint processId);
            try
            {
                var process = Process.GetProcessById((int)processId);
                // 过滤掉 Quanta 自身和 OpenCode（IDE）的窗口
                if (process.ProcessName.Equals("Quanta", StringComparison.OrdinalIgnoreCase) ||
                    process.ProcessName.Equals("opencode", StringComparison.OrdinalIgnoreCase) ||
                    process.ProcessName.Equals("OpenCode", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { }

            // 获取窗口标题，跳过无标题的窗口
            int length = GetWindowTextLength(hWnd);
            if (length == 0) return true;
            var sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString();
            if (string.IsNullOrWhiteSpace(title)) return true;

            // 检查窗口扩展样式，过滤掉工具窗口（非应用窗口样式的工具窗口）
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0) return true;

            // 仅显示顶级窗口（无所有者）或显式标记为应用窗口的窗口
            // 有所有者的窗口（如对话框/弹出框）通常不应出现在列表中
            IntPtr owner = GetWindow(hWnd, GW_OWNER);
            bool hasOwner = owner != IntPtr.Zero;
            if (hasOwner && (exStyle & WS_EX_APPWINDOW) == 0) return true;

            // 将符合条件的窗口添加到结果列表中
            windows.Add(new SearchResult { Title = title, Type = SearchResultType.Window, WindowHandle = hWnd, Path = $"Window: {title}" });
            return true;
        }, IntPtr.Zero);
        return windows;
    }

    /// <summary>
    /// 通过窗口句柄激活（切换到前台）指定的窗口。
    /// 如果窗口处于最小化状态，会先将其恢复再激活。
    /// </summary>
    /// <param name="hWnd">要激活的窗口句柄</param>
    /// <returns>激活成功返回 true，如果句柄无效则返回 false</returns>
    public bool ActivateWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return false;
        if (IsIconic(hWnd)) ShowWindow(hWnd, SW_RESTORE);
        return SetForegroundWindow(hWnd);
    }

    /// <summary>
    /// 通过搜索结果对象激活对应的窗口。
    /// 仅当搜索结果类型为窗口时才执行激活操作。
    /// </summary>
    /// <param name="result">包含窗口句柄信息的搜索结果对象</param>
    /// <returns>激活成功返回 true，如果结果类型不是窗口则返回 false</returns>
    public bool ActivateWindow(SearchResult result) => result.Type == SearchResultType.Window && ActivateWindow(result.WindowHandle);
}
