using Avalonia.Controls;
using Atune.ViewModels;
using System.Windows.Input;

namespace Atune.Views;

public partial class HomeView : UserControl
{
    // Parameterless constructor for XAML loader
    public HomeView()
    {
        InitializeComponent();
    }

    // DI constructor: inject HomeViewModel, set DataContext, then initialize view
    public HomeView(HomeViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    // Expose ViewModel commands for XAML binding
    public ICommand? PlayTopTrackCommand => (DataContext as HomeViewModel)?.PlayTopTrackCommand;
    public ICommand? PlayTopAlbumCommand => (DataContext as HomeViewModel)?.PlayTopAlbumCommand;
    public ICommand? PlayTopPlaylistCommand => (DataContext as HomeViewModel)?.PlayTopPlaylistCommand;
    public ICommand? PlayRecentTrackCommand => (DataContext as HomeViewModel)?.PlayRecentTrackCommand;
}

