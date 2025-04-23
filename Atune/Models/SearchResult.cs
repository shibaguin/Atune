namespace Atune.Models;

public class SearchResult
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public object? Value { get; set; }
} 