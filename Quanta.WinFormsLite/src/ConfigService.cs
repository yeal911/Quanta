using System.Text.Json;

namespace Quanta.WinFormsLite;

public static class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "lite.config.json");

    public static AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
        {
            var seed = CreateDefault();
            Save(seed);
            return seed;
        }

        var json = File.ReadAllText(ConfigPath);
        var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
        return cfg ?? CreateDefault();
    }

    public static void Save(AppConfig cfg)
    {
        var json = JsonSerializer.Serialize(cfg, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    private static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            Commands =
            [
                new CommandItem { Keyword = "g", Name = "Google", Type = "Url", Path = "https://www.google.com/search?q={param}" },
                new CommandItem { Keyword = "gh", Name = "GitHub", Type = "Url", Path = "https://github.com/search?q={param}" },
                new CommandItem { Keyword = "yt", Name = "YouTube", Type = "Url", Path = "https://www.youtube.com/results?search_query={param}" }
            ],
            AppSettings = new AppSettings
            {
                MaxResults = 8,
                EnableFileSearch = true,
                FileScanLimit = 300,
                FileSearchRoots =
                [
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                ]
            }
        };
    }
}
