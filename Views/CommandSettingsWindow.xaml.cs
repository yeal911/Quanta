using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Quanta.Models;
using Quanta.Helpers;
using Quanta.Services;

namespace Quanta.Views;

public partial class CommandSettingsWindow : Window
{
    public ObservableCollection<CommandConfig> Commands { get; set; } = new();
    private AppConfig? _config;

    public CommandSettingsWindow()
    {
        InitializeComponent();
        _config = ConfigLoader.Load();
        if (_config?.Commands != null)
            Commands = new ObservableCollection<CommandConfig>(_config!.Commands);
        DataContext = this;
    }

    private void AddCommand_Click(object sender, RoutedEventArgs e)
    {
        var cmd = new CommandConfig 
        { 
            Keyword = "new", 
            Name = "新命令", 
            Type = "Url", 
            Path = "https://www.example.com/search?q={param}"
        };
        Commands.Add(cmd);
    }

    private void DeleteCommand_Click(object sender, RoutedEventArgs e)
    {
        var grid = FindName("CommandsGrid") as System.Windows.Controls.DataGrid;
        if (grid?.SelectedItem is CommandConfig selected)
            Commands.Remove(selected);
    }

    private void SaveCommand_Click(object sender, RoutedEventArgs e)
    {
        if (_config == null) _config = new AppConfig();
        _config.Commands = Commands.ToList();
        ConfigLoader.Save(_config);
        
        // Reload commands in search engine
        Logger.Log("Commands saved, reloading...");
        
        System.Windows.MessageBox.Show("命令已保存。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
