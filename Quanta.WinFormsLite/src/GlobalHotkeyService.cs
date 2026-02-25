using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Quanta.WinFormsLite;

public sealed class GlobalHotkeyService : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 0xB001;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public event EventHandler? Pressed;

    public GlobalHotkeyService()
    {
        CreateHandle(new CreateParams());
    }

    public bool Register(HotkeyConfig hotkey)
    {
        UnregisterHotKey(Handle, HotkeyId);

        var modifier = hotkey.Modifier.ToLowerInvariant() switch
        {
            "alt" => 0x0001u,
            "control" or "ctrl" => 0x0002u,
            "shift" => 0x0004u,
            "win" => 0x0008u,
            _ => 0x0001u
        };

        if (!Enum.TryParse<Keys>(hotkey.Key, true, out var key))
            key = Keys.R;

        return RegisterHotKey(Handle, HotkeyId, modifier, (uint)key);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterHotKey(Handle, HotkeyId);
        DestroyHandle();
        GC.SuppressFinalize(this);
    }
}
