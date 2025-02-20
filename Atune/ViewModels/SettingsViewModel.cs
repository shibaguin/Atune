using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Atune.Models;
using Atune.Services;
using Atune.Views;
using ThemeVariant = Atune.Models.ThemeVariant;
using System.Collections.Generic;

namespace Atune.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private int selectedThemeIndex;
    
    // Default value - displayed language name
    [ObservableProperty]
    private string selectedLanguage = "Русская";

    // List for selection: displayed language names
    public List<string> AvailableLanguages { get; } = new() { "Русская", "English" };

    private readonly ISettingsService _settingsService;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        // Loading saved settings
        var settings = _settingsService.LoadSettings();
        SelectedThemeIndex = (int)settings.ThemeVariant;

        // Преобразуем сохранённый языковой код в отображаемое название
        SelectedLanguage = Atune.Utils.LanguageConverter.CodeToDisplay(settings.Language);
    }

    [RelayCommand]
    private void SaveSettings()
    {
        // Преобразуем выбранное отображаемое название в код языка для сохранения
        string languageCode = Atune.Utils.LanguageConverter.DisplayToCode(SelectedLanguage);

        _settingsService.SaveSettings(new AppSettings
        { 
            ThemeVariant = (ThemeVariant)SelectedThemeIndex,
            Language = languageCode
        });
    }
} 