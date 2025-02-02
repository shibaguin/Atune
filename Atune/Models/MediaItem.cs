namespace Atune.Models;
using System;

public class MediaItem
{
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
} 