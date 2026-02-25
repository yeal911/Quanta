namespace Quanta.WinFormsLite;

public enum SearchKind
{
    Command,
    File
}

public sealed class SearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public SearchKind Kind { get; set; }
    public double Score { get; set; }

    public override string ToString()
        => string.IsNullOrWhiteSpace(Subtitle) ? Title : $"{Title} — {Subtitle}";
}
