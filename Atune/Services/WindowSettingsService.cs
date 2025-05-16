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
    private const double MIN_WINDOW_WIDTH = 400;
    private const double MIN_WINDOW_HEIGHT = 300;
    private const double MAX_WINDOW_WIDTH = 7680;
    private const double MAX_WINDOW_HEIGHT = 4320;

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
        Log.Information("Getting current window settings: {@Settings}", _currentSettings);
        return _currentSettings;
    }

    public (double Width, double Height) ValidateWindowSize(double width, double height)
    {
        var validatedWidth = Math.Max(MIN_WINDOW_WIDTH, Math.Min(width, MAX_WINDOW_WIDTH));
        var validatedHeight = Math.Max(MIN_WINDOW_HEIGHT, Math.Min(height, MAX_WINDOW_HEIGHT));

        if (validatedWidth != width || validatedHeight != height)
        {
            Log.Information("Window size adjusted from {Width}x{Height} to {NewWidth}x{NewHeight}",
                width, height, validatedWidth, validatedHeight);
        }

        return (validatedWidth, validatedHeight);
    }

    public (double X, double Y) ValidateWindowPosition(double x, double y, double width, double height)
    {
        // Получаем размеры рабочей области
        var screenBounds = GetScreenBounds();

        // Проверяем, чтобы окно не выходило за пределы экрана
        var maxX = screenBounds.Width - width;
        var maxY = screenBounds.Height - height;

        var validatedX = Math.Max(0, Math.Min(x, maxX));
        var validatedY = Math.Max(0, Math.Min(y, maxY));

        if (validatedX != x || validatedY != y)
        {
            Log.Information("Window position adjusted from ({X}, {Y}) to ({NewX}, {NewY})",
                x, y, validatedX, validatedY);
        }

        return (validatedX, validatedY);
    }

    private (double Width, double Height) GetScreenBounds()
    {
        // В реальном приложении здесь нужно получить размеры экрана
        // Для примера используем максимальные размеры
        return (MAX_WINDOW_WIDTH, MAX_WINDOW_HEIGHT);
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

                try
                {
                    var settings = JsonSerializer.Deserialize<WindowSettings>(json, WindowSettingsJsonContext.Default.WindowSettings);
                    if (settings != null)
                    {
                        // Проверяем валидность настроек
                        if (IsValidSettings(settings))
                        {
                            // Валидируем размеры и позицию
                            var (validatedWidth, validatedHeight) = ValidateWindowSize(settings.Width, settings.Height);
                            var (validatedX, validatedY) = ValidateWindowPosition(settings.X, settings.Y, validatedWidth, validatedHeight);

                            settings.Width = validatedWidth;
                            settings.Height = validatedHeight;
                            settings.X = validatedX;
                            settings.Y = validatedY;

                            Log.Information("Window settings loaded and validated: {@Settings}", settings);
                            return settings;
                        }
                        else
                        {
                            Log.Warning("Invalid window settings detected in file, using defaults");
                        }
                    }
                    else
                    {
                        Log.Warning("Deserialized settings are null from file");
                    }
                }
                catch (JsonException ex)
                {
                    Log.Error(ex, "Failed to deserialize settings from JSON file: {Json}", json);
                }
            }
            else
            {
                Log.Warning("Settings file does not exist at {Path}", _settingsPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading window settings from {Path}", _settingsPath);
        }

        // Создаем дефолтные настройки только если файл не существует или содержит невалидные данные
        Log.Information("Creating default window settings");
        var defaultSettings = new WindowSettings
        {
            X = 100,
            Y = 100,
            Width = 1280,
            Height = 720,
            IsMaximized = false,
            CurrentPage = "Home"
        };
        Log.Information("Created default settings: {@Settings}", defaultSettings);

        // Сохраняем дефолтные настройки только если файл не существует
        if (!File.Exists(_settingsPath))
        {
            Log.Information("Saving default settings to file");
            SaveSettingsInternalAsync(defaultSettings).Wait();
        }

        return defaultSettings;
    }

    private bool IsValidSettings(WindowSettings settings)
    {
        if (settings == null)
        {
            Log.Warning("Settings object is null");
            return false;
        }

        // Проверяем, что CurrentPage соответствует одному из допустимых значений
        if (!Enum.TryParse<MainViewModel.SectionType>(settings.CurrentPage, out _))
        {
            Log.Warning("Invalid CurrentPage value: {Page}", settings.CurrentPage);
            return false;
        }

        Log.Information("Settings validation passed: {@Settings}", settings);
        return true;
    }

    public async Task SaveSettingsAsync(WindowSettings settings)
    {
        Log.Information("Saving window settings: {@Settings}", settings);

        // Валидируем размеры и позицию перед сохранением
        if (!settings.IsMaximized)
        {
            var (validatedWidth, validatedHeight) = ValidateWindowSize(settings.Width, settings.Height);
            var (validatedX, validatedY) = ValidateWindowPosition(settings.X, settings.Y, validatedWidth, validatedHeight);

            settings.Width = validatedWidth;
            settings.Height = validatedHeight;
            settings.X = validatedX;
            settings.Y = validatedY;
        }

        _currentSettings = settings;
        _isDirty = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public async Task ForceSaveSettingsAsync()
    {
        Log.Information("Force saving window settings: {@Settings}", _currentSettings);
        _saveTimer.Stop();
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
            Log.Information("Starting to save settings to file: {Path}", _settingsPath);
            var json = JsonSerializer.Serialize(settings, WindowSettingsJsonContext.Default.WindowSettings);
            Log.Information("Serialized settings: {Json}", json);
            await File.WriteAllTextAsync(_settingsPath, json);
            Log.Information("Settings file written successfully");

            // Проверяем, что файл действительно содержит правильные данные
            var savedContent = await File.ReadAllTextAsync(_settingsPath);
            Log.Information("Verification - content of saved file: {Content}", savedContent);

            Log.Information("Window settings saved successfully: {@Settings}", settings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving window settings to {Path}", _settingsPath);
        }
    }
}