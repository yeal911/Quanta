using System.Windows.Forms;

namespace Quanta.WinFormsLite;

public sealed class LauncherForm : Form
{
    private readonly SearchService _searchService;
    private readonly TextBox _searchBox;
    private readonly ListBox _resultList;
    private readonly Label _hintLabel;

    private List<SearchResult> _currentResults = [];

    public LauncherForm(AppConfig config, SearchService searchService)
    {
        _searchService = searchService;

        Text = "Quanta WinForms Lite";
        Width = 640;
        Height = 440;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        ShowInTaskbar = false;

        _searchBox = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 36,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12f)
        };

        _resultList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Font = new Font("Segoe UI", 10f)
        };

        _hintLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            Text = "Alt+R 呼出 | ↑/↓ 选择 | Enter 执行 | Esc 隐藏",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };

        Controls.Add(_resultList);
        Controls.Add(_searchBox);
        Controls.Add(_hintLabel);

        _searchBox.TextChanged += (_, _) => RefreshResults();
        _searchBox.KeyDown += OnSearchBoxKeyDown;
        _resultList.DoubleClick += (_, _) => ExecuteSelected();

        var palette = ThemeManager.Resolve(config.Theme);
        ThemeManager.Apply(this, _searchBox, _resultList, _hintLabel, palette);
    }

    public void ToggleVisibleAndFocus()
    {
        if (Visible)
        {
            HideLauncher();
            return;
        }

        Show();
        Activate();
        _searchBox.Focus();
        _searchBox.SelectAll();
    }

    private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Down when _resultList.Items.Count > 0:
                _resultList.SelectedIndex = Math.Min(_resultList.SelectedIndex + 1, _resultList.Items.Count - 1);
                e.Handled = true;
                break;
            case Keys.Up when _resultList.Items.Count > 0:
                _resultList.SelectedIndex = Math.Max(_resultList.SelectedIndex - 1, 0);
                e.Handled = true;
                break;
            case Keys.Enter:
                ExecuteSelected();
                e.Handled = true;
                break;
            case Keys.Escape:
                HideLauncher();
                e.Handled = true;
                break;
        }
    }

    private void RefreshResults()
    {
        _currentResults = _searchService.Search(_searchBox.Text);
        _resultList.BeginUpdate();
        _resultList.Items.Clear();

        foreach (var item in _currentResults)
            _resultList.Items.Add(item.ToString());

        _resultList.EndUpdate();
        if (_resultList.Items.Count > 0) _resultList.SelectedIndex = 0;
    }

    private void ExecuteSelected()
    {
        if (_resultList.SelectedIndex < 0 || _resultList.SelectedIndex >= _currentResults.Count) return;

        var chosen = _currentResults[_resultList.SelectedIndex];
        SearchService.Execute(chosen);
        HideLauncher();
    }

    private void HideLauncher()
    {
        _searchBox.Clear();
        _resultList.Items.Clear();
        Hide();
    }
}
