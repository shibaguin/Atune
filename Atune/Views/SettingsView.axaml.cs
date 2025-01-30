using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Atune.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
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
}