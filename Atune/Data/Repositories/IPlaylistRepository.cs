using System.Collections.Generic;
using System.Threading.Tasks;
using Atune.Models;

namespace Atune.Data.Repositories
{
    public interface IPlaylistRepository
    {
        Task<long> CreatePlaylistAsync(string name);
        Task<IEnumerable<Playlist>> GetPlaylistsAsync();
        Task<int> AddToPlaylistAsync(int playlistId, IEnumerable<int> mediaItemIds);
        Task<IEnumerable<MediaItem>> GetSongsInPlaylistAsync(int playlistId);
        Task RemoveFromPlaylistAsync(int playlistId, int mediaItemId);
        Task<int> DeletePlaylistAsync(int playlistId);
        Task RenamePlaylistAsync(int playlistId, string newName);
    }
} 
