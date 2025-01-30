using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
namespace Atune.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }
    
    private void ToggleTheme(object sender, RoutedEventArgs e)
    {
        if (Application.Current is null) return;
        
        var newTheme = Application.Current.RequestedThemeVariant == ThemeVariant.Dark 
            ? ThemeVariant.Light 
            : ThemeVariant.Dark;
        
        Application.Current.RequestedThemeVariant = newTheme;
    }
    
    private void OnSettingsClicked(object? sender, RoutedEventArgs e)
    {
        // Реализация метода
    }
}