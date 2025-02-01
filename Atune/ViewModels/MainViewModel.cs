using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Styling;
using Atune.Views;
using Atune.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Atune.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    public enum SectionType { Home, Media, History, Settings }
    
    [ObservableProperty]
    private SectionType _selectedSection;
    
    [ObservableProperty]
    private string _headerText = "Atune";
    
    [ObservableProperty]
    private object? _currentView;

    private readonly ISettingsService _settingsService;

    public MainViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        CurrentView = ServiceLocator.GetService<HomeView>();
        LoadInitialSettings();
    }

    private void LoadInitialSettings()
    {
        var settings = _settingsService.LoadSettings();
        // Применяем настройки
    }

    [RelayCommand]
    private void GoHome()
    {
        HeaderText = "Atune";
        CurrentView = ServiceLocator.GetService<HomeView>();
    }

    [RelayCommand]
    private void GoMedia()
    {
        HeaderText = "Медиатека";
        CurrentView = ServiceLocator.GetService<MediaView>();
    }

    [RelayCommand]
    private void GoHistory()
    {
        HeaderText = "История";
        CurrentView = ServiceLocator.GetService<HistoryView>();
    }
    [RelayCommand]
    private void GoSettings()
    {
        HeaderText = "Настройки";
        CurrentView = ServiceLocator.GetService<SettingsView>();
    }
}