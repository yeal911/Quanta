using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Quanta.Models;
using Quanta.Helpers;
using Quanta.Services;

namespace Quanta.Views;

public partial class CommandSettingsWindow : Window
{
    public ObservableCollection<CommandConfig> Commands { get; set; } = new();
    private ICollectionView? _commandsView;
    private string _commandSearchText = "";
    private AppConfig? _config;
    private string _currentHotkey = "";
    private bool _isDarkTheme;

    public CommandSettingsWindow()
    {
        InitializeComponent();
        LoadConfig();
        ApplyLocalization();
        DataContext = this;

        _commandsView = CollectionViewSource.GetDefaultView(Commands);
        _commandsView.Filter = CommandFilter;
        CommandsGrid.ItemsSource = _commandsView;
    }

    public void SetDarkTheme(bool isDark)
    {
        _isDarkTheme = isDark;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (_isDarkTheme)
        {
            RootBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            RootBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
            TitleText.Foreground = System.Windows.Media.Brushes.White;
            HotkeyLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170));
            HotkeyTextBox.Foreground = System.Windows.Media.Brushes.White;
            HotkeyTextBox.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
            CommandSearchBox.Foreground = System.Windows.Media.Brushes.White;
            CommandSearchBox.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
            CommandSearchPlaceholder.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));
            FooterText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));
            CommandsGrid.Foreground = System.Windows.Media.Brushes.White;
            CommandsGrid.RowBackground = System.Windows.Media.Brushes.Transparent;
            CommandsGrid.AlternatingRowBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 38, 38));
        }
    }

    private bool CommandFilter(object obj)
    {
        if (string.IsNullOrWhiteSpace(_commandSearchText)) return true;
        if (obj is not CommandConfig cmd) return false;
        var q = _commandSearchText;
        return cmd.Keyword.Contains(q, StringComparison.OrdinalIgnoreCase)
            || cmd.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || cmd.Type.Contains(q, StringComparison.OrdinalIgnoreCase)
            || cmd.Path.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private void CommandSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _commandSearchText = CommandSearchBox.Text;
        CommandSearchPlaceholder.Visibility = string.IsNullOrEmpty(_commandSearchText)
            ? Visibility.Visible : Visibility.Collapsed;
        SearchClearBtn.Visibility = string.IsNullOrEmpty(_commandSearchText)
            ? Visibility.Collapsed : Visibility.Visible;
        _commandsView?.Refresh();
    }

    private void SearchClearBtn_Click(object sender, MouseButtonEventArgs e)
    {
        CommandSearchBox.Text = "";
        CommandSearchBox.Focus();
    }

    private void ApplyLocalization()
    {
        TitleText.Text = LocalizationService.Get("SettingsTitle");
        HotkeyLabel.Text = LocalizationService.Get("HotkeyLabel") + "：";
        ImportButton.Content = LocalizationService.Get("ImportCommand");
        ExportButton.Content = LocalizationService.Get("ExportCommand");
        AddButton.Content = LocalizationService.Get("AddCommand");
        DeleteButton.Content = LocalizationService.Get("DeleteCommand");
        SaveButton.Content = LocalizationService.Get("SaveCommand");
        KeywordColumn.Header = LocalizationService.Get("Keyword");
        NameColumn.Header = LocalizationService.Get("Name");
        TypeColumn.Header = LocalizationService.Get("Type");
        PathColumn.Header = LocalizationService.Get("Path");
        FooterText.Text = LocalizationService.Get("Footer");
        CommandSearchPlaceholder.Text = LocalizationService.Get("CommandSearchPlaceholder");
    }

    private void LoadConfig()
    {
        _config = ConfigLoader.Load();
        if (_config?.Commands != null)
            Commands = new ObservableCollection<CommandConfig>(
                _config.Commands.OrderByDescending(c => c.ModifiedAt));

        // Show current hotkey
        if (_config?.Hotkey != null)
        {
            _currentHotkey = FormatHotkey(_config.Hotkey.Modifier, _config.Hotkey.Key);
            HotkeyTextBox.Text = string.IsNullOrEmpty(_currentHotkey)
                ? LocalizationService.Get("HotkeyPress") : _currentHotkey;
        }
        else
        {
            HotkeyTextBox.Text = LocalizationService.Get("HotkeyPress");
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
        HotkeyTextBox.Text = LocalizationService.Get("HotkeyPress");
        HotkeyTextBox.SelectAll();
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        var modifiers = Keyboard.Modifiers;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (modifiers == ModifierKeys.None)
        {
            HotkeyTextBox.Text = LocalizationService.Get("HotkeyPress");
            return;
        }

        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        string modifierStr = "";
        if (modifiers.HasFlag(ModifierKeys.Control)) modifierStr += "Ctrl+";
        if (modifiers.HasFlag(ModifierKeys.Alt)) modifierStr += "Alt+";
        if (modifiers.HasFlag(ModifierKeys.Shift)) modifierStr += "Shift+";
        if (modifiers.HasFlag(ModifierKeys.Windows)) modifierStr += "Win+";

        string keyStr = key.ToString();
        _currentHotkey = modifierStr + keyStr;
        HotkeyTextBox.Text = _currentHotkey;
    }

    private void AddCommand_Click(object sender, RoutedEventArgs e)
    {
        var cmd = new CommandConfig
        {
            Keyword = "",
            Name = "",
            Type = "Url",
            Path = "",
            Enabled = true,
            ModifiedAt = DateTime.Now
        };
        Commands.Insert(0, cmd);
        CommandsGrid.SelectedIndex = 0;
        CommandsGrid.ScrollIntoView(cmd);

        CommandsGrid.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            CommandsGrid.Focus();
            CommandsGrid.CurrentCell = new DataGridCellInfo(cmd, CommandsGrid.Columns[0]);
            CommandsGrid.BeginEdit();
        });

        ToastService.Instance.ShowSuccess(LocalizationService.Get("Added"));
    }

    private void CommandsGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
    {
        if (CommandsGrid.SelectedItem is CommandConfig selected)
        {
            selected.ModifiedAt = DateTime.Now;
        }
    }

    private void DeleteCommand_Click(object sender, RoutedEventArgs e)
    {
        if (CommandsGrid?.SelectedItem is CommandConfig selected)
        {
            Commands.Remove(selected);
            ToastService.Instance.ShowSuccess(LocalizationService.Get("Deleted"));
        }
    }

    private void SaveCommand_Click(object sender, RoutedEventArgs e)
    {
        if (_config == null) _config = new AppConfig();

        if (!string.IsNullOrEmpty(_currentHotkey) && _currentHotkey != LocalizationService.Get("HotkeyPress"))
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
        ToastService.Instance.ShowSuccess(LocalizationService.Get("Saved"));
        Close();
    }

    private async void ExportCommands_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = ".json",
            FileName = "quanta-commands"
        };

        if (dialog.ShowDialog() == true)
        {
            var success = await CommandService.ExportCommandsAsync(dialog.FileName, Commands.ToList());
            if (success)
                ToastService.Instance.ShowSuccess(LocalizationService.Get("ExportSuccess"));
            else
                ToastService.Instance.ShowError(LocalizationService.Get("ExportFailed"));
        }
    }

    private async void ImportCommands_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            var (success, importedCommands, error) = await CommandService.ImportCommandsAsync(dialog.FileName);
            if (!success || importedCommands == null)
            {
                ToastService.Instance.ShowError(LocalizationService.Get("ImportFailed"));
                return;
            }

            int added = 0;
            int skipped = 0;
            var existingKeywords = new HashSet<string>(
                Commands.Select(c => c.Keyword.Trim()),
                StringComparer.OrdinalIgnoreCase);

            foreach (var cmd in importedCommands)
            {
                if (existingKeywords.Contains(cmd.Keyword.Trim()))
                {
                    skipped++;
                }
                else
                {
                    Commands.Insert(0, cmd);
                    existingKeywords.Add(cmd.Keyword.Trim());
                    added++;
                }
            }

            ToastService.Instance.ShowSuccess(LocalizationService.Get("ImportResult", added, skipped));
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
