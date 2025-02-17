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
    private readonly IPlatformPathService _platformPathService;
    // The settings file path is now stored in an instance
    private readonly string _settingsPath;

    private readonly ILoggerService _logger;

    public SettingsService(IMemoryCache cache, IPlatformPathService platformPathService, ILoggerService logger)
    {
        _cache = cache;
        _platformPathService = platformPathService;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSize(1024) // Size of the record in bytes (~1KB for settings)
            .SetPriority(CacheItemPriority.High)
            .SetSlidingExpiration(TimeSpan.FromMinutes(30));

        // Initialize the path to the settings file through our service
        _settingsPath = _platformPathService.GetSettingsPath();

        _logger = logger;
    }

    private static long GetPlatformCacheLimit()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            return 50 * 1024 * 1024; // 50 MB for mobile devices
        }
        else
        {
            return 100 * 1024 * 1024; // 100 MB for desktop
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            lock (_fileLockAsync)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSize(1024)
                    .SetPriority(CacheItemPriority.High)
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));
                
                _cache.Set("AppSettings", settings, cacheOptions);
                
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    try
                    {
                        // Directory.CreateDirectory will create the directory if it doesn't exist,
                        // and will not throw an exception if the directory already exists.
                        Directory.CreateDirectory(directory);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Debug.WriteLine("Not enough permissions to create directory: " + directory);
                        throw new InvalidOperationException("Not enough permissions to access directory " + directory, ex);
                    }
                    catch (IOException ex)
                    {
                        // If the error occurred due to a race condition, check the directory again.
                        if (!Directory.Exists(directory))
                        {
                            Debug.WriteLine("Error creating directory: " + directory);
                            throw new InvalidOperationException("Failed to create directory " + directory, ex);
                        }
                    }
                }

                var lines = new List<string>
                {
                    $"ThemeVariant={(int)settings.ThemeVariant}",
                    $"Language={settings.Language}"
                };

                // For Android, we use a special storage
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
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error saving settings", ex);
            _cache.Remove("AppSettings");
            throw new SettingsException("Failed to save settings", ex);
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
                        case "Language":
                            settings.Language = parts[1];
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
                Debug.WriteLine($"Error loading settings: {ex.Message}");
                return new AppSettings();
            }
        }
    }

    // Add custom exception
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
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSize(1024)
                .SetPriority(CacheItemPriority.High)
                .SetSlidingExpiration(TimeSpan.FromMinutes(30));
            
            await Task.Run(() => _cache.Set("AppSettings", settings, cacheOptions));
            
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                await Task.Run(() => Directory.CreateDirectory(directory));
            }
            
            await File.WriteAllTextAsync(_settingsPath, JsonSerializer.Serialize(settings));
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