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

    public SettingsViewModel()
    {
        // Загрузка сохранённых настроек
        var settings = SettingsManager.LoadSettings();
        SelectedThemeIndex = (int)settings.ThemeVariant;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var settings = new AppSettings
        {
            ThemeVariant = (ThemeVariant)SelectedThemeIndex
        };
        SettingsManager.SaveSettings(settings);
    }
} 