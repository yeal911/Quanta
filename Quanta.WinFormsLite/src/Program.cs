using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace Quanta.WinFormsLite;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, "Quanta_WinFormsLite_Single", out var created);
        if (!created) return;

        ApplicationConfiguration.Initialize();

        var config = ConfigService.Load();
        var searchService = new SearchService(config);

        using var hotkey = new GlobalHotkeyService();
        var form = new LauncherForm(config, searchService);

        hotkey.Pressed += (_, _) =>
        {
            if (form.InvokeRequired)
            {
                form.BeginInvoke(new Action(form.ToggleVisibleAndFocus));
                return;
            }
            form.ToggleVisibleAndFocus();
        };

        if (!hotkey.Register(config.Hotkey))
        {
            Debug.WriteLine("Hotkey register failed, fallback to visible form");
            form.Show();
            form.Activate();
        }

        Application.Run(form);
    }
}
