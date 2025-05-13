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

        private static string NormalizeAlbumTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            // Заменяем различные варианты разделителей на стандартный
            var normalized = title.Replace("-", " ")
                               .Replace("\\", " ")
                               .Replace("/", " ")
                               .Replace("&", " and ")
                               .Trim();

            // Удаляем множественные пробелы
            while (normalized.Contains("  "))
            {
                normalized = normalized.Replace("  ", " ");
            }

            return normalized;
        }

        public async Task<IEnumerable<Album>> GetAllAlbumsAsync()
        {
            return await _context.Albums.ToListAsync();
        }

        public async Task<Album?> GetAlbumByIdAsync(int albumId)
        {
            return await _context.Albums.FindAsync(albumId);
        }

        public async Task<Album?> GetByTitleAsync(string title)
        {
            var normalizedTitle = NormalizeAlbumTitle(title);
            var albums = await _context.Albums.ToListAsync();
            return albums.FirstOrDefault(a => NormalizeAlbumTitle(a.Title) == normalizedTitle);
        }

        public async Task<IEnumerable<MediaItem>> GetSongsForAlbumAsync(int albumId)
        {
            return await _context.MediaItems
                .Where(m => m.AlbumId == albumId)
                .Include(m => m.Album)
                .Include(m => m.TrackArtists)
                    .ThenInclude(ta => ta.Artist)
                .ToListAsync();
        }

        public async Task<IEnumerable<Album>> SearchAlbumsAsync(string query, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<Album>();

            var normalizedQuery = NormalizeAlbumTitle(query);
            return await _context.Albums
                .Where(a => NormalizeAlbumTitle(a.Title).Contains(normalizedQuery, System.StringComparison.CurrentCultureIgnoreCase))
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
