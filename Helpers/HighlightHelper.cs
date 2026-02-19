// ============================================================================
// 文件名: HighlightHelper.cs
// 文件用途: 提供 TextBlock 搜索关键字高亮的附加属性。
//          通过 HighlightHelper.Text 和 HighlightHelper.Query 两个附加属性，
//          自动将 TextBlock 中与搜索词匹配的字符以橙色加粗方式高亮显示。
// ============================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Quanta.Helpers;

/// <summary>
/// TextBlock 高亮附加属性辅助类。
/// 用法（XAML）:
/// <code>
///   xmlns:h="clr-namespace:Quanta.Helpers"
///   &lt;TextBlock h:HighlightHelper.Text="{Binding Title}"
///              h:HighlightHelper.Query="{Binding QueryMatch}" /&gt;
/// </code>
/// </summary>
public static class HighlightHelper
{
    // ── 附加属性：显示文本 ──────────────────────────────────
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(HighlightHelper),
            new PropertyMetadata(null, OnHighlightChanged));

    public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);
    public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);

    // ── 附加属性：匹配的搜索词 ──────────────────────────────
    public static readonly DependencyProperty QueryProperty =
        DependencyProperty.RegisterAttached(
            "Query", typeof(string), typeof(HighlightHelper),
            new PropertyMetadata(null, OnHighlightChanged));

    public static string GetQuery(DependencyObject obj) => (string)obj.GetValue(QueryProperty);
    public static void SetQuery(DependencyObject obj, string value) => obj.SetValue(QueryProperty, value);

    /// <summary>
    /// Text 或 Query 属性变更时重新渲染高亮 Inlines
    /// </summary>
    private static void OnHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock tb)
            RefreshHighlight(tb);
    }

    /// <summary>
    /// 根据 Text 和 Query 构建带高亮的 Inlines 集合。
    /// 匹配策略：优先整段连续包含匹配；其次子序列（按字符顺序）模糊匹配。
    /// </summary>
    private static void RefreshHighlight(TextBlock tb)
    {
        var text  = GetText(tb)  ?? "";
        var query = GetQuery(tb) ?? "";

        tb.Inlines.Clear();

        if (string.IsNullOrEmpty(text))
            return;

        if (string.IsNullOrEmpty(query))
        {
            tb.Inlines.Add(new Run(text));
            return;
        }

        var matchedIndices = FindMatchIndices(text, query);

        if (matchedIndices.Count == 0)
        {
            tb.Inlines.Add(new Run(text));
            return;
        }

        // 高亮颜色：深橙色，与蓝色关键字形成对比
        var highlightBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 81, 0));

        var matchSet = new HashSet<int>(matchedIndices);
        int pos = 0;

        while (pos < text.Length)
        {
            if (matchSet.Contains(pos))
            {
                // 找连续的匹配字符
                int end = pos;
                while (end < text.Length && matchSet.Contains(end)) end++;

                tb.Inlines.Add(new Run(text.Substring(pos, end - pos))
                {
                    Foreground = highlightBrush,
                    FontWeight = FontWeights.Bold
                });
                pos = end;
            }
            else
            {
                // 找连续的非匹配字符
                int end = pos;
                while (end < text.Length && !matchSet.Contains(end)) end++;

                tb.Inlines.Add(new Run(text.Substring(pos, end - pos)));
                pos = end;
            }
        }
    }

    /// <summary>
    /// 在文本中查找与查询词匹配的字符索引列表。
    /// 优先使用连续子串匹配；其次使用字符顺序模糊匹配。
    /// </summary>
    private static List<int> FindMatchIndices(string text, string query)
    {
        var textLower  = text.ToLower();
        var queryLower = query.ToLower();

        // 连续子串匹配（最高优先级，textLower 和 queryLower 已全部小写）
        int idx = textLower.IndexOf(queryLower, StringComparison.Ordinal);
        if (idx >= 0)
        {
            var indices = new List<int>(queryLower.Length);
            for (int i = 0; i < queryLower.Length; i++)
                indices.Add(idx + i);
            return indices;
        }

        // 模糊子序列匹配（按字符顺序，已小写无需 StringComparison）
        var fuzzy = new List<int>();
        int textIdx = 0;
        foreach (char c in queryLower)
        {
            int found = textLower.IndexOf(c, textIdx); // textLower 已全小写，直接比较
            if (found < 0) return new List<int>(); // 不匹配
            fuzzy.Add(found);
            textIdx = found + 1;
        }
        return fuzzy;
    }
}
