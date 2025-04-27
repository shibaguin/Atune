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
        private readonly IPlaybackService _playbackService;

        public PlayPlaylistService(IPlaylistService playlistService, IPlaybackService playbackService)
        {
            _playlistService = playlistService;
            _playbackService = playbackService;
        }

        public async Task PlayPlaylistAsync(Playlist playlist)
        {
            if (playlist == null)
                return;

            // Get songs in playlist
            var songs = (await _playlistService.GetSongsInPlaylistAsync(playlist.Id)).ToList();

            // Use unified playback service
            _playbackService.ClearQueue();
            foreach (var track in songs)
                _playbackService.Enqueue(track);
            await _playbackService.Play();
        }
    }
}
