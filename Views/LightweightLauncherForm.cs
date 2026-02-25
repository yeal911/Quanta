// ============================================================================
// 文件名：LightweightLauncherForm.cs
// 文件用途：WinForms 轻量启动器界面，用于在低内存模式下替代 WPF 主窗口。
// ============================================================================

using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Quanta.Models;
using Quanta.ViewModels;

namespace Quanta.Views;

/// <summary>
/// 基于 WinForms 的轻量主界面。
/// 通过复用 MainViewModel 保持搜索与命令执行逻辑一致，仅替换渲染层。
/// </summary>
public class LightweightLauncherForm : Form
{
    private readonly MainViewModel _viewModel;
    private readonly TextBox _searchBox;
    private readonly ListBox _resultList;
    private readonly Label _tipLabel;

    public LightweightLauncherForm(MainViewModel viewModel)
    {
        _viewModel = viewModel;

        Text = "Quanta Lite";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        Width = 620;
        Height = 420;

        _searchBox = new TextBox
        {
            Dock = DockStyle.Top,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12f),
            Margin = new Padding(12),
        };

        _tipLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            Text = "↑/↓ 选择，Enter 执行，Esc 清空",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
        };

        _resultList = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f),
            IntegralHeight = false,
        };

        Controls.Add(_resultList);
        Controls.Add(_searchBox);
        Controls.Add(_tipLabel);

        _searchBox.TextChanged += OnSearchTextChanged;
        _searchBox.KeyDown += OnSearchKeyDown;
        _resultList.DoubleClick += (_, _) => ExecuteSelected();
        _resultList.KeyDown += OnResultListKeyDown;

        _viewModel.Results.CollectionChanged += OnResultsChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        FormClosed += (_, _) =>
        {
            _viewModel.Results.CollectionChanged -= OnResultsChanged;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        };

        ApplyTheme(_viewModel.IsDarkTheme);
        _viewModel.ThemeChanged += (_, isDark) => ApplyTheme(isDark);
    }

    private void OnSearchTextChanged(object? sender, EventArgs e)
    {
        _viewModel.SearchText = _searchBox.Text;
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Down:
                _viewModel.SelectNextCommand.Execute(null);
                e.Handled = true;
                break;
            case Keys.Up:
                _viewModel.SelectPreviousCommand.Execute(null);
                e.Handled = true;
                break;
            case Keys.Enter:
                ExecuteSelected();
                e.Handled = true;
                break;
            case Keys.Escape:
                _viewModel.ClearSearch();
                _searchBox.Clear();
                e.Handled = true;
                break;
        }
    }

    private void OnResultListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            ExecuteSelected();
            e.Handled = true;
        }
    }

    private void OnResultsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(RefreshResultList));
            return;
        }

        RefreshResultList();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedIndex) && _viewModel.SelectedIndex >= 0 && _viewModel.SelectedIndex < _resultList.Items.Count)
        {
            _resultList.SelectedIndex = _viewModel.SelectedIndex;
        }
    }

    private void RefreshResultList()
    {
        _resultList.BeginUpdate();
        _resultList.Items.Clear();

        foreach (var result in _viewModel.Results)
        {
            _resultList.Items.Add(ToDisplayText(result));
        }

        _resultList.EndUpdate();

        if (_viewModel.SelectedIndex >= 0 && _viewModel.SelectedIndex < _resultList.Items.Count)
        {
            _resultList.SelectedIndex = _viewModel.SelectedIndex;
        }
    }

    private static string ToDisplayText(SearchResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Subtitle))
        {
            return $"{result.IconText} {result.Title}".Trim();
        }

        return $"{result.IconText} {result.Title}  —  {result.Subtitle}".Trim();
    }

    private void ExecuteSelected()
    {
        if (_resultList.SelectedIndex < 0)
        {
            return;
        }

        _viewModel.SelectedIndex = _resultList.SelectedIndex;
        _viewModel.ExecuteSelectedCommand.Execute(null);
        _searchBox.Clear();
    }

    private void ApplyTheme(bool isDark)
    {
        var back = isDark ? Color.FromArgb(28, 28, 30) : Color.White;
        var text = isDark ? Color.Gainsboro : Color.Black;
        var panel = isDark ? Color.FromArgb(44, 44, 46) : Color.FromArgb(244, 244, 244);

        BackColor = back;
        ForeColor = text;

        _searchBox.BackColor = panel;
        _searchBox.ForeColor = text;

        _resultList.BackColor = panel;
        _resultList.ForeColor = text;

        _tipLabel.BackColor = back;
        _tipLabel.ForeColor = isDark ? Color.DarkGray : Color.DimGray;
    }
}
