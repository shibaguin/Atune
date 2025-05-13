using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Platform;
using Serilog;
using Atune.ViewModels;

namespace Atune.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(WindowSettings))]
internal partial class WindowSettingsJsonContext : JsonSerializerContext
{
}

public class WindowSettings
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string CurrentPage { get; set; } = "Home";
    public bool IsMaximized { get; set; }
}

public class WindowSettingsService
{
    private readonly string _settingsPath;
    private WindowSettings _currentSettings;
    private readonly System.Timers.Timer _saveTimer;
    private bool _isDirty;

    public WindowSettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Atune"
        );
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "window_settings.json");
        _currentSettings = LoadSettings();

        // Инициализируем таймер для отложенного сохранения
        _saveTimer = new System.Timers.Timer(1000) // 1 секунда задержки
        {
            AutoReset = false
        };
        _saveTimer.Elapsed += async (s, e) =>
        {
            if (_isDirty)
            {
                await SaveSettingsInternalAsync(_currentSettings);
                _isDirty = false;
            }
        };
    }

    public WindowSettings GetCurrentSettings()
    {
        return _currentSettings;
    }

    private WindowSettings LoadSettings()
    {
        try
        {
            Log.Information("Attempting to load window settings from {Path}", _settingsPath);
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                Log.Information("Read JSON from file: {Json}", json);
                var settings = JsonSerializer.Deserialize<WindowSettings>(json, WindowSettingsJsonContext.Default.WindowSettings);
                if (settings != null)
                {
                    // Проверяем валидность настроек
                    if (IsValidSettings(settings))
                    {
                        Log.Information("Window settings loaded successfully: {@Settings}", settings);
                        return settings;
                    }
                    else
                    {
                        Log.Warning("Invalid window settings detected, using defaults");
                    }
                }
                else
                {
                    Log.Warning("Deserialized settings are null");
                }
            }
            else
            {
                Log.Warning("Settings file does not exist at {Path}", _settingsPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading window settings");
        }

        Log.Information("Using default window settings");
        return new WindowSettings
        {
            X = 100,
            Y = 100,
            Width = 1280,
            Height = 720,
            IsMaximized = false,
            CurrentPage = "Home"
        };
    }

    private bool IsValidSettings(WindowSettings settings)
    {
        if (settings == null) return false;

        // Проверяем, что CurrentPage соответствует одному из допустимых значений
        if (!Enum.TryParse<MainViewModel.SectionType>(settings.CurrentPage, out _))
        {
            Log.Warning("Invalid CurrentPage value: {Page}", settings.CurrentPage);
            return false;
        }

        // Проверяем, что размеры окна в разумных пределах
        if (settings.Width < 800 || settings.Width > 3840 || settings.Height < 600 || settings.Height > 2160)
        {
            Log.Warning("Invalid window dimensions: {Width}x{Height}", settings.Width, settings.Height);
            return false;
        }

        return true;
    }

    public async Task SaveSettingsAsync(WindowSettings settings)
    {
        _currentSettings = settings;
        _isDirty = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public async Task ForceSaveSettingsAsync()
    {
        if (_isDirty)
        {
            await SaveSettingsInternalAsync(_currentSettings);
            _isDirty = false;
        }
    }

    private async Task SaveSettingsInternalAsync(WindowSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, WindowSettingsJsonContext.Default.WindowSettings);
            await File.WriteAllTextAsync(_settingsPath, json);
            Log.Information("Window settings saved successfully: {@Settings}", settings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving window settings");
        }
    }
}