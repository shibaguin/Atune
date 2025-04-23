using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.Models;
using Atune.ViewModels;

namespace Atune.Services
{
    public class PlayArtistService : IPlayArtistService
    {
        public async Task PlayArtistAsync(ArtistInfo artist)
        {
            if (artist == null)
                return;

            // Get MainViewModel from application lifetime
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                           ?.MainWindow?.DataContext as MainViewModel;
            if (mainVm?.MediaViewModelInstance == null)
                return;

            var mediaVm = mainVm.MediaViewModelInstance;

            // Clear existing queue
            mediaVm.ClearQueueCommand.Execute(null);

            // Enqueue all tracks for the artist
            foreach (var track in artist.Tracks)
            {
                mediaVm.AddToQueueCommand.Execute(track);
            }

            // Start playback
            await mediaVm.PlayNextInQueueCommand.ExecuteAsync(null);
        }
    }
} 
