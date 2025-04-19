using System;
using Atune.Models;

namespace Atune.Models;

public class AppSettings
{
    public ThemeVariant ThemeVariant { get; set; } = ThemeVariant.System;
    public string? LastUsedProfile { get; set; }
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    public string Language { get; set; } = "en";
    public int Volume { get; set; } = 50;
    // Sort order settings per tab
    public string SortOrderTracks { get; set; } = "A-Z";
    public string SortOrderAlbums { get; set; } = "A-Z";
    public string SortOrderPlaylists { get; set; } = "A-Z";
    public string SortOrderArtists { get; set; } = "A-Z";
} 