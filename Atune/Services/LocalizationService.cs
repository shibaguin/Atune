using System;
using System.ComponentModel;
using System.Windows;
using Atune.Services;
using Avalonia.Controls;
using Avalonia;
using System.IO;
using Avalonia.Markup.Xaml;
using System.Resources;
using System.Collections.Generic;

public class LocalizationService : INotifyPropertyChanged
{
    private ResourceDictionary _currentResources = new ResourceDictionary();
    private readonly ISettingsService _settingsService;
    private readonly ILoggerService _logger;
    private readonly Dictionary<string, ResourceDictionary> _resourceCache = new Dictionary<string, ResourceDictionary>(StringComparer.OrdinalIgnoreCase);

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationService(ISettingsService settingsService, ILoggerService logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        LoadLanguage(_settingsService.LoadSettings().Language);
    }

    public string this[string key] => 
        _currentResources[key]?.ToString() ?? $"#{key}#";

    public void SetLanguage(string languageCode)
    {
        // Добавляем принудительное обновление настроек
        var settings = _settingsService.LoadSettings();
        if (settings.Language != languageCode)
        {
            settings.Language = languageCode;
            _settingsService.SaveSettings(settings);
        }
        
        LoadLanguage(languageCode);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
        
        // Заменяем проблемную строку
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = app.ActualThemeVariant;
        }
    }

    private void LoadLanguage(string languageCode)
    {
        // Если передан не код языка, а отображаемое название (например, "Русский" или "English"),
        // то преобразуем его в корректный код ("ru" или "en").
        if (languageCode != "ru" && languageCode != "en")
        {
            var originalLanguage = languageCode;
            languageCode = Atune.Utils.LanguageConverter.DisplayToCode(languageCode);
            _logger.LogWarning($"Converted display name '{originalLanguage}' to code '{languageCode}'.");
        }

        // Загрузка основного словаря ресурсов из .resx файла вместо .axaml
        // Loading the main resource dictionary from the .resx file instead of the .axaml file
        var primaryRd = LoadResxAsResourceDictionary(languageCode);
        if (primaryRd == null)
        {
            _logger.LogError($"Resx localization file not found for language: {languageCode}");
            throw new FileNotFoundException($"Resx localization file not found for language: {languageCode}");
        }

        // Определение резервного языка: если выбран "en", резервный "ru" и наоборот
        // Determining the fallback language: if "en" is selected, the fallback is "ru" and vice versa
        string? fallbackLanguage = null;
        if (languageCode.Equals("en", StringComparison.OrdinalIgnoreCase))
            fallbackLanguage = "ru";
        else if (languageCode.Equals("ru", StringComparison.OrdinalIgnoreCase))
            fallbackLanguage = "en";

        ResourceDictionary fallbackRd;
        if (fallbackLanguage != null)
        {
            fallbackRd = LoadResxAsResourceDictionary(fallbackLanguage) ?? new ResourceDictionary();
        }
        else
        {
            fallbackRd = new ResourceDictionary();
        }
        
        if (Application.Current?.Resources?.MergedDictionaries == null)
        {
            _logger.LogError("Application.Current or its Resources/MergedDictionaries are null.");
            throw new NullReferenceException("Application.Current or its Resources/MergedDictionaries are null.");
        }
        
        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(fallbackRd);
        Application.Current.Resources.MergedDictionaries.Add(primaryRd);

        _currentResources = primaryRd;
    }

    // Новый вспомогательный метод для загрузки .resx файла и преобразования его в ResourceDictionary
    // New helper method for loading a .resx file and converting it to a ResourceDictionary
    private ResourceDictionary? LoadResxAsResourceDictionary(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
            return null;
        
        if (_resourceCache.TryGetValue(languageCode, out var cachedRd))
            return cachedRd;
        
        var resourceDictionary = new ResourceDictionary();
        try
        {
            var baseName = $"Atune.Resources.Localization.{languageCode}";
            var rm = new ResourceManager(baseName, typeof(LocalizationService).Assembly);
            var resourceSet = rm.GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, true, true);
            if (resourceSet == null)
            {
                return null;
            }
            foreach (System.Collections.DictionaryEntry entry in resourceSet)
            {
                resourceDictionary[entry.Key] = entry.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading resource for language: {languageCode}. Exception: {ex.Message}", ex);
            return null;
        }
        
        _resourceCache[languageCode] = resourceDictionary;
        return resourceDictionary;
    }

    public void Refresh() => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
} 
