using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Styling;
using Atune.Views;

namespace Atune.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    public enum SectionType { Home, Media, History, Settings }
    
    [ObservableProperty]
    private SectionType _selectedSection;
    
    [ObservableProperty]
    private string _headerText = "Atune";
    
    [ObservableProperty]
    private object _currentContent = new HomeView();

    [RelayCommand]
    private void GoHome()
    {
        HeaderText = "Atune";
        CurrentContent = new HomeView();
    }

    [RelayCommand]
    private void GoMedia()
    {
        HeaderText = "Медиатека";
        CurrentContent = new MediaView();
    }

    [RelayCommand]
    private void GoHistory()
    {
        HeaderText = "История";
        CurrentContent = new HistoryView();
    }
    [RelayCommand]
    private void GoSettings()
    {
        HeaderText = "История";
        CurrentContent = new SettingsView();
    }
}