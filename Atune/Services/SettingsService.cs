using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Atune.Models;
using Avalonia;
using Avalonia.Platform;
using Microsoft.Extensions.Options;

namespace Atune.Services;

public class SettingsService : ISettingsService
{
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private static readonly string _settingsPath = GetSettingsPath();
    private static readonly object _fileLock = new object();

    public SettingsService(IMemoryCache cache)
    {
        _cache = cache;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSize(1024) // Размер записи в байтах (~1KB на настройки)
            .SetPriority(CacheItemPriority.High)
            .SetSlidingExpiration(TimeSpan.FromMinutes(30));
    }

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

    private static long GetPlatformCacheLimit()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            return 50 * 1024 * 1024; // 50 MB для мобильных устройств
        }
        return 100 * 1024 * 1024; // 100 MB для десктопа
    }

    public void SaveSettings(AppSettings settings)
    {
        lock (_fileLock)
        {
            try
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSize(1024)
                    .SetPriority(CacheItemPriority.High)
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));
                
                _cache.Set("AppSettings", settings, cacheOptions);
                
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
                _cache.Remove("AppSettings");
                Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
                throw new SettingsException("Не удалось сохранить настройки", ex);
            }
        }
    }

    public AppSettings LoadSettings()
    {
        if (_cache.TryGetValue("AppSettings", out AppSettings? cachedSettings) && cachedSettings != null)
        {
            return cachedSettings;
        }
        return LoadSettingsInternal();
    }
    
    private AppSettings LoadSettingsInternal()
    {
        lock (_fileLock)
        {
            try
            {
                if (!File.Exists(_settingsPath))
                    return new AppSettings();

                var settings = new AppSettings();
                
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

                _cache.Set("AppSettings", settings, _cacheOptions);
                return settings;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
                return new AppSettings();
            }
        }
    }

    // Добавим кастомное исключение
    public class SettingsException : Exception
    {
        public SettingsException(string message, Exception inner) 
            : base(message, inner) {}
    }

    public string GetCacheStatistics()
    {
        if (_cache is MemoryCache memoryCache)
        {
            return $"Кэш настроек: {memoryCache.Count} записей";
        }
        return "Статистика кэша недоступна";
    }

    public void BackupDatabase(string backupPath)
    {
        File.Copy(
            Path.Combine(Environment.CurrentDirectory, "media_library.db"),
            backupPath,
            overwrite: true
        );
    }

    public string GetSetting(string key)
    {
        var settings = LoadSettings();
        return settings?.GetType().GetProperty(key)?.GetValue(settings)?.ToString() ?? string.Empty;
    }
} 