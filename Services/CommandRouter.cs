using System.Diagnostics;
using System.Text.RegularExpressions;
using Quanta.Models;

namespace Quanta.Services;

public class CommandRouter
{
    private readonly UsageTracker _usageTracker;
    private static readonly Regex PowerShellRegex = new(@"^>\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CalcRegex = new(@"^calc\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex GoogleSearchRegex = new(@"^g\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CommandRouter(UsageTracker usageTracker) => _usageTracker = usageTracker;

    public async Task<SearchResult?> TryHandleCommandAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var psMatch = PowerShellRegex.Match(input);
        if (psMatch.Success) return await ExecutePowerShellAsync(psMatch.Groups[1].Value);
        var calcMatch = CalcRegex.Match(input);
        if (calcMatch.Success) return Calculate(calcMatch.Groups[1].Value);
        var gMatch = GoogleSearchRegex.Match(input);
        if (gMatch.Success) return await SearchInBrowserAsync(gMatch.Groups[1].Value);
        return null;
    }

    private async Task<SearchResult> ExecutePowerShellAsync(string command)
    {
        var result = new SearchResult { Title = $"PowerShell: {command}", Type = SearchResultType.Command, Path = command };
        try
        {
            var psi = new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using var process = Process.Start(psi);
            if (process != null)
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                result.Data = new CommandResult { Success = process.ExitCode == 0, Output = output, Error = error };
                _usageTracker.RecordUsage($"cmd:{command}");
            }
        }
        catch (Exception ex) { result.Data = new CommandResult { Success = false, Error = ex.Message }; }
        return result;
    }

    private SearchResult Calculate(string expression)
    {
        var result = new SearchResult { Title = $"= {expression}", Type = SearchResultType.Calculator, Path = expression };
        try
        {
            string sanitized = Regex.Replace(expression, @"[^0-9+\-*/().%^]", "");
            var computed = new System.Data.DataTable().Compute(sanitized, null);
            result.Subtitle = computed.ToString() ?? "Error";
            result.Data = new CommandResult { Success = true, Output = computed.ToString() ?? "" };
            _usageTracker.RecordUsage($"calc:{expression}");
        }
        catch (Exception ex) { result.Subtitle = $"Error: {ex.Message}"; result.Data = new CommandResult { Success = false, Error = ex.Message }; }
        return result;
    }

    private async Task<SearchResult> SearchInBrowserAsync(string keyword)
    {
        var result = new SearchResult { Title = $"Search: {keyword}", Subtitle = "Open in browser", Type = SearchResultType.WebSearch, Path = keyword };
        try
        {
            Process.Start(new ProcessStartInfo { FileName = $"https://www.google.com/search?q={Uri.EscapeDataString(keyword)}", UseShellExecute = true });
            result.Data = new CommandResult { Success = true };
            _usageTracker.RecordUsage($"search:{keyword}");
        }
        catch (Exception ex) { result.Data = new CommandResult { Success = false, Error = ex.Message }; }
        return result;
    }

    public static List<SearchResult> GetCommandSuggestions() => new()
    {
        new() { Title = "> command", Subtitle = "Execute PowerShell command", Type = SearchResultType.Command },
        new() { Title = "calc expression", Subtitle = "Calculate expression", Type = SearchResultType.Calculator },
        new() { Title = "g keyword", Subtitle = "Search in browser", Type = SearchResultType.WebSearch }
    };
}
