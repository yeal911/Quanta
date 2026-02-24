// ============================================================================
// 文件名：CommandSettingsWindow.Commands.cs
// 文件用途：命令管理面板的 CRUD、导入/导出、测试及配置加载逻辑。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Quanta.Helpers;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.Views;

public partial class CommandSettingsWindow
{
    /// <summary>
    /// 命令列表的过滤器函数。根据搜索文本对命令的关键字、名称、类型、路径进行模糊匹配。
    /// </summary>
    private bool CommandFilter(object obj)
    {
        if (obj is not CommandConfig cmd) return false;
        if (string.IsNullOrWhiteSpace(_commandSearchText)) return true;
        var q = _commandSearchText;
        return cmd.Keyword.Contains(q, StringComparison.OrdinalIgnoreCase)
            || cmd.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || cmd.Type.Contains(q, StringComparison.OrdinalIgnoreCase)
            || cmd.Path.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// DataGrid 加载行事件处理，为每行设置序号。
    /// </summary>
    private void CommandsGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        if (e.Row.Item is CommandConfig cmd)
            cmd.Index = e.Row.GetIndex() + 1;
    }

    /// <summary>
    /// DataGrid 选中单元格变更事件处理，更新选中命令的修改时间。
    /// </summary>
    private void CommandsGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
    {
        if (CommandsGrid.SelectedItem is CommandConfig selected)
            selected.ModifiedAt = DateTime.Now;
    }

    /// <summary>DataGrid 双击事件处理，预留行编辑行为扩展。</summary>
    private void CommandsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Ensure the row is selected when double-clicking
        if (CommandsGrid.SelectedItem != null)
        {
            // Can add edit behavior here if needed
        }
    }

    /// <summary>
    /// 从配置文件加载应用配置，填充命令列表和快捷键显示。
    /// 命令按修改时间倒序排列。
    /// </summary>
    private void LoadConfig()
    {
        Logger.Debug("Loading config...");
        _config = ConfigLoader.Load();

        if (_config?.Commands != null)
        {
            Commands = new ObservableCollection<CommandConfig>(
                _config.Commands.OrderByDescending(c => c.ModifiedAt));
            Logger.Debug($"Loaded {Commands.Count} commands into ObservableCollection");
            if (Commands.Count > 0)
            {
                var commandList = string.Join(", ", Commands.Select(c => $"{c.Keyword}({c.Name})"));
                Logger.Debug($"Commands in UI: {commandList}");
            }
        }
        else
        {
            Logger.Debug("No commands found in config");
            Commands = new ObservableCollection<CommandConfig>();
        }

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

        LoadAppSettings();

        _suppressRecordingEvents = true;
        LoadRecordingSettings();
        _suppressRecordingEvents = false;

        LoadExchangeRateSettings();
    }

    // ── CRUD ──────────────────────────────────────────────────────────

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
            RunAsAdmin = false,
            RunHidden = false,
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
    /// 删除命令按钮点击事件处理，移除当前选中的命令。
    /// </summary>
    private void DeleteCommand_Click(object sender, RoutedEventArgs e)
    {
        if (CommandsGrid?.SelectedItem is CommandConfig selected)
        {
            Commands.Remove(selected);
            SaveCommandsToConfigAndRefresh();
            ToastService.Instance.ShowSuccess(LocalizationService.Get("Deleted"));
        }
    }

    /// <summary>
    /// 保存命令按钮点击事件处理。
    /// 将当前命令列表写入配置文件，不关闭设置窗口。
    /// </summary>
    private void SaveCommand_Click(object sender, RoutedEventArgs e)
    {
        SaveCommandsToConfigAndRefresh();
        ShowAutoSaveToast();
    }


    /// <summary>
    /// 持久化命令并刷新搜索引擎内存缓存，确保主窗口即时可搜索。
    /// </summary>
    private void SaveCommandsToConfigAndRefresh()
    {
        var config = ConfigLoader.Load();
        config.Commands = Commands.Where(c => !c.IsBuiltIn).ToList();
        ConfigLoader.Save(config);
        _searchEngine?.ReloadCommands();
    }

    // ── 导入/导出 ─────────────────────────────────────────────────────

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
            var userCommands = Commands.Where(c => !c.IsBuiltIn).ToList();
            var success = await CommandService.ExportCommandsAsync(dialog.FileName, userCommands);
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

            SaveCommandsToConfigAndRefresh();
            ToastService.Instance.ShowSuccess(LocalizationService.Get("ImportResult", added, skipped));
        }
    }

    // ── 测试 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 测试命令按钮点击事件处理。
    /// 获取当前选中的命令，调用搜索引擎执行，并以 Toast 通知显示执行结果。
    /// </summary>
    private async void TestCommand_Click(object sender, RoutedEventArgs e)
    {
        if (CommandsGrid.SelectedItem is not CommandConfig cmd)
        {
            ToastService.Instance.ShowWarning("请先选中要测试的命令");
            return;
        }

        if (_searchEngine == null)
        {
            ToastService.Instance.ShowWarning("测试功能不可用");
            return;
        }

        var result = new SearchResult
        {
            Id = $"cmd:{cmd.Keyword}",
            Title = cmd.Keyword,
            Type = SearchResultType.CustomCommand,
            CommandConfig = cmd,
            Path = cmd.Path
        };

        TestButton.IsEnabled = false;
        try
        {
            bool success = await _searchEngine.ExecuteCustomCommandAsync(result, "");
            if (success)
                ToastService.Instance.ShowSuccess(LocalizationService.Get("TestExecuted") + $": {cmd.Name}");
            else
                ToastService.Instance.ShowError(LocalizationService.Get("TestFailed") + $": {cmd.Name}");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError($"{LocalizationService.Get("TestFailed")}: {ex.Message}");
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }
}
