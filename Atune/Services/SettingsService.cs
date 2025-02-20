using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Atune.Models;
using Avalonia;
using Avalonia.Platform;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using Atune.Services;

namespace Atune.Services;

public class SettingsService : ISettingsService
{
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private static readonly SemaphoreSlim _fileLockAsync = new SemaphoreSlim(1, 1);

    // Add dependency injection for platform-specific paths
    // Добавьте зависимость для платформенно-специфичных путей
    private readonly IPlatformPathService _platformPathService;
    // The settings file path is now stored in an instance
    private readonly string _settingsPath;

    private readonly ILoggerService _logger;

    public SettingsService(IMemoryCache cache, IPlatformPathService platformPathService, ILoggerService logger)
    {
        _cache = cache;
        _platformPathService = platformPathService;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSize(1024) // Size of the record in bytes (~1KB for settings) / Размер записи в байтах (~1KB для настроек)
            .SetPriority(CacheItemPriority.High)
            .SetSlidingExpiration(TimeSpan.FromMinutes(30));

        // Initialize the path to the settings file through our service
        // Инициализируйте путь к файлу настроек через наш сервис
        _settingsPath = _platformPathService.GetSettingsPath();

        _logger = logger;
    }

    private static long GetPlatformCacheLimit()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            return 50 * 1024 * 1024; // 50 MB for mobile devices / 50 MB для мобильных устройств
        }
        else
        {
            return 100 * 1024 * 1024; // 100 MB for desktop / 100 MB для настольных устройств   
        }
    }

    // Добавляем новые методы для разбора INI‑файла на секции
    private Dictionary<string, List<string>> ParseIniFile()
    {
        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        List<string>? currentSectionLines = null;
        string currentSectionName = string.Empty;
        if (File.Exists(_settingsPath))
        {
            foreach (var line in File.ReadAllLines(_settingsPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    // Начало новой секции
                    currentSectionName = trimmed.TrimStart('[').TrimEnd(']');
                    if (!sections.ContainsKey(currentSectionName))
                    {
                        sections[currentSectionName] = new List<string>();
                    }
                    currentSectionLines = sections[currentSectionName];
                }
                else
                {
                    // Добавляем строку, только если она не пустая
                    if (currentSectionLines != null && !string.IsNullOrWhiteSpace(line))
                    {
                        currentSectionLines.Add(line);
                    }
                }
            }
        }
        return sections;
    }

    private void WriteIniFile(Dictionary<string, List<string>> sections)
    {
        var lines = new List<string>();
        foreach (var kvp in sections)
        {
            lines.Add($"[{kvp.Key}]");
            lines.AddRange(kvp.Value);
            lines.Add(string.Empty); // пустая строка для отделения секций
        }
        File.WriteAllLines(_settingsPath, lines);
    }

    // Изменяем метод сохранения основных настроек, чтобы обновлять только секцию [AppSettings]
    public void SaveSettings(AppSettings settings)
    {
        lock (_fileLockAsync)
        {
            // Готовим содержимое секции для основных настроек
            var appSection = new List<string>
            {
                $"ThemeVariant={(int)settings.ThemeVariant}",
                $"Language={settings.Language}"
            };

            // Разбираем текущее содержимое файла на секции (если файл существует)
            var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(_settingsPath))
            {
                sections = ParseIniFile();
            }

            // Обновляем (или добавляем) секцию AppSettings
            sections["AppSettings"] = appSection;

            // Записываем обновлённый INI‑файл
            WriteIniFile(sections);
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
        lock (_fileLockAsync)
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    _logger.LogWarning($"Settings file not found at {_settingsPath}. Using default settings.");
                    return new AppSettings();
                }

                var sections = ParseIniFile();
                if (!sections.ContainsKey("AppSettings"))
                {
                    _logger.LogWarning("Section [AppSettings] not found in settings file. Using default settings.");
                    return new AppSettings();
                }

                var appSection = sections["AppSettings"];
                var settings = new AppSettings();
                foreach (var line in appSection)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length != 2)
                    {
                        _logger.LogWarning($"Ignoring malformed line in settings file: '{line}'");
                        continue;
                    }

                    switch (parts[0])
                    {
                        case "ThemeVariant":
                            if (!int.TryParse(parts[1], out var theme))
                            {
                                _logger.LogWarning($"Invalid value for ThemeVariant: '{parts[1]}'. Using default.");
                                settings.ThemeVariant = ThemeVariant.System;
                            }
                            else
                            {
                                settings.ThemeVariant = theme switch
                                {
                                    0 => ThemeVariant.System,
                                    1 => ThemeVariant.Light,
                                    2 => ThemeVariant.Dark,
                                    _ => ThemeVariant.System
                                };
                            }
                            break;
                        case "Language":
                            settings.Language = string.IsNullOrWhiteSpace(parts[1]) ? "en" : parts[1];
                            break;
                        case "LastUsedProfile":
                            settings.LastUsedProfile = parts[1];
                            break;
                        case "LastUpdated":
                            if (!DateTimeOffset.TryParse(parts[1], out var date))
                            {
                                _logger.LogWarning($"Invalid value for LastUpdated: '{parts[1]}'. Using current date.");
                                settings.LastUpdated = DateTimeOffset.Now;
                            }
                            else
                            {
                                settings.LastUpdated = date;
                            }
                            break;
                        default:
                            _logger.LogWarning($"Unknown key '{parts[0]}' encountered in settings file.");
                            break;
                    }
                }

                _cache.Set("AppSettings", settings, _cacheOptions);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading settings from INI file.", ex);
                return new AppSettings();
            }
        }
    }

    // Add custom exception
    // Добавьте пользовательское исключение
    public class SettingsException : Exception
    {
        public SettingsException(string message, Exception inner) 
            : base(message, inner) {}
    }

    public string GetCacheStatistics()
    {
        if (_cache is MemoryCache memoryCache)
        {
            return $"Settings cache: {memoryCache.Count} records";
        }
        return "Cache statistics are not available";
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

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await _fileLockAsync.WaitAsync();
        try
        {
            // Обновляем кэш
            _cache.Set("AppSettings", settings, _cacheOptions);

            // Разбираем существующий INI-файл, если он существует
            var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(_settingsPath))
            {
                sections = ParseIniFile();
            }

            // Готовим секцию для основных настроек (локализации, темы и т.п.)
            var appSection = new List<string>
            {
                $"ThemeVariant={(int)settings.ThemeVariant}",
                $"Language={settings.Language}",
                $"LastUsedProfile={settings.LastUsedProfile}",
                $"LastUpdated={settings.LastUpdated}"
            };

            // Обновляем (или добавляем) секцию AppSettings
            sections["AppSettings"] = appSection;

            // Записываем обновлённый INI‑файл (запись запускаем в отдельном таске, чтобы не блокировать поток)
            await Task.Run(() => WriteIniFile(sections));
        }
        finally
        {
            _fileLockAsync.Release();
        }
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        await _fileLockAsync.WaitAsync();
        try
        {
            if (_cache.TryGetValue("AppSettings", out AppSettings? cachedSettings))
            {
                return cachedSettings!;
            }

            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            var json = await File.ReadAllTextAsync(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSize(1024)
                .SetPriority(CacheItemPriority.High)
                .SetSlidingExpiration(TimeSpan.FromMinutes(30));
            
            _cache.Set("AppSettings", settings, cacheOptions);
            return settings;
        }
        finally
        {
            _fileLockAsync.Release();
        }
    }
} 