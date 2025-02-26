using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Atune.Models;
using Atune.ViewModels;

namespace Atune.Views;


public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
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

    public void PlayButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PlayCommand.Execute(null);
        }
    }

    public void StopButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.StopCommand.Execute(null);
        }
    }
}