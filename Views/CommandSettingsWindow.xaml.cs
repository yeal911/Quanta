using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Quanta.Models;
using Quanta.Helpers;
using Quanta.Services;
using WinDialog = Microsoft.Win32;

namespace Quanta.Views;

public partial class CommandSettingsWindow : Window
{
    public ObservableCollection<CommandConfig> Commands { get; set; } = new();
    public ObservableCollection<CommandGroup> Groups { get; set; } = new();
    private AppConfig? _config;

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
        if (_config?.CommandGroups != null)
            Groups = new ObservableCollection<CommandGroup>(_config.CommandGroups);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (NavCommands == null || NavGroups == null || NavImportExport == null || NavAdvanced == null)
            return;

        CommandsPanel.Visibility = NavCommands.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        GroupsPanel.Visibility = NavGroups.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        ImportExportPanel.Visibility = NavImportExport.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        AdvancedPanel.Visibility = NavAdvanced.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddCommand_Click(object sender, RoutedEventArgs e)
    {
        var cmd = new CommandConfig 
        { 
            Keyword = "new", 
            Name = "新命令", 
            Type = "Url", 
            Path = "https://www.example.com/search?q={param}",
            Enabled = true
        };
        Commands.Add(cmd);
        ToastService.Instance.ShowSuccess("已添加新命令");
    }

    private void DeleteCommand_Click(object sender, RoutedEventArgs e)
    {
        if (CommandsGrid?.SelectedItem is CommandConfig selected)
        {
            Commands.Remove(selected);
            ToastService.Instance.ShowSuccess("已删除命令");
        }
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (CommandsGrid?.SelectedItem is CommandConfig selected)
        {
            var index = Commands.IndexOf(selected);
            if (index > 0)
            {
                Commands.Move(index, index - 1);
            }
        }
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (CommandsGrid?.SelectedItem is CommandConfig selected)
        {
            var index = Commands.IndexOf(selected);
            if (index < Commands.Count - 1)
            {
                Commands.Move(index, index + 1);
            }
        }
    }

    private void AddGroup_Click(object sender, RoutedEventArgs e)
    {
        var group = new CommandGroup
        {
            Name = "新分组",
            Icon = "📁",
            Color = "#0078D4",
            SortOrder = Groups.Count + 1,
            Expanded = true
        };
        Groups.Add(group);
        ToastService.Instance.ShowSuccess("已添加新分组");
    }

    private void DeleteGroup_Click(object sender, RoutedEventArgs e)
    {
        if (GroupsGrid?.SelectedItem is CommandGroup selected)
        {
            Groups.Remove(selected);
            ToastService.Instance.ShowSuccess("已删除分组");
        }
    }

    private void ExportCommands_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WinDialog.SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = "json",
            FileName = "commands.json"
        };
        
        if (dialog.ShowDialog() == true)
        {
            var success = CommandService.ExportCommandsAsync(dialog.FileName, Commands.ToList()).Result;
            if (success)
                ToastService.Instance.ShowSuccess("命令导出成功！");
            else
                ToastService.Instance.ShowError("命令导出失败！");
        }
    }

    private void ExportAll_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WinDialog.SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = "json",
            FileName = "quanta_config.json"
        };
        
        if (dialog.ShowDialog() == true)
        {
            if (_config == null) _config = new AppConfig();
            _config.Commands = Commands.ToList();
            _config.CommandGroups = Groups.ToList();
            
            var success = CommandService.ExportAllAsync(dialog.FileName, _config).Result;
            if (success)
                ToastService.Instance.ShowSuccess("配置导出成功！");
            else
                ToastService.Instance.ShowError("配置导出失败！");
        }
    }

    private void ImportCommands_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WinDialog.OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = "json"
        };
        
        if (dialog.ShowDialog() == true)
        {
            var result = CommandService.ImportCommandsAsync(dialog.FileName).Result;
            if (result.Success && result.Commands != null)
            {
                foreach (var cmd in result.Commands)
                {
                    Commands.Add(cmd);
                }
                ToastService.Instance.ShowSuccess($"成功导入 {result.Commands.Count} 条命令！");
            }
            else
            {
                ToastService.Instance.ShowError($"导入失败: {result.Error}");
            }
        }
    }

    private void ImportAll_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WinDialog.OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = "json"
        };
        
        if (dialog.ShowDialog() == true)
        {
            var result = CommandService.ImportAllAsync(dialog.FileName).Result;
            if (result.Success && result.Config != null)
            {
                Commands = new ObservableCollection<CommandConfig>(result.Config.Commands);
                Groups = new ObservableCollection<CommandGroup>(result.Config.CommandGroups);
                DataContext = this;
                CommandsGrid.ItemsSource = Commands;
                GroupsGrid.ItemsSource = Groups;
                ToastService.Instance.ShowSuccess("配置导入成功！");
            }
            else
            {
                ToastService.Instance.ShowError($"导入失败: {result.Error}");
            }
        }
    }

    private void LoadSampleCommands_Click(object sender, RoutedEventArgs e)
    {
        var samples = CommandService.GenerateSampleCommands();
        int added = 0;
        foreach (var cmd in samples)
        {
            if (!Commands.Any(c => c.Keyword == cmd.Keyword))
            {
                Commands.Add(cmd);
                added++;
            }
        }
        if (added > 0)
            ToastService.Instance.ShowSuccess($"已添加 {added} 条示例命令！");
        else
            ToastService.Instance.ShowInfo("所有示例命令已存在");
    }

    private void SaveAdvanced_Click(object sender, RoutedEventArgs e)
    {
        if (CommandsGrid?.SelectedItem is CommandConfig selected)
        {
            selected.Hotkey = HotkeyTextBox.Text;
            selected.IconPath = IconPathTextBox.Text;
            selected.Description = DescriptionTextBox.Text;
            selected.ParamPlaceholder = ParamPlaceholderTextBox.Text;
            selected.RunHidden = RunHiddenCheckBox.IsChecked ?? false;
            
            ToastService.Instance.ShowSuccess("高级属性已保存！");
        }
        else
        {
            ToastService.Instance.ShowWarning("请先选择一条命令！");
        }
    }

    private void SaveCommand_Click(object sender, RoutedEventArgs e)
    {
        if (_config == null) _config = new AppConfig();
        _config.Commands = Commands.ToList();
        _config.CommandGroups = Groups.ToList();
        ConfigLoader.Save(_config);
        
        Logger.Log("Commands saved, reloading...");
        
        ToastService.Instance.ShowSuccess("设置已保存！");
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}