using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Atune.Models;

namespace Atune.Views;


public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        InitializeDesignResources();
    }
    
    private void InitializeDesignResources()
    {
        // Инициализация ресурсов из статического класса
        Resources.Add("HeaderFontSize", DesignSettings.Dimensions.HeaderFontSize);
        Resources.Add("NavigationDividerWidth", DesignSettings.Dimensions.NavigationDividerWidth);
        Resources.Add("NavigationDividerHeight", DesignSettings.Dimensions.NavigationDividerHeight);
        Resources.Add("TopDockHeight", DesignSettings.Dimensions.TopDockHeight);
        Resources.Add("BarHeight", DesignSettings.Dimensions.BarHeight);
        Resources.Add("NavigationFontSize", DesignSettings.Dimensions.NavigationFontSize);
        
        // Пример добавления цветов
        // Resources.Add("PrimaryColor", Color.Parse(DesignSettings.Colors.PrimaryColor));
        // Resources.Add("SecondaryColor", Color.Parse(DesignSettings.Colors.SecondaryColor));
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