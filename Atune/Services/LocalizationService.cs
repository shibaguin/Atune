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

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
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
        var primaryRd = LoadResxAsResourceDictionary(languageCode);
        if (primaryRd == null)
        {
            throw new FileNotFoundException($"Resx localization file not found for language: {languageCode}");
        }

        // Определение резервного языка: если выбран "en", резервный "ru" и наоборот
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
        if (Application.Current?.Resources?.MergedDictionaries == null)
        {
            throw new NullReferenceException("Application.Current или его Resources/MergedDictionaries равны null.");
        }

        // Очистка предыдущих словарей и добавление новых:
        Application.Current.Resources.MergedDictionaries.Clear();
        // Добавляем резервный словарь первым (резервные ресурсы доступны, если основной не содержит нужного ключа)
        Application.Current.Resources.MergedDictionaries.Add(fallbackRd);
        // Затем основной словарь, значения из которого имеют приоритет
        Application.Current.Resources.MergedDictionaries.Add(primaryRd);

        // Сохранение основного словаря локализации для доступа через индексатор (this[string key])
        _currentResources = primaryRd;
    }

    // Новый вспомогательный метод для загрузки .resx файла и преобразования его в ResourceDictionary
    private ResourceDictionary? LoadResxAsResourceDictionary(string languageCode)
    {
        var resourceDictionary = new ResourceDictionary();
        try
        {
            // Формируем имя базового ресурса, например "Atune.Resources.Localization.en" или "Atune.Resources.Localization.ru"
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
        catch (Exception)
        {
            return null;
        }
    }

    public void Refresh() => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
} 