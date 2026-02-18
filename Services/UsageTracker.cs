// ============================================================================
// 文件名: UsageTracker.cs
// 文件用途: 提供用户使用频率跟踪服务，记录每个搜索项的使用次数和最后使用时间。
//          数据以 JSON 格式持久化存储到本地应用数据目录中，
//          并支持基于使用频率和最近使用时间计算搜索结果的相关性评分。
// ============================================================================

using System.IO;
using System.Text.Json;
using System.Timers;
using Quanta.Models;
using Timer = System.Timers.Timer;

namespace Quanta.Services;

/// <summary>
/// 使用频率跟踪器，负责记录和管理用户对各搜索项的使用情况。
/// 包括使用次数统计、最后使用时间记录、使用评分计算等功能。
/// 数据定期自动保存到本地 JSON 文件中，实现 IDisposable 接口以确保退出时数据不丢失。
/// </summary>
public class UsageTracker : IDisposable
{
    /// <summary>
    /// 使用数据 JSON 文件的完整路径
    /// </summary>
    private readonly string _dataFilePath;

    /// <summary>
    /// 当前加载的使用数据对象，存储所有项目的使用记录
    /// </summary>
    private UsageData _usageData;

    /// <summary>
    /// 用于保证多线程访问使用数据时线程安全的锁对象
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// 定时保存计时器，每 30 秒检查一次是否需要将数据写入磁盘
    /// </summary>
    private readonly Timer _saveTimer;

    /// <summary>
    /// 标记自上次保存以来数据是否有变更
    /// </summary>
    private bool _hasChanges;

    /// <summary>
    /// 标记对象是否已被释放
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 构造函数，初始化使用跟踪器。
    /// 创建数据存储目录（如不存在），加载已有的使用数据，并启动定时保存计时器。
    /// </summary>
    public UsageTracker()
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Quanta");
        Directory.CreateDirectory(appDataPath);
        _dataFilePath = Path.Combine(appDataPath, "usage.json");
        _usageData = LoadData();

        // 每 30 秒自动保存一次（如果数据有变更）
        _saveTimer = new Timer(30000);
        _saveTimer.Elapsed += OnSaveTimerElapsed;
        _saveTimer.AutoReset = true;
        _saveTimer.Start();

        DebugLog.Log("UsageTracker initialized");
    }

    /// <summary>
    /// 定时保存计时器的回调方法。当数据有变更时触发保存操作。
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">计时器事件参数</param>
    private void OnSaveTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_hasChanges)
        {
            SaveDataIfNeeded();
        }
    }

    /// <summary>
    /// 从本地 JSON 文件加载使用数据。
    /// 如果文件不存在或加载失败，则返回一个新的空数据对象。
    /// </summary>
    /// <returns>加载的使用数据对象，加载失败时返回新的空 UsageData 实例</returns>
    private UsageData LoadData()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var data = JsonSerializer.Deserialize<UsageData>(File.ReadAllText(_dataFilePath));
                if (data != null) return data;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load usage data", ex);
        }
        return new UsageData();
    }

    /// <summary>
    /// 在有变更的情况下将使用数据保存到本地 JSON 文件。
    /// 使用锁机制保证线程安全，保存后重置变更标记。
    /// </summary>
    private void SaveDataIfNeeded()
    {
        lock (_lock)
        {
            if (!_hasChanges) return;

            try
            {
                File.WriteAllText(_dataFilePath, JsonSerializer.Serialize(_usageData, new JsonSerializerOptions { WriteIndented = true }));
                _hasChanges = false;
                DebugLog.Log("Usage data saved");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save usage data", ex);
            }
        }
    }

    /// <summary>
    /// 记录指定项目的一次使用。增加使用计数并更新最后使用时间。
    /// 如果该项目尚无使用记录，则新建一条记录。
    /// </summary>
    /// <param name="itemId">项目的唯一标识符</param>
    public void RecordUsage(string itemId)
    {
        lock (_lock)
        {
            if (!_usageData.Items.TryGetValue(itemId, out var item))
            {
                item = new UsageItem();
                _usageData.Items[itemId] = item;
            }
            item.UsageCount++;
            item.LastUsedTime = DateTime.Now;
            _hasChanges = true;
        }
    }

    /// <summary>
    /// 获取指定项目的累计使用次数。
    /// </summary>
    /// <param name="itemId">项目的唯一标识符</param>
    /// <returns>使用次数。如果项目无使用记录则返回 0</returns>
    public int GetUsageCount(string itemId)
    {
        lock (_lock)
        {
            return _usageData.Items.TryGetValue(itemId, out var item) ? item.UsageCount : 0;
        }
    }

    /// <summary>
    /// 获取指定项目的最后使用时间。
    /// </summary>
    /// <param name="itemId">项目的唯一标识符</param>
    /// <returns>最后使用时间。如果项目无使用记录则返回 DateTime.MinValue</returns>
    public DateTime GetLastUsedTime(string itemId)
    {
        lock (_lock)
        {
            return _usageData.Items.TryGetValue(itemId, out var item) ? item.LastUsedTime : DateTime.MinValue;
        }
    }

    /// <summary>
    /// 基于使用频率和最近使用时间计算项目的综合相关性评分。
    /// 评分由三部分组成：
    /// 1. 基础分（baseScore）：由调用方提供的初始分数
    /// 2. 使用频率加成：使用 log10(使用次数+1) * 0.5 计算，使用越多加成越高（但增长递减）
    /// 3. 最近使用加成：1天内 +1.0 分，7天内 +0.5 分，30天内 +0.2 分
    /// </summary>
    /// <param name="itemId">项目的唯一标识符</param>
    /// <param name="baseScore">基础评分</param>
    /// <returns>综合评分（基础分 + 使用频率加成 + 最近使用加成）</returns>
    public double CalculateUsageScore(string itemId, double baseScore)
    {
        lock (_lock)
        {
            if (!_usageData.Items.TryGetValue(itemId, out var item)) return baseScore;

            double usageBoost = Math.Log10(item.UsageCount + 1) * 0.5;
            double recentBonus = 0;

            if (item.LastUsedTime != DateTime.MinValue)
            {
                var days = (DateTime.Now - item.LastUsedTime).TotalDays;
                if (days < 1) recentBonus = 1.0;
                else if (days < 7) recentBonus = 0.5;
                else if (days < 30) recentBonus = 0.2;
            }

            return baseScore + usageBoost + recentBonus;
        }
    }

    /// <summary>
    /// 获取最近使用过的项目 ID 列表，按最后使用时间倒序排列。
    /// </summary>
    /// <param name="maxCount">最多返回的条目数</param>
    /// <returns>最近使用项目 ID 的列表</returns>
    public List<string> GetRecentItemIds(int maxCount)
    {
        lock (_lock)
        {
            return _usageData.Items
                .Where(kv => kv.Value.LastUsedTime != DateTime.MinValue)
                .OrderByDescending(kv => kv.Value.LastUsedTime)
                .Take(maxCount)
                .Select(kv => kv.Key)
                .ToList();
        }
    }

    /// <summary>
    /// 释放使用跟踪器占用的资源。
    /// 停止并释放定时保存计时器，并执行最终一次数据保存以防止数据丢失。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _saveTimer.Stop();
        _saveTimer.Dispose();
        SaveDataIfNeeded(); // 释放前执行最终保存，确保数据不丢失
        _disposed = true;
    }
}
