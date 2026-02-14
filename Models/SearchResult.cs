namespace Quanta.Models;

public enum SearchResultType { Application, File, RecentFile, Window, Command, Calculator, WebSearch, CustomCommand }

public class SearchResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public SearchResultType Type { get; set; }
    public int UsageCount { get; set; }
    public DateTime LastUsedTime { get; set; }
    public double MatchScore { get; set; }
    public object? Data { get; set; }
    public IntPtr WindowHandle { get; set; }
    public CommandConfig? CommandConfig { get; set; }
}

public class CommandResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
