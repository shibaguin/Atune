using System;
using System.ComponentModel;
using System.Windows;
using Atune.Services;
using Avalonia.Controls;
using Avalonia;
using System.IO;
using Avalonia.Markup.Xaml;

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
        // Загружаем основной ресурсный словарь (например, "en.axaml" или "ru.axaml")
        var primaryUri = new Uri($"avares://Atune/Resources/Localization/{languageCode}.axaml");
        var primaryRd = AvaloniaXamlLoader.Load(primaryUri) as ResourceDictionary;
        if (primaryRd == null)
        {
            throw new FileNotFoundException($"Основной файл локализации не найден: {primaryUri}");
        }

        // Определяем fallback язык: если выбран «en» – fallback будет «ru» и наоборот
        string? fallbackLanguage = null;
        if (languageCode.Equals("en", StringComparison.OrdinalIgnoreCase))
            fallbackLanguage = "ru";
        else if (languageCode.Equals("ru", StringComparison.OrdinalIgnoreCase))
            fallbackLanguage = "en";

        ResourceDictionary fallbackRd;
        if (fallbackLanguage != null)
        {
            var fallbackUri = new Uri($"avares://Atune/Resources/Localization/{fallbackLanguage}.axaml");
            fallbackRd = AvaloniaXamlLoader.Load(fallbackUri) as ResourceDictionary ?? new ResourceDictionary();
        }
        else
        {
            fallbackRd = new ResourceDictionary();
        }

        // Проверяем наличие Application.Current.Resources.MergedDictionaries
        if (Application.Current?.Resources?.MergedDictionaries == null)
        {
            throw new NullReferenceException("Application.Current или его Resources/MergedDictionaries равны null.");
        }

        // Очищаем предыдущие ресурсные словари локализации
        Application.Current.Resources.MergedDictionaries.Clear();
        // Добавляем fallback словарь первым. При динамическом поиске ресурсов сначала проверяется fallback.
        Application.Current.Resources.MergedDictionaries.Add(fallbackRd);
        // Затем добавляем основной словарь, чтобы его значения имели приоритет, если ключ присутствует.
        Application.Current.Resources.MergedDictionaries.Add(primaryRd);

        // Если необходимо, можно сохранить основной словарь локально
        _currentResources = primaryRd;
    }

    public void Refresh() => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
} 