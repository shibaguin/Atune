using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atune.Data;
using Atune.Models;
using Microsoft.EntityFrameworkCore;
using Atune.Data.Interfaces;

namespace Atune.Data.Repositories
{
    public class AlbumRepository(AppDbContext context) : IAlbumRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<IEnumerable<Album>> GetAllAlbumsAsync()
        {
            return await _context.Albums.ToListAsync();
        }

        public async Task<Album?> GetAlbumByIdAsync(int albumId)
        {
            return await _context.Albums.FindAsync(albumId);
        }

        public async Task<IEnumerable<MediaItem>> GetSongsForAlbumAsync(int albumId)
        {
            return await _context.MediaItems
                .Where(m => m.AlbumId == albumId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Album>> SearchAlbumsAsync(string query, int limit = 50)
        {
            return await _context.Albums
                .Where(a => a.Title.Contains(query))
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Album>> GetAlbumsForArtistAsync(int artistId)
        {
            // Фильтруем альбомы через связывающую таблицу AlbumArtists (многие-ко-многим)
            return await _context.Albums
                .Where(a => a.AlbumArtists.Any(aa => aa.ArtistId == artistId))
                .ToListAsync();
        }
    }
}
