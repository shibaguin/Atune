using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Serilog;

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

    public WindowSettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Atune"
        );
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "window_settings.json");
        _currentSettings = LoadSettings();
    }

    public WindowSettings GetCurrentSettings()
    {
        return _currentSettings;
    }

    private WindowSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<WindowSettings>(json, WindowSettingsJsonContext.Default.WindowSettings);
                if (settings != null)
                {
                    Log.Information("Window settings loaded successfully: {@Settings}", settings);
                    return settings;
                }
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

    public async Task SaveSettingsAsync(WindowSettings settings)
    {
        try
        {
            _currentSettings = settings;
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