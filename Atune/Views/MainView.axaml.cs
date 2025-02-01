using Avalonia.Controls;
using Atune.Models;
using Atune.ViewModels;

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
        Resources.Add("HeaderFontSize", DesignSettings.Dimensions.HeaderFontSize);
        Resources.Add("NavigationDividerWidth", DesignSettings.Dimensions.NavigationDividerWidth);
        Resources.Add("NavigationDividerHeight", DesignSettings.Dimensions.NavigationDividerHeight);
        Resources.Add("TopDockHeight", DesignSettings.Dimensions.TopDockHeight);
        Resources.Add("BarHeight", DesignSettings.Dimensions.BarHeight);
        Resources.Add("NavigationFontSize", DesignSettings.Dimensions.NavigationFontSize);
        Resources.Add("BarPadding", DesignSettings.Dimensions.BarPadding);
    }
}