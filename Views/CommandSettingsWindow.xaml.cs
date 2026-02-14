using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Quanta.Models;
using Quanta.Helpers;
using Quanta.Services;

namespace Quanta.Views;

public partial class CommandSettingsWindow : Window
{
    public ObservableCollection<CommandConfig> Commands { get; set; } = new();
    private AppConfig? _config;
    private string _currentHotkey = "";

    public CommandSettingsWindow()
    {
        InitializeComponent();
        LoadConfig();
        DataContext = this;
    }

    private void LoadConfig()
    {
        _config = ConfigLoader.Load();
        if (_config?.Commands != null)
            Commands = new ObservableCollection<CommandConfig>(_config.Commands);
        
        // Load hotkey settings
        if (_config?.Hotkey != null)
        {
            _currentHotkey = FormatHotkey(_config.Hotkey.Modifier, _config.Hotkey.Key);
            HotkeyTextBox.Text = string.IsNullOrEmpty(_currentHotkey) ? "请按下快捷键..." : _currentHotkey;
        }
        else
        {
            HotkeyTextBox.Text = "请按下快捷键...";
        }
    }

    private string FormatHotkey(string modifier, string key)
    {
        if (string.IsNullOrEmpty(modifier) && string.IsNullOrEmpty(key))
            return "";
        return $"{modifier}+{key}".TrimStart('+');
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        HotkeyTextBox.Text = "请按下快捷键...";
        HotkeyTextBox.SelectAll();
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        
        var modifiers = Keyboard.Modifiers;
        var key = e.Key;
        
        // Ignore modifier keys alone
        if (key == Key.LeftCtrl || key == Key.RightCtrl || 
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        // If no modifier, ignore (require at least one)
        if (modifiers == ModifierKeys.None)
        {
            HotkeyTextBox.Text = "请按下快捷键...";
            return;
        }

        string modifierStr = "";
        if (modifiers.HasFlag(ModifierKeys.Control)) modifierStr += "Ctrl+";
        if (modifiers.HasFlag(ModifierKeys.Alt)) modifierStr += "Alt+";
        if (modifiers.HasFlag(ModifierKeys.Shift)) modifierStr += "Shift+";
        if (modifiers.HasFlag(ModifierKeys.Windows)) modifierStr += "Win+";

        // Get the actual key (handle Key.System for Alt combinations)
        string keyStr = key.ToString();
        if (key == Key.System)
        {
            keyStr = e.SystemKey.ToString();
        }

        _currentHotkey = modifierStr + keyStr;
        HotkeyTextBox.Text = _currentHotkey;
    }

    private void AddCommand_Click(object sender, RoutedEventArgs e)
    {
        var cmd = new CommandConfig 
        { 
            Keyword = "new", 
            Name = "新命令", 
            Type = "Url", 
            Path = "https://example.com",
            Enabled = true
        };
        Commands.Add(cmd);
        ToastService.Instance.ShowSuccess("已添加");
    }

    private void DeleteCommand_Click(object sender, RoutedEventArgs e)
    {
        if (CommandsGrid?.SelectedItem is CommandConfig selected)
        {
            Commands.Remove(selected);
            ToastService.Instance.ShowSuccess("已删除");
        }
    }

    private void SaveCommand_Click(object sender, RoutedEventArgs e)
    {
        if (_config == null) _config = new AppConfig();
        
        // Save hotkey settings
        if (!string.IsNullOrEmpty(_currentHotkey) && _currentHotkey != "请按下快捷键...")
        {
            var parts = _currentHotkey.Split('+');
            string modifier = "";
            string key = "";
            
            foreach (var part in parts)
            {
                if (part == "Ctrl" || part == "Alt" || part == "Shift" || part == "Win")
                    modifier = part;
                else
                    key = part;
            }
            
            _config.Hotkey = new HotkeyConfig
            {
                Modifier = modifier,
                Key = key
            };
        }
        
        _config.Commands = Commands.ToList();
        ConfigLoader.Save(_config);
        ToastService.Instance.ShowSuccess("已保存");
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}