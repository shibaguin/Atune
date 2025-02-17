using System;
using Atune.Models;

namespace Atune.Models;

public class AppSettings
{
    public ThemeVariant ThemeVariant { get; set; } = ThemeVariant.System;
    public string? LastUsedProfile { get; set; }
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    public string Language { get; set; } = "ru";
} 