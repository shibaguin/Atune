using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Atune.Models;
using Avalonia;
using Avalonia.Platform;

namespace Atune.Services;

public class SettingsService : ISettingsService
{
    private static readonly string _settingsPath = GetSettingsPath();
    
    private static AppSettings? _cachedSettings;
    private static readonly object _fileLock = new object();

    private static string GetSettingsPath()
    {
        const string fileName = "settings.ini";
        
        if (OperatingSystem.IsAndroid())
        {
            // Для внешнего хранилища
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                fileName);
        }
        
        // Для десктопных ОС
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Atune",
            fileName);
    }

    public void SaveSettings(AppSettings settings)
    {
        lock (_fileLock)
        {
            try
            {
                _cachedSettings = settings;
                
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                var lines = new List<string>
                {
                    $"ThemeVariant={(int)settings.ThemeVariant}"
                };

                // Для Android используем специальное хранилище
                if (OperatingSystem.IsAndroid())
                {
                    using var stream = File.Create(_settingsPath);
                    using var writer = new StreamWriter(stream);
                    lines.ForEach(writer.WriteLine);
                }
                else
                {
                    File.WriteAllLines(_settingsPath, lines);
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки (позже можно добавить ILogger)
                Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
                throw new SettingsException("Не удалось сохранить настройки", ex);
            }
        }
    }

    public AppSettings LoadSettings()
    {
        return _cachedSettings ??= LoadSettingsInternal();
    }
    
    private static AppSettings LoadSettingsInternal()
    {
        var settings = new AppSettings();
        
        if (!File.Exists(_settingsPath))
            return settings;

        try
        {
            foreach (var line in File.ReadAllLines(_settingsPath))
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;

                switch (parts[0])
                {
                    case "ThemeVariant" when int.TryParse(parts[1], out var theme):
                        settings.ThemeVariant = theme switch
                        {
                            0 => ThemeVariant.System,
                            1 => ThemeVariant.Light,
                            2 => ThemeVariant.Dark,
                            _ => ThemeVariant.System
                        };
                        break;
                    case "LastUsedProfile":
                        settings.LastUsedProfile = parts[1];
                        break;
                    case "LastUpdated" when DateTimeOffset.TryParse(parts[1], out var date):
                        settings.LastUpdated = date;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
        }
        
        return settings;
    }

    // Добавим кастомное исключение
    public class SettingsException : Exception
    {
        public SettingsException(string message, Exception inner) 
            : base(message, inner) {}
    }
} 