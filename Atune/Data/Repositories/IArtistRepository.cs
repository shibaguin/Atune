using System.Collections.Generic;
using System.Threading.Tasks;
using Atune.Models;

namespace Atune.Data.Repositories
{
    public interface IArtistRepository
    {
        Task<IEnumerable<Artist>> GetAllArtistsAsync();
        Task<Artist?> GetArtistByIdAsync(int artistId);
        Task<IEnumerable<Artist>> SearchArtistsAsync(string query, int limit = 50);
        Task<IEnumerable<MediaItem>> GetSongsForArtistAsync(int artistId);
    }
} 