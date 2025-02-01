using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Atune.Models;
using Atune.Services;
using Atune.Views;
using ThemeVariant = Atune.Models.ThemeVariant;

namespace Atune.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _selectedThemeIndex;

    private readonly ISettingsService _settingsService;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        // Загрузка сохранённых настроек
        var settings = _settingsService.LoadSettings();
        SelectedThemeIndex = (int)settings.ThemeVariant;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settingsService.SaveSettings(new AppSettings { 
            ThemeVariant = (ThemeVariant)SelectedThemeIndex 
        });
    }
} 