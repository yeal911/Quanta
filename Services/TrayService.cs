using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Forms;
using Quanta.Helpers;

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
        // Always rebuild the menu if NotifyIcon already exists
        if (_notifyIcon != null)
        {
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        // Load language from config
        LocalizationService.LoadFromConfig();

        // Create a simple, minimalist icon programmatically
        Icon icon = CreateAppIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = "Quanta - Launcher",
        };

        // Build context menu
        BuildContextMenu();

        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

        Logger.Log("TrayService initialized");
    }

    private void BuildContextMenu()
    {
        if (_notifyIcon == null) return;
        
        var menu = new ContextMenuStrip();
        
        // Show
        var showItem = new ToolStripMenuItem(LocalizationService.Get("TrayShow"));
        showItem.Click += (s, e) => ShowMainWindow();
        menu.Items.Add(showItem);
        
        // Settings
        var settingsItem = new ToolStripMenuItem(LocalizationService.Get("TraySettings"));
        settingsItem.Click += (s, e) => OnSettingsRequested();
        menu.Items.Add(settingsItem);
        
        // Language submenu
        var langMenu = new ToolStripMenuItem(LocalizationService.Get("TrayLanguage"));
        
        var zhItem = new ToolStripMenuItem(LocalizationService.Get("TrayChinese"));
        zhItem.Click += (s, e) => SetLanguage("zh-CN");
        zhItem.Checked = LocalizationService.CurrentLanguage == "zh-CN";
        langMenu.DropDownItems.Add(zhItem);
        
        var enItem = new ToolStripMenuItem(LocalizationService.Get("TrayEnglish"));
        enItem.Click += (s, e) => SetLanguage("en-US");
        enItem.Checked = LocalizationService.CurrentLanguage == "en-US";
        langMenu.DropDownItems.Add(enItem);
        
        menu.Items.Add(langMenu);
        
        menu.Items.Add(new ToolStripSeparator());
        
        // About
        var aboutItem = new ToolStripMenuItem(LocalizationService.Get("TrayAbout"));
        aboutItem.Click += (s, e) => ShowAbout();
        menu.Items.Add(aboutItem);
        
        // Exit
        var exitItem = new ToolStripMenuItem(LocalizationService.Get("TrayExit"));
        exitItem.Click += (s, e) => OnExitRequested();
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = menu;
    }

    private void SetLanguage(string lang)
    {
        LocalizationService.CurrentLanguage = lang;
        // Rebuild menu to update language
        BuildContextMenu();

        // Refresh MainWindow localization
        _mainWindow.Dispatcher.Invoke(() =>
        {
            if (_mainWindow is Quanta.Views.MainWindow mainWin)
            {
                mainWin.RefreshLocalization();
            }
        });
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
            ToastService.Instance.ShowInfo($"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
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
