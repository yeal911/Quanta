// ============================================================================
// 文件名: ClipboardMonitor.cs
// 文件描述: 剪贴板变化监听器。
//           通过 Win32 AddClipboardFormatListener + HwndSource.AddHook 接收
//           WM_CLIPBOARDUPDATE 消息，在剪贴板文本变化时触发 ClipboardChanged 事件。
// ============================================================================

using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Quanta.Services;

/// <summary>
/// 剪贴板变化监听器。
/// 在主窗口加载后调用 <see cref="Start"/> 注册监听，应用退出前调用 <see cref="Stop"/> 清理。
/// </summary>
public class ClipboardMonitor
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private const int WM_CLIPBOARDUPDATE = 0x031D;

    private HwndSource? _hwndSource;
    private IntPtr _hwnd = IntPtr.Zero;

    /// <summary>剪贴板文本内容变化时触发，参数为新的文本内容。</summary>
    public event Action<string>? ClipboardChanged;

    /// <summary>
    /// 启动剪贴板监听。需传入主窗口句柄（在 Window.Loaded 后获取）。
    /// </summary>
    public void Start(IntPtr hwnd)
    {
        if (_hwnd != IntPtr.Zero) return; // 防止重复注册

        _hwnd = hwnd;
        _hwndSource = HwndSource.FromHwnd(hwnd);
        _hwndSource?.AddHook(WndProc);
        AddClipboardFormatListener(hwnd);
    }

    /// <summary>停止剪贴板监听并清理资源。</summary>
    public void Stop()
    {
        if (_hwnd == IntPtr.Zero) return;
        _hwndSource?.RemoveHook(WndProc);
        RemoveClipboardFormatListener(_hwnd);
        _hwnd = IntPtr.Zero;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_CLIPBOARDUPDATE) return IntPtr.Zero;

        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                string text = System.Windows.Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                    ClipboardChanged?.Invoke(text);
            }
        }
        catch
        {
            // 剪贴板被其他进程锁定时静默忽略
        }

        return IntPtr.Zero;
    }
}
