using System.Collections.Generic;
using System.Threading.Tasks;
using Atune.Models;

namespace Atune.Data.Interfaces
{
    public interface IAlbumRepository
    {
        Task<IEnumerable<Album>> GetAllAlbumsAsync();
        Task<Album?> GetAlbumByIdAsync(int albumId);
        Task<Album?> GetByTitleAsync(string title);
        Task<IEnumerable<MediaItem>> GetSongsForAlbumAsync(int albumId);
        Task<IEnumerable<Album>> SearchAlbumsAsync(string query, int limit = 50);
        Task<IEnumerable<Album>> GetAlbumsForArtistAsync(int artistId);
        Task AddAsync(Album album);
    }
}
