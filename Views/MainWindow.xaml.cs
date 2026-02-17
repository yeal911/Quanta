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
        var commandRouter = new CommandRouter(usageTracker);
        var searchEngine = new SearchEngine(usageTracker, commandRouter);
        
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

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel.IsParamMode)
        {
            // In param mode, update the param in ViewModel
            _viewModel.CommandParam = SearchBox.Text;
            // Placeholder stays hidden, ParamIndicator stays visible
        }
        else
        {
            // Show/hide placeholder based on text
            PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Load language setting
        LocalizationService.LoadFromConfig();
        
        // Set placeholder text
        PlaceholderText.Text = LocalizationService.Get("SearchPlaceholder");
        
        // Initialize ToastService with main window
        ToastService.Instance.SetMainWindow(this);
        
        _windowHandle = new WindowInteropHelper(this).Handle;
        var config = ConfigLoader.Load();
        
        var registered = _hotkeyManager.Initialize(_windowHandle, config.Hotkey);
        _hotkeyManager.HotkeyPressed += (s, args) => Dispatcher.Invoke(() => ToggleVisibility());
        if (!registered)
        {
            Dispatcher.BeginInvoke(() =>
                ToastService.Instance.ShowWarning(LocalizationService.Get("HotkeyRegisterFailed")));
        }
        
        // Initialize system tray
        _trayService = new TrayService(this);
        _trayService.SettingsRequested += (s, args) => Dispatcher.Invoke(() => OpenCommandSettings());
        _trayService.ExitRequested += (s, args) => Dispatcher.Invoke(() => _trayService?.Dispose());
        _trayService.Initialize();
        
        BuildSearchIconMenu();

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
        ParamIndicator.Visibility = Visibility.Collapsed;
        SearchBox.Padding = new Thickness(6, 4, 0, 4);
        PlaceholderText.Visibility = Visibility.Visible;
        SearchBox.Focus();
        
        // Update toast position
        ToastService.Instance.SetMainWindow(this);
        
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

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Ctrl+数字 快速执行
        if (e.Key >= Key.D1 && e.Key <= Key.D9 && Keyboard.Modifiers == ModifierKeys.Control)
        {
            int index = e.Key - Key.D1;
            ExecuteByIndex(index);
            e.Handled = true;
            return;
        }
        
        // Ctrl+数字 (小键盘)
        if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad9 && Keyboard.Modifiers == ModifierKeys.Control)
        {
            int index = e.Key - Key.NumPad1;
            ExecuteByIndex(index);
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                if (_viewModel.IsParamMode)
                {
                    _viewModel.SwitchToNormalModeCommand.Execute(null);
                    SearchBox.Text = "";
                    ParamIndicator.Visibility = Visibility.Collapsed;
                    SearchBox.Padding = new Thickness(6, 4, 0, 4);
                    PlaceholderText.Visibility = Visibility.Visible;
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

    private void ExecuteByIndex(int index)
    {
        if (index >= 0 && index < _viewModel.Results.Count)
        {
            _viewModel.SelectedIndex = index;
            _ = ExecuteSelectedAsync();
        }
    }

    private void HandleTabKey()
    {
        if (_viewModel.IsParamMode)
        {
            SearchBox.Focus();
            SearchBox.CaretIndex = SearchBox.Text.Length;
            return;
        }

        // Try to find the first matching CustomCommand in results
        string? matchedKeyword = null;
        foreach (var result in _viewModel.Results)
        {
            if (result.Type == SearchResultType.CustomCommand)
            {
                matchedKeyword = result.Title;
                break;
            }
        }

        if (matchedKeyword != null)
        {
            EnterParamMode(matchedKeyword);
            return;
        }

        // Normal tab behavior - select next item
        _viewModel.SelectNextCommand.Execute(null);
    }

    private void EnterParamMode(string keyword)
    {
        _viewModel.SwitchToParamModeCommand.Execute(keyword);
        ParamIndicator.Text = keyword + " >";
        ParamIndicator.Visibility = Visibility.Visible;
        PlaceholderText.Visibility = Visibility.Collapsed;
        SearchBox.Text = "";
        SearchBox.Focus();

        // Adjust SearchBox left padding to avoid overlapping with ParamIndicator
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            ParamIndicator.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            var indicatorWidth = ParamIndicator.DesiredSize.Width;
            SearchBox.Padding = new Thickness(indicatorWidth, 4, 0, 4);
            SearchBox.CaretIndex = 0;
        });
    }

    private void UpdateParamIndicator()
    {
        if (_viewModel.IsParamMode)
        {
            ParamIndicator.Text = _viewModel.CommandKeyword + " >";
            ParamIndicator.Visibility = Visibility.Visible;
            PlaceholderText.Visibility = Visibility.Collapsed;

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                ParamIndicator.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                var indicatorWidth = ParamIndicator.DesiredSize.Width;
                SearchBox.Padding = new Thickness(indicatorWidth, 4, 0, 4);
            });
        }
        else
        {
            ParamIndicator.Visibility = Visibility.Collapsed;
            SearchBox.Padding = new Thickness(6, 4, 0, 4);
        }
    }

    public void RefreshLocalization()
    {
        PlaceholderText.Text = LocalizationService.Get("SearchPlaceholder");
        BuildSearchIconMenu();
    }

    private void BuildSearchIconMenu()
    {
        SearchIconMenu.Items.Clear();

        var showItem = new MenuItem { Header = LocalizationService.Get("TrayShow") };
        showItem.Click += (s, e) => ShowWindow();
        SearchIconMenu.Items.Add(showItem);

        var settingsItem = new MenuItem { Header = LocalizationService.Get("TraySettings") };
        settingsItem.Click += (s, e) => OpenCommandSettings();
        SearchIconMenu.Items.Add(settingsItem);

        // Language submenu
        var langItem = new MenuItem { Header = LocalizationService.Get("TrayLanguage") };
        var zhItem = new MenuItem { Header = LocalizationService.Get("TrayChinese"), IsChecked = LocalizationService.CurrentLanguage == "zh-CN" };
        zhItem.Click += (s, e) => { LocalizationService.CurrentLanguage = "zh-CN"; RefreshLocalization(); _trayService?.Initialize(); };
        var enItem = new MenuItem { Header = LocalizationService.Get("TrayEnglish"), IsChecked = LocalizationService.CurrentLanguage == "en-US" };
        enItem.Click += (s, e) => { LocalizationService.CurrentLanguage = "en-US"; RefreshLocalization(); _trayService?.Initialize(); };
        langItem.Items.Add(zhItem);
        langItem.Items.Add(enItem);
        SearchIconMenu.Items.Add(langItem);

        SearchIconMenu.Items.Add(new Separator());

        var aboutItem = new MenuItem { Header = LocalizationService.Get("TrayAbout") };
        aboutItem.Click += (s, e) => ToastService.Instance.ShowInfo($"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
        SearchIconMenu.Items.Add(aboutItem);

        var exitItem = new MenuItem { Header = LocalizationService.Get("TrayExit") };
        exitItem.Click += (s, e) => { _trayService?.Dispose(); System.Windows.Application.Current.Shutdown(); };
        SearchIconMenu.Items.Add(exitItem);
    }

    private void SearchIcon_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        BuildSearchIconMenu();
        SearchIconMenu.IsOpen = true;
        e.Handled = true;
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
        win.SetDarkTheme(_viewModel.IsDarkTheme);
        win.Show();
        
        // After settings window closes, reload hotkey and commands
        win.Closed += (s, args) =>
        {
            var config = ConfigLoader.Load();
            var registered = _hotkeyManager.Reregister(config.Hotkey);
            _viewModel.SearchEngine.ReloadCommands();
            if (!registered)
            {
                ToastService.Instance.ShowWarning(LocalizationService.Get("HotkeyRegisterFailed"));
            }
        };
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
