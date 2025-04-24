namespace Atune.Services;

public class MediaMetadata
{
    public string Title { get; set; } = null!;
    public string Artist { get; set; } = null!;
    public string? Album { get; set; }
    public string? Genre { get; set; }
    public string? Year { get; set; }
}
