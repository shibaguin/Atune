using System;
using System.IO;
using Atune.Models;

namespace Atune.Services;

public static class SettingsManager
{
    private static readonly string SettingsPath = 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "Atune", "settings.ini");

    public static void SaveSettings(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }
        
        var lines = new[]
        {
            $"ThemeVariant={(int)settings.ThemeVariant}"
        };
        
        File.WriteAllLines(SettingsPath, lines);
    }

    public static AppSettings LoadSettings()
    {
        var settings = new AppSettings();
        
        if (!File.Exists(SettingsPath))
            return settings;

        foreach (var line in File.ReadAllLines(SettingsPath))
        {
            var parts = line.Split('=');
            if (parts.Length != 2) continue;

            switch (parts[0])
            {
                case "ThemeVariant" when int.TryParse(parts[1], out var theme):
                    settings.ThemeVariant = (ThemeVariant)theme;
                    break;
            }
        }
        
        return settings;
    }
} 