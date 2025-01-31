using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Styling;
using Atune.Views;

namespace Atune.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _headerText = "Atune";
    
    [ObservableProperty]
    private object _currentContent;
    
    public MainViewModel()
    {
        CurrentContent = "Welcome to Avalonia!";
    }
    [RelayCommand]
    private void ToggleTheme()
    {
        if (Application.Current is null) return;
        Application.Current.RequestedThemeVariant = 
            Application.Current.ActualThemeVariant == ThemeVariant.Dark 
                ? ThemeVariant.Light 
                : ThemeVariant.Dark;
    }

    [RelayCommand]
    private void ShowSettings()
    {
        HeaderText = "Настройки";
        CurrentContent = new SettingsView();
    }

    [RelayCommand]
    private void GoHome()
    {
        HeaderText = "Atune";
        // 
        CurrentContent = "Welcome to Avalonia!";
        
        //
        CurrentContent = new HomeView();
        //
    }
    
    [RelayCommand]
    private void GoMedia()
    {
        HeaderText = "Медиатека";
        //
        CurrentContent = "Welcome to Avalonia!";
        
        //
        CurrentContent = new MediaView();
        //
    }
    
    [RelayCommand]
    private void GoHistory()
    {
        HeaderText = "История";
        CurrentContent = "Welcome to Avalonia!";
    }
}