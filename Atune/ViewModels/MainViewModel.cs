using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Atune.Views;
using Atune.Services;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Atune.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    public enum SectionType { Home, Media, History, Settings }
    
    [ObservableProperty]
    private SectionType _selectedSection;
    
    [ObservableProperty]
    private string _headerText = "Atune";
    
    [ObservableProperty]
    private Control? _currentView;

    private readonly ISettingsService _settingsService;
    private readonly Dictionary<SectionType, Control> _views;
    private readonly Func<Type, ViewModelBase> _viewModelFactory;
    private readonly Func<Type, Control> _viewFactory;

    public MainViewModel(
        ISettingsService settingsService,
        Func<Type, ViewModelBase> viewModelFactory,
        Func<Type, Control> viewFactory)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        _viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        
        _views = new Dictionary<SectionType, Control>
        {
            [SectionType.Home] = viewFactory(typeof(HomeView)),
            [SectionType.Media] = viewFactory(typeof(MediaView)),
            [SectionType.History] = viewFactory(typeof(HistoryView)),
            [SectionType.Settings] = viewFactory(typeof(SettingsView))
        };
        
        CurrentView = _views[SectionType.Home];
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
        CurrentView = _views[SectionType.Home];
    }

    [RelayCommand]
    private void GoMedia()
    {
        HeaderText = "Медиатека";
        CurrentView = _views[SectionType.Media];
    }

    [RelayCommand]
    private void GoHistory()
    {
        HeaderText = "История";
        CurrentView = _views[SectionType.History];
    }
    [RelayCommand]
    private void GoSettings()
    {
        if (_views.TryGetValue(SectionType.Settings, out var vm))
        {
            HeaderText = "Настройки";
            CurrentView = vm;
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    [RelayCommand]
    private void AddMedia()
    {
        if (CurrentView?.DataContext is MediaViewModel mediaVM)
        {
            mediaVM.AddMediaCommand.Execute(null);
        }
    }

    public bool CurrentPageIsMedia => 
        CurrentView is MediaView;
}