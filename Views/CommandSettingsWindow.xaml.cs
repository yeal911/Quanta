// ============================================================================
// 文件名：CommandSettingsWindow.xaml.cs
// 文件用途：命令设置窗口的代码隐藏文件，提供命令的增删改查、导入导出功能，
//          以及全局快捷键的配置。支持明暗主题切换和命令搜索过滤。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Quanta.Models;
using Quanta.Helpers;
using Quanta.Services;

namespace Quanta.Views;

/// <summary>
/// 命令设置窗口，允许用户管理自定义命令（增、删、编辑、导入、导出）、
/// 配置全局快捷键，并支持命令搜索过滤和明暗主题切换。
/// </summary>
public partial class CommandSettingsWindow : Window
{
    /// <summary>命令配置的可观察集合，绑定到 DataGrid 控件</summary>
    public ObservableCollection<CommandConfig> Commands { get; set; } = new();

    /// <summary>命令集合的视图，支持过滤和排序</summary>
    private ICollectionView? _commandsView;

    /// <summary>命令搜索框中的当前文本</summary>
    private string _commandSearchText = "";

    /// <summary>当前加载的应用配置</summary>
    private AppConfig? _config;

    /// <summary>当前配置的快捷键字符串（如 "Alt+Space"）</summary>
    private string _currentHotkey = "";

    /// <summary>当前是否为暗色主题</summary>
    private bool _isDarkTheme;

    /// <summary>
    /// 构造函数，初始化组件、加载配置、应用本地化文本，
    /// 并设置命令列表的过滤视图。
    /// </summary>
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

    /// <summary>
    /// 窗口加载完成后播放显示动画
    /// </summary>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Fancy show animation: scale + fade in on the root border
        RootBorder.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        var scaleTransform = new System.Windows.Media.ScaleTransform(1, 1);
        RootBorder.RenderTransform = scaleTransform;

        RootBorder.Opacity = 0;
        var fadeIn = new System.Windows.Media.Animation.DoubleAnimation { From = 0, To = 1, Duration = System.TimeSpan.FromMilliseconds(100) };
        var scaleIn = new System.Windows.Media.Animation.DoubleAnimation { From = 0.9, To = 1, Duration = System.TimeSpan.FromMilliseconds(100) };

        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleIn);
        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleIn);
        RootBorder.BeginAnimation(System.Windows.UIElement.OpacityProperty, fadeIn);
    }

    /// <summary>
    /// 设置当前主题（明/暗），并立即应用主题样式
    /// </summary>
    /// <param name="isDark">是否为暗色主题</param>
    public void SetDarkTheme(bool isDark)
    {
        _isDarkTheme = isDark;
        ApplyTheme();
    }

    /// <summary>
    /// 根据 _isDarkTheme 标志，统一应用明暗主题的颜色方案
    /// 确保在任何主题下，表格文字、选中行、悬浮行都清晰可见
    /// </summary>
    private void ApplyTheme()
    {
        if (_isDarkTheme)
        {
            // === 暗色主题 ===
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

            // DataGrid 整体前景色（未选中行的文字颜色）
            CommandsGrid.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220));
            CommandsGrid.RowBackground = System.Windows.Media.Brushes.Transparent;
            CommandsGrid.AlternatingRowBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 38, 38));

            // 暗色主题：选中行黄色背景，悬停行深色背景
            if (Resources["SelectedRowBackgroundBrush"] is System.Windows.Media.SolidColorBrush selectedBrush)
                selectedBrush.Color = System.Windows.Media.Color.FromRgb(255, 215, 0); // 黄色
            if (Resources["HoverRowBackgroundBrush"] is System.Windows.Media.SolidColorBrush hoverBrush)
                hoverBrush.Color = System.Windows.Media.Color.FromRgb(50, 50, 50); // 暗色悬停
        }
        else
        {
            // === 亮色主题 ===
            RootBorder.Background = System.Windows.Media.Brushes.White;
            RootBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224));
            TitleText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            HotkeyLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102));
            HotkeyTextBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            HotkeyTextBox.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 221, 221));
            CommandSearchBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            CommandSearchBox.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 221, 221));
            CommandSearchPlaceholder.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(187, 187, 187));
            FooterText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170));

            CommandsGrid.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 51, 51));
            CommandsGrid.RowBackground = System.Windows.Media.Brushes.Transparent;
            CommandsGrid.AlternatingRowBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 248, 248));

            // 亮色主题：选中行橙色背景，悬停行浅色背景
            if (Resources["SelectedRowBackgroundBrush"] is System.Windows.Media.SolidColorBrush selectedBrush)
                selectedBrush.Color = System.Windows.Media.Color.FromRgb(230, 126, 34); // 橙色
            if (Resources["HoverRowBackgroundBrush"] is System.Windows.Media.SolidColorBrush hoverBrush)
                hoverBrush.Color = System.Windows.Media.Color.FromRgb(240, 246, 255); // 亮色悬停
        }
    }

    /// <summary>
    /// 命令列表的过滤器函数。根据搜索文本对命令的关键字、名称、类型、路径进行模糊匹配。
    /// </summary>
    /// <param name="obj">待过滤的对象</param>
    /// <returns>如果匹配搜索条件返回 true</returns>
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

    /// <summary>
    /// 命令搜索框文本变更事件处理。更新搜索文本、切换占位符和清除按钮的可见性，并刷新过滤视图。
    /// </summary>
    private void CommandSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _commandSearchText = CommandSearchBox.Text;
        CommandSearchPlaceholder.Visibility = string.IsNullOrEmpty(_commandSearchText)
            ? Visibility.Visible : Visibility.Collapsed;
        SearchClearBtn.Visibility = string.IsNullOrEmpty(_commandSearchText)
            ? Visibility.Collapsed : Visibility.Visible;
        _commandsView?.Refresh();
    }

    /// <summary>
    /// 搜索清除按钮点击事件处理，清空搜索框并重新聚焦。
    /// </summary>
    private void SearchClearBtn_Click(object sender, MouseButtonEventArgs e)
    {
        CommandSearchBox.Text = "";
        CommandSearchBox.Focus();
    }

    /// <summary>
    /// DataGrid 加载行事件处理，为每行设置序号。
    /// </summary>
    private void CommandsGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        // Also set the Index property for the bound command
        if (e.Row.Item is CommandConfig cmd)
        {
            cmd.Index = e.Row.GetIndex() + 1;
        }
    }

    /// <summary>
    /// 应用本地化文本到窗口中的所有 UI 元素（标题、按钮、列头、页脚等）。
    /// </summary>
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

    /// <summary>
    /// 从配置文件加载应用配置，填充命令列表和快捷键显示。
    /// 命令按修改时间倒序排列。
    /// </summary>
    private void LoadConfig()
    {
        _config = ConfigLoader.Load();
        if (_config?.Commands != null)
            Commands = new ObservableCollection<CommandConfig>(
                _config.Commands.OrderByDescending(c => c.ModifiedAt));

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

    /// <summary>
    /// 将修饰键和主键格式化为可读的快捷键字符串（如 "Alt+Space"）。
    /// </summary>
    /// <param name="modifier">修饰键（如 "Alt"、"Ctrl"）</param>
    /// <param name="key">主键（如 "Space"）</param>
    /// <returns>格式化后的快捷键字符串</returns>
    private string FormatHotkey(string modifier, string key)
    {
        if (string.IsNullOrEmpty(modifier) && string.IsNullOrEmpty(key))
            return "";
        return $"{modifier}+{key}".TrimStart('+');
    }

    /// <summary>
    /// 窗口鼠标左键按下事件处理，允许拖动窗口。
    /// </summary>
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    /// <summary>
    /// 快捷键输入框获取焦点事件处理，显示提示文本并全选。
    /// </summary>
    private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        HotkeyTextBox.Text = LocalizationService.Get("HotkeyPress");
        HotkeyTextBox.SelectAll();
    }

    /// <summary>
    /// 快捷键输入框键盘按下预处理事件。
    /// 捕获修饰键 + 主键的组合，格式化后显示并自动保存。
    /// 忽略单独的修饰键按下。
    /// </summary>
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

        // Auto-save hotkey
        SaveHotkey();
    }

    /// <summary>
    /// 保存当前配置的快捷键到配置文件。
    /// 解析快捷键字符串，分离修饰键和主键后写入配置。
    /// </summary>
    private void SaveHotkey()
    {
        if (string.IsNullOrEmpty(_currentHotkey) || _currentHotkey == LocalizationService.Get("HotkeyPress"))
            return;

        var config = ConfigLoader.Load();
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

        config.Hotkey = new HotkeyConfig { Modifier = modifier, Key = key };
        ConfigLoader.Save(config);
    }

    /// <summary>
    /// 添加命令按钮点击事件处理。
    /// 创建一个空白命令插入到列表顶部，并自动进入编辑模式。
    /// </summary>
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

    /// <summary>
    /// DataGrid 选中单元格变更事件处理，更新选中命令的修改时间。
    /// </summary>
    private void CommandsGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
    {
        if (CommandsGrid.SelectedItem is CommandConfig selected)
        {
            selected.ModifiedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// DataGrid 双击事件处理，预留行编辑行为扩展。
    /// </summary>
    private void CommandsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Ensure the row is selected when double-clicking
        if (CommandsGrid.SelectedItem != null)
        {
            // Can add edit behavior here if needed
        }
    }

    /// <summary>
    /// 删除命令按钮点击事件处理，移除当前选中的命令。
    /// </summary>
    private void DeleteCommand_Click(object sender, RoutedEventArgs e)
    {
        if (CommandsGrid?.SelectedItem is CommandConfig selected)
        {
            Commands.Remove(selected);
            ToastService.Instance.ShowSuccess(LocalizationService.Get("Deleted"));
        }
    }

    /// <summary>
    /// 保存命令按钮点击事件处理。
    /// 将当前命令列表写入配置文件并关闭设置窗口。
    /// </summary>
    private void SaveCommand_Click(object sender, RoutedEventArgs e)
    {
        // Only save commands; hotkey is auto-saved when user sets it
        var config = ConfigLoader.Load();
        config.Commands = Commands.ToList();
        ConfigLoader.Save(config);
        ToastService.Instance.ShowSuccess(LocalizationService.Get("Saved"));
        Close();
    }

    /// <summary>
    /// 导出命令按钮点击事件处理。
    /// 打开文件保存对话框，将当前命令列表导出为 JSON 文件。
    /// </summary>
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

    /// <summary>
    /// 导入命令按钮点击事件处理。
    /// 打开文件选择对话框，从 JSON 文件导入命令。
    /// 自动跳过已存在的同名关键字命令，避免重复。
    /// </summary>
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

    /// <summary>
    /// 取消按钮点击事件处理，直接关闭设置窗口（不保存更改）。
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
