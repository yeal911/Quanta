/// <summary>
/// 搜索结果评分器
/// 负责计算查询字符串与目标字符串之间的模糊匹配分数。
/// </summary>

namespace Quanta.Services;

/// <summary>
/// 搜索结果评分器接口
/// </summary>
public interface ISearchResultScorer
{
    /// <summary>
    /// 计算模糊匹配分数
    /// </summary>
    /// <param name="query">用户输入的搜索关键词</param>
    /// <param name="target">待匹配的目标字符串</param>
    /// <returns>匹配分数，范围 0.0 ~ 1.0</returns>
    double CalculateFuzzyScore(string query, string target);
}

/// <summary>
/// 搜索结果评分器默认实现
/// </summary>
public sealed class SearchResultScorer : ISearchResultScorer
{
    public static readonly SearchResultScorer Instance = new();

    private SearchResultScorer() { }

    /// <summary>
    /// 计算模糊匹配分数
    /// 匹配逻辑：完全包含(1.0) > 前缀匹配(0.9) > 逐字符顺序匹配(按匹配比例 * 0.7 计算)
    /// </summary>
    public double CalculateFuzzyScore(string query, string target)
    {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target))
            return 0;

        query = query.ToLower();
        target = target.ToLower();

        // 完全包含匹配，得分最高
        if (target.Contains(query)) return 1.0;

        // 前缀匹配
        if (target.StartsWith(query)) return 0.9;

        // 逐字符顺序模糊匹配：按顺序在目标中查找查询的每个字符
        int matchedChars = 0;
        int targetIndex = 0;
        foreach (char c in query)
        {
            int foundIndex = target.IndexOf(c, targetIndex);
            if (foundIndex >= 0)
            {
                matchedChars++;
                targetIndex = foundIndex + 1;
            }
        }

        // 按匹配字符占比计算分数，乘以 0.7 作为模糊匹配的权重折扣
        return matchedChars > 0 ? (double)matchedChars / query.Length * 0.7 : 0;
    }
}
