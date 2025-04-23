using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.ViewModels;
using Atune.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Atune.Models;
using CommunityToolkit.Mvvm.Input;
using Avalonia;

namespace Atune.Views
{
    public partial class ArtistView : UserControl
    {
        public ArtistView()
        {
            InitializeComponent();
        }

        private async void PlayArtistButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ArtistViewModel vm)
            {
                var service = App.Current.Services.GetRequiredService<IPlayArtistService>();
                await service.PlayArtistAsync(vm.Artist);
            }
        }

        // Expose MediaViewModel's PlayTrackCommand for TrackListView
        public IAsyncRelayCommand<MediaItem>? PlayTrackCommand
        {
            get
            {
                var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                               ?.MainWindow?.DataContext as MainViewModel;
                return mainVm?.MediaViewModelInstance?.PlayTrackCommand;
            }
        }

        private void GoBack_Click(object? sender, RoutedEventArgs e)
        {
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            mainVm?.GoMediaCommand.Execute(null);
        }
    }
} 
