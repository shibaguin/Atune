using System;
using System.ComponentModel;
using System.Windows;
using Atune.Services;
using Avalonia.Controls;
using Avalonia;
using System.IO;
using Avalonia.Markup.Xaml;
using System.Resources;

public class LocalizationService : INotifyPropertyChanged
{
    private ResourceDictionary _currentResources = new ResourceDictionary();
    private readonly ISettingsService _settingsService;
    private readonly ILoggerService _logger;

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
        LoadLanguage(languageCode);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
    }

    private void LoadLanguage(string languageCode)
    {
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

        // Проверка, что Application.Current.Resources.MergedDictionaries не равен null
        // Check that Application.Current.Resources.MergedDictionaries is not null
        if (Application.Current?.Resources?.MergedDictionaries == null)
        {
            _logger.LogError("Application.Current or its Resources/MergedDictionaries are null.");
            throw new NullReferenceException("Application.Current or its Resources/MergedDictionaries are null.");
        }

        // Очистка предыдущих словарей и добавление новых:
        // Clear previous dictionaries and add new ones:
        Application.Current.Resources.MergedDictionaries.Clear();
        // Добавляем резервный словарь первым (резервные ресурсы доступны, если основной не содержит нужного ключа)
        // Add the fallback dictionary first (fallback resources are available if the main dictionary does not contain the desired key)
        Application.Current.Resources.MergedDictionaries.Add(fallbackRd);
        // Затем основной словарь, значения из которого имеют приоритет
        // Then add the main dictionary, the values of which have priority
        Application.Current.Resources.MergedDictionaries.Add(primaryRd);

        // Сохранение основного словаря локализации для доступа через индексатор (this[string key])
        // Save the main localization dictionary for access through the indexer (this[string key])
        _currentResources = primaryRd;
    }

    // Новый вспомогательный метод для загрузки .resx файла и преобразования его в ResourceDictionary
    // New helper method for loading a .resx file and converting it to a ResourceDictionary
    private ResourceDictionary? LoadResxAsResourceDictionary(string languageCode)
    {
        var resourceDictionary = new ResourceDictionary();
        try
        {
            // Формируем имя базового ресурса, например "Atune.Resources.Localization.en" или "Atune.Resources.Localization.ru"
            // Form the base resource name, for example "Atune.Resources.Localization.en" or "Atune.Resources.Localization.ru"
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
            return resourceDictionary;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading resource for language: {languageCode}. Exception: {ex.Message}", ex);
            return null;
        }
    }

    public void Refresh() => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
} 