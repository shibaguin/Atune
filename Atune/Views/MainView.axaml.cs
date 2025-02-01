using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Atune.Models;
using Atune.ViewModels;

namespace Atune.Views;


public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        InitializeDesignResources();
        DataContext = ServiceLocator.GetService<MainViewModel>();
    }

    private void InitializeDesignResources()
    {
        Resources.Add("HeaderFontSize", DesignSettings.Dimensions.HeaderFontSize);
        Resources.Add("NavigationDividerWidth", DesignSettings.Dimensions.NavigationDividerWidth);
        Resources.Add("NavigationDividerHeight", DesignSettings.Dimensions.NavigationDividerHeight);
        Resources.Add("TopDockHeight", DesignSettings.Dimensions.TopDockHeight);
        Resources.Add("BarHeight", DesignSettings.Dimensions.BarHeight);
        Resources.Add("NavigationFontSize", DesignSettings.Dimensions.NavigationFontSize);
        Resources.Add("BarPadding", DesignSettings.Dimensions.BarPadding);
    }
}