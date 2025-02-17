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
        // Load the main resource dictionary (e.g., "en.axaml" or "ru.axaml")
        var primaryUri = new Uri($"avares://Atune/Resources/Localization/{languageCode}.axaml");
        var primaryRd = AvaloniaXamlLoader.Load(primaryUri) as ResourceDictionary;
        if (primaryRd == null)
        {
            throw new FileNotFoundException($"Main localization file not found: {primaryUri}");
        }

        // Determine fallback language: if "en" is selected, fallback will be "ru" and vice versa
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

        // Check if Application.Current.Resources.MergedDictionaries is null
        if (Application.Current?.Resources?.MergedDictionaries == null)
        {
            throw new NullReferenceException("Application.Current or its Resources/MergedDictionaries are null.");
        }

        // Clear previous localization resource dictionaries
        Application.Current.Resources.MergedDictionaries.Clear();
        // Add the fallback dictionary first. When dynamically searching for resources, the fallback is checked first.
        Application.Current.Resources.MergedDictionaries.Add(fallbackRd);
        // Then add the main dictionary so its values have priority if the key is present.
        Application.Current.Resources.MergedDictionaries.Add(primaryRd);

        // If necessary, you can save the main dictionary locally
        _currentResources = primaryRd;
    }

    public void Refresh() => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
} 