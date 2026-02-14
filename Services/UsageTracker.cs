using System.IO;
using System.Text.Json;
using System.Timers;
using Quanta.Models;
using Timer = System.Timers.Timer;

namespace Quanta.Services;

public class UsageTracker : IDisposable
{
    private readonly string _dataFilePath;
    private UsageData _usageData;
    private readonly object _lock = new();
    private readonly Timer _saveTimer;
    private bool _hasChanges;
    private bool _disposed;

    public UsageTracker()
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Quanta");
        Directory.CreateDirectory(appDataPath);
        _dataFilePath = Path.Combine(appDataPath, "usage.json");
        _usageData = LoadData();

        // Save every 30 seconds if there are changes
        _saveTimer = new Timer(30000);
        _saveTimer.Elapsed += OnSaveTimerElapsed;
        _saveTimer.AutoReset = true;
        _saveTimer.Start();
        
        Logger.Log("UsageTracker initialized");
    }

    private void OnSaveTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_hasChanges)
        {
            SaveDataIfNeeded();
        }
    }

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

    private void SaveDataIfNeeded()
    {
        lock (_lock)
        {
            if (!_hasChanges) return;
            
            try 
            { 
                File.WriteAllText(_dataFilePath, JsonSerializer.Serialize(_usageData, new JsonSerializerOptions { WriteIndented = true })); 
                _hasChanges = false;
                Logger.Debug("Usage data saved");
            } 
            catch (Exception ex) 
            {
                Logger.Error("Failed to save usage data", ex);
            }
        }
    }

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

    public int GetUsageCount(string itemId) 
    { 
        lock (_lock) 
        { 
            return _usageData.Items.TryGetValue(itemId, out var item) ? item.UsageCount : 0; 
        } 
    }

    public DateTime GetLastUsedTime(string itemId) 
    { 
        lock (_lock) 
        { 
            return _usageData.Items.TryGetValue(itemId, out var item) ? item.LastUsedTime : DateTime.MinValue; 
        } 
    }

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

    public void Dispose()
    {
        if (_disposed) return;
        
        _saveTimer.Stop();
        _saveTimer.Dispose();
        SaveDataIfNeeded(); // Final save on dispose
        _disposed = true;
    }
}
