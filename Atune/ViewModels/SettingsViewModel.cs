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
    
    // Значение по умолчанию – отображаемое название языка
    [ObservableProperty]
    private string selectedLanguage = "Русская";

    // Список для выбора: отображаемые названия языков
    public List<string> AvailableLanguages { get; } = new() { "Русская", "English" };

    private readonly ISettingsService _settingsService;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        // Загрузка сохранённых настроек
        var settings = _settingsService.LoadSettings();
        SelectedThemeIndex = (int)settings.ThemeVariant;

        // Преобразуем сохранённый код языка в отображаемое название
        SelectedLanguage = settings.Language switch
        {
            "ru" => "Русская",
            "en" => "English",
            _ => settings.Language
        };
    }

    [RelayCommand]
    private void SaveSettings()
    {
        // Преобразуем выбранное отображаемое название в код языка для сохранения
        string languageCode = SelectedLanguage switch
        {
            "Русская" => "ru",
            "English" => "en",
            _ => SelectedLanguage
        };

        _settingsService.SaveSettings(new AppSettings
        { 
            ThemeVariant = (ThemeVariant)SelectedThemeIndex,
            Language = languageCode
        });
    }
} 