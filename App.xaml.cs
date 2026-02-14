using System;
using System.Diagnostics;
using System.Windows;

namespace Quanta;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (!EnsureSingleInstance()) { Current.Shutdown(); return; }
    }

    private bool EnsureSingleInstance()
    {
        string mutexName = "Quanta_SingleInstance_Mutex";
        var mutex = new System.Threading.Mutex(true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            var currentProcess = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {
                    var handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero) { SetForegroundWindow(handle); ShowWindow(handle, SW_RESTORE); }
                    break;
                }
            }
            return false;
        }
        return true;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_RESTORE = 9;
}
