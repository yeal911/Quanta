// ============================================================================
// 文件名: ClipboardHistoryService.cs
// 文件描述: 剪贴板历史服务（单例）。
//           记录系统剪贴板的文本变化，提供关键字搜索和持久化到磁盘功能。
// ============================================================================

using System.IO;
using System.Text.Json;
using Quanta.Core.Constants;
using Quanta.Core.Interfaces;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>单条剪贴板历史记录。</summary>
public record ClipboardEntry(string Text, DateTime Time);

/// <summary>
/// 剪贴板历史服务（单例）。
/// 维护最近 50 条文本剪贴板记录，支持关键字搜索，并持久化到本地磁盘。
/// </summary>
public class ClipboardHistoryService : IClipboardHistoryService
{
    public static readonly ClipboardHistoryService Instance = new();

    private readonly List<ClipboardEntry> _history = new();
    private const int MaxItems = 50;
    private readonly string _storagePath;
    private readonly object _lock = new();

    private ClipboardHistoryService()
    {
        _storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Quanta", "clipboard_history.json");
        LoadFromDisk();
    }

    /// <summary>
    /// 添加一条剪贴板内容。自动去重（相同内容移到最前）并限制最大条数。
    /// </summary>
    public void Add(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = text.Trim();

        lock (_lock)
        {
            // 去重：相同内容移到最前
            _history.RemoveAll(e => e.Text == text);
            _history.Insert(0, new ClipboardEntry(text, DateTime.Now));
            if (_history.Count > MaxItems)
                _history.RemoveRange(MaxItems, _history.Count - MaxItems);
        }

        // 后台持久化，不阻塞调用方
        Task.Run(SaveToDisk);
    }

    /// <summary>
    /// 按关键字搜索历史（空关键字返回最新 20 条）。
    /// 每条结果的 Data.Output 存放完整原始文本，供剪贴板设置和粘贴使用。
    /// </summary>
    public List<SearchResult> Search(string keyword)
    {
        List<ClipboardEntry> items;
        lock (_lock)
        {
            items = string.IsNullOrWhiteSpace(keyword)
                ? _history.Take(20).ToList()
                : _history
                    .Where(e => e.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .Take(20)
                    .ToList();
        }

        return items.Select((entry, idx) =>
        {
            // 显示用：单行化、截断为 80 字符
            string preview = entry.Text.ReplaceLineEndings(" ").Trim();
            if (preview.Length > 80) preview = preview[..80] + "…";

            return new SearchResult
            {
                Index = idx + 1,
                Title = preview,
                Subtitle = FormatTime(entry.Time),
                Type = SearchResultType.Calculator, // 复用 Calculator 的复制逻辑
                IconText = "📋",
                GroupLabel = LocalizationService.Get("GroupClip"),
                GroupOrder = 0,          // 剪贴板历史排在最前面
                MatchScore = 1000 - idx, // 越新分数越高
                Path = string.Empty,
                Data = new CommandResult { Success = true, Output = entry.Text }
            };
        }).ToList();
    }

    /// <summary>将复制时间格式化为精确时间字符串。今天显示 HH:mm:ss，其他日期显示 MM-dd HH:mm。</summary>
    private static string FormatTime(DateTime time)
    {
        if (time.Date == DateTime.Today)
            return time.ToString("HH:mm:ss");
        return time.ToString("MM-dd HH:mm");
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_storagePath)) return;
            var json = File.ReadAllText(_storagePath);
            var list = JsonSerializer.Deserialize<List<ClipboardEntry>>(json);
            if (list != null)
            {
                lock (_lock)
                {
                    _history.AddRange(list.Take(MaxItems));
                }
            }
        }
        catch { /* 文件损坏时忽略，从空历史开始 */ }
    }

    private void SaveToDisk()
    {
        try
        {
            List<ClipboardEntry> snapshot;
            lock (_lock) { snapshot = _history.ToList(); }

            Directory.CreateDirectory(Path.GetDirectoryName(_storagePath)!);
            var json = JsonSerializer.Serialize(snapshot, JsonDefaults.Standard);
            File.WriteAllText(_storagePath, json);
        }
        catch { /* 保存失败时静默忽略 */ }
    }
}
