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
        var uri = new Uri($"avares://Atune/Resources/Localization/{languageCode}.axaml");
        var rd = AvaloniaXamlLoader.Load(uri) as ResourceDictionary;
        if (rd == null)
        {
            throw new FileNotFoundException($"Localization file not found: {uri}");
        }
        
        // Дополнительная проверка на null для Application.Current и его ресурсов
        if (Application.Current?.Resources?.MergedDictionaries == null)
        {
            throw new NullReferenceException("Application.Current или его Resources/MergedDictionaries равны null.");
        }
        
        // Очищаем предыдущие ресурсные словари локализации
        Application.Current!.Resources.MergedDictionaries.Clear();
        // Добавляем новый ресурсный словарь
        Application.Current!.Resources.MergedDictionaries.Add(rd);
    }

    public void Refresh() => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
} 