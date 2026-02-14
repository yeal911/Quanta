using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Quanta.Models;

namespace Quanta.Services;

public class WindowManager
{
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    private const int SW_RESTORE = 9, GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x00000080, WS_EX_APPWINDOW = 0x00040000;
    private const uint GW_OWNER = 4;
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public List<SearchResult> GetVisibleWindows()
    {
        var windows = new List<SearchResult>();
        var shellWindow = GetShellWindow();
        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd) || hWnd == shellWindow) return true;
            int length = GetWindowTextLength(hWnd);
            if (length == 0) return true;
            var sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString();
            if (string.IsNullOrWhiteSpace(title)) return true;
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0) return true;
            IntPtr owner = GetWindow(hWnd, GW_OWNER);
            if (owner == IntPtr.Zero && (exStyle & WS_EX_APPWINDOW) == 0) return true;
            windows.Add(new SearchResult { Title = title, Type = SearchResultType.Window, WindowHandle = hWnd, Path = $"Window: {title}" });
            return true;
        }, IntPtr.Zero);
        return windows;
    }

    public bool ActivateWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return false;
        if (IsIconic(hWnd)) ShowWindow(hWnd, SW_RESTORE);
        return SetForegroundWindow(hWnd);
    }

    public bool ActivateWindow(SearchResult result) => result.Type == SearchResultType.Window && ActivateWindow(result.WindowHandle);
}
