using System.Collections.Generic;
using System.Threading.Tasks;
using Atune.Models;
using Atune.Data.Repositories;

namespace Atune.Services
{
    public interface IPlaylistService
    {
        Task<long> CreatePlaylistAsync(string name);
        Task<IEnumerable<Playlist>> GetPlaylistsAsync();
        Task<int> AddToPlaylistAsync(int playlistId, int mediaItemId);
        Task<IEnumerable<MediaItem>> GetSongsInPlaylistAsync(int playlistId);
        Task RemoveFromPlaylistAsync(int playlistId, int mediaItemId);
        Task<int> DeletePlaylistAsync(int playlistId);
        Task RenamePlaylistAsync(int playlistId, string newName);
    }

    public class PlaylistService(IPlaylistRepository playlistRepository) : IPlaylistService
    {
        private readonly IPlaylistRepository _playlistRepository = playlistRepository;

        public Task<long> CreatePlaylistAsync(string name)
            => _playlistRepository.CreatePlaylistAsync(name);

        public Task<IEnumerable<Playlist>> GetPlaylistsAsync()
            => _playlistRepository.GetPlaylistsAsync();

        public Task<int> AddToPlaylistAsync(int playlistId, int mediaItemId)
            => _playlistRepository.AddToPlaylistAsync(playlistId, new[] { mediaItemId });

        public Task<IEnumerable<MediaItem>> GetSongsInPlaylistAsync(int playlistId)
            => _playlistRepository.GetSongsInPlaylistAsync(playlistId);

        public Task RemoveFromPlaylistAsync(int playlistId, int mediaItemId)
            => _playlistRepository.RemoveFromPlaylistAsync(playlistId, mediaItemId);

        public Task<int> DeletePlaylistAsync(int playlistId)
            => _playlistRepository.DeletePlaylistAsync(playlistId);

        public Task RenamePlaylistAsync(int playlistId, string newName)
            => _playlistRepository.RenamePlaylistAsync(playlistId, newName);
    }
}
