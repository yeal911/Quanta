using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Forms;

namespace Quanta.Services;

public class TrayService : IDisposable
{
    private readonly Window _mainWindow;
    private NotifyIcon? _notifyIcon;
    private bool _disposed;

    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    public TrayService(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public void Initialize()
    {
        if (_notifyIcon != null) return;

        // Create a simple, minimalist icon programmatically
        Icon icon = CreateAppIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = "Quanta - 启动器",
        };

        // Build context menu
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示界面", null, (s, e) => ShowMainWindow());
        menu.Items.Add("命令设置", null, (s, e) => OnSettingsRequested());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("关于", null, (s, e) => ShowAbout());
        menu.Items.Add("退出", null, (s, e) => OnExitRequested());

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

        Logger.Log("TrayService initialized");
    }

    private Icon CreateAppIcon()
    {
        // Create a simple 16x16 minimalist icon
        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        
        // Draw a simple "Q" shape
        using var brush = new SolidBrush(Color.FromArgb(0, 120, 212)); // Windows blue
        using var pen = new Pen(brush, 2);
        
        // Draw circle
        g.DrawEllipse(pen, 2, 2, 12, 12);
        
        // Draw inner dot
        g.FillEllipse(brush, 6, 6, 4, 4);
        
        return Icon.FromHandle(bitmap.GetHicon());
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;
        
        _mainWindow.Dispatcher.Invoke(() =>
        {
            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;
            
            _mainWindow.Show();
            _mainWindow.Activate();
            _mainWindow.Focus();
        });
    }

    private void OnSettingsRequested()
    {
        ShowMainWindow();
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ShowAbout()
    {
        // Show toast notification with about info
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ToastService.Instance.ShowInfo("作者：yeal911\n邮箱：yeal91117@gmail.com", 3.0);
        });
    }

    private void OnExitRequested()
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
        Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        
        Logger.Log("TrayService disposed");
    }
}
