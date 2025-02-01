using System;
using System.IO;
using Atune.Models;

namespace Atune.Services;

public class SettingsService : ISettingsService
{
    private static readonly string _settingsPath = 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "Atune", "settings.ini");
    
    private static AppSettings? _cachedSettings;

    public void SaveSettings(AppSettings settings)
    {
        // Переносим реализацию из SettingsManager
    }

    public AppSettings LoadSettings()
    {
        return _cachedSettings ??= LoadSettingsInternal();
    }
    
    private static AppSettings LoadSettingsInternal()
    {
        var settings = new AppSettings();
        
        if (OperatingSystem.IsAndroid() && !File.Exists(_settingsPath))
        {
            settings.ThemeVariant = ThemeVariant.System;
            return settings;
        }

        if (!File.Exists(_settingsPath))
            return settings;

        foreach (var line in File.ReadAllLines(_settingsPath))
        {
            var parts = line.Split('=');
            if (parts.Length != 2) continue;

            switch (parts[0])
            {
                case "ThemeVariant" when int.TryParse(parts[1], out var theme):
                    settings.ThemeVariant = theme >= 0 && theme <= 2 
                        ? (ThemeVariant)theme 
                        : ThemeVariant.System;
                    break;
            }
        }
        
        return settings;
    }
} 