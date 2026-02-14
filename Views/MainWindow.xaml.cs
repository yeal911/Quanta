using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Quanta.Helpers;
using Quanta.Models;
using Quanta.Services;
using Quanta.ViewModels;

namespace Quanta.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly HotkeyManager _hotkeyManager;
    private IntPtr _windowHandle;
    private bool _isVisible;
    private TrayService? _trayService;

    public MainWindow()
    {
        InitializeComponent();
        
        var usageTracker = new UsageTracker();
        var windowManager = new WindowManager();
        var commandRouter = new CommandRouter(usageTracker);
        var searchEngine = new SearchEngine(usageTracker, windowManager, commandRouter);
        
        _viewModel = new MainViewModel(searchEngine, usageTracker);
        _hotkeyManager = new HotkeyManager();
        
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
        
        // Enable dragging
        MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
    }

    private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        var config = ConfigLoader.Load();
        
        _hotkeyManager.Initialize(_windowHandle, config.Hotkey);
        _hotkeyManager.HotkeyPressed += (s, args) => Dispatcher.Invoke(() => ToggleVisibility());
        
        // Initialize system tray
        _trayService = new TrayService(this);
        _trayService.SettingsRequested += (s, args) => Dispatcher.Invoke(OpenCommandSettings);
        _trayService.ExitRequested += (s, args) => Dispatcher.Invoke(() => _trayService?.Dispose());
        _trayService.Initialize();
        
        SearchBox.Focus();
        Hide();
        _isVisible = false;
        
        Logger.Log("MainWindow loaded");
    }

    public void ToggleVisibility()
    {
        if (_isVisible)
            HideWindow();
        else
            ShowWindow();
    }

    private void ShowWindow()
    {
        var screen = SystemParameters.WorkArea;
        Left = (screen.Width - Width) / 2;
        Top = (screen.Height - Height) / 2;
        Show(); Activate(); Focus();
        
        _viewModel.ClearSearchCommand.Execute(null);
        SearchBox.Focus();
        
        Opacity = 0;
        BeginAnimation(OpacityProperty, new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(150) });
        _isVisible = true;
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleThemeCommand.Execute(null);
        
        Dispatcher.Invoke(() =>
        {
            var border = FindName("MainBorder") as Border;
            var icon = FindName("ThemeIcon") as TextBlock;
            
            if (border != null && icon != null)
            {
                if (_viewModel.IsDarkTheme)
                {
                    // Switch to Dark mode
                    border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
                    border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 51, 51));
                    SearchBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    SearchBox.CaretBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    icon.Text = "☀";
                    icon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                }
                else
                {
                    // Switch to Light mode
                    border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224));
                    SearchBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
                    SearchBox.CaretBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
                    icon.Text = "🌙";
                    icon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
                }
            }
        });
    }

    private void HideWindow()
    {
        var animation = new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromMilliseconds(100) };
        animation.Completed += (s, e) => Hide();
        BeginAnimation(OpacityProperty, animation);
        _isVisible = false;
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(async () =>
        {
            await Task.Delay(150);
            if (_isVisible && !IsActive) HideWindow();
        });
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                if (_viewModel.IsParamMode)
                {
                    _viewModel.SwitchToNormalModeCommand.Execute(null);
                    SearchBox.Text = "";
                }
                else
                {
                    HideWindow();
                }
                e.Handled = true;
                break;

            case Key.Down:
                _viewModel.SelectNextCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Up:
                _viewModel.SelectPreviousCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Enter:
                _ = ExecuteSelectedAsync();
                e.Handled = true;
                break;

            case Key.Tab:
                HandleTabKey();
                e.Handled = true;
                break;
        }
    }

    private void HandleTabKey()
    {
        var text = SearchBox.Text;

        if (_viewModel.IsParamMode)
        {
            SearchBox.Focus();
            SearchBox.CaretIndex = SearchBox.Text.Length;
            return;
        }

        // Check if current text matches a command
        foreach (var result in _viewModel.Results)
        {
            if (result.Type == SearchResultType.CustomCommand && 
                result.Title.Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                _viewModel.SwitchToParamModeCommand.Execute(result.Title);
                SearchBox.Text = _viewModel.SearchText;
                SearchBox.CaretIndex = SearchBox.Text.Length;
                return;
            }
        }

        // If no exact match but starts with a known command, auto-switch
        var config = ConfigLoader.Load();
        foreach (var cmd in config.Commands)
        {
            if (cmd.Keyword.Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                _viewModel.SwitchToParamModeCommand.Execute(cmd.Keyword);
                SearchBox.Text = _viewModel.SearchText;
                SearchBox.CaretIndex = SearchBox.Text.Length;
                return;
            }
        }

        // Normal tab behavior - select next item
        _viewModel.SelectNextCommand.Execute(null);
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem != null)
            ResultsList.ScrollIntoView(ResultsList.SelectedItem);
    }

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => _ = ExecuteSelectedAsync();

    private void OpenCommandSettings(object? sender = null, RoutedEventArgs? e = null)
    {
        var win = new CommandSettingsWindow { Owner = this };
        win.Show();
    }

    private async Task ExecuteSelectedAsync()
    {
        await _viewModel.ExecuteSelectedCommand.ExecuteAsync(null);
        if (string.IsNullOrEmpty(_viewModel.SearchText))
        {
            HideWindow();
        }
    }
}
