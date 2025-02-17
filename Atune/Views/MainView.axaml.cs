using Avalonia.Controls;
using Avalonia.Input;
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

    private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SearchCommand.Execute(null);
            }
        }
    }

    private void SearchSuggestions_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is string selectedSuggestion &&
            DataContext is MainViewModel vm)
        {
            listBox.SelectedItem = null;
            vm.SearchQuery = selectedSuggestion;
            vm.IsSuggestionsOpen = false;
        }
    }
}