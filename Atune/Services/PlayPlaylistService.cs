using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.Models;
using Atune.Services;
using Atune.ViewModels;

namespace Atune.Services
{
    public interface IPlayPlaylistService
    {
        Task PlayPlaylistAsync(Playlist playlist);
    }

    public class PlayPlaylistService : IPlayPlaylistService
    {
        private readonly IPlaylistService _playlistService;

        public PlayPlaylistService(IPlaylistService playlistService)
        {
            _playlistService = playlistService;
        }

        public async Task PlayPlaylistAsync(Playlist playlist)
        {
            if (playlist == null)
                return;

            // Get songs in playlist
            var songs = (await _playlistService.GetSongsInPlaylistAsync(playlist.Id)).ToList();

            // Route through MainViewModel to play playlist
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                           ?.MainWindow?.DataContext as MainViewModel;
            var mediaVm = mainVm?.MediaViewModelInstance;
            if (mediaVm == null)
                return;

            // Clear existing queue
            mediaVm.ClearQueueCommand.Execute(null);
            // Enqueue songs
            foreach (var track in songs)
                mediaVm.AddToQueueCommand.Execute(track);
            // Start playback
            await mediaVm.PlayNextInQueueCommand.ExecuteAsync(null);
        }
    }
} 