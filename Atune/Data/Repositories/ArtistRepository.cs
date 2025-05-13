using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atune.Data;
using Atune.Models;
using Microsoft.EntityFrameworkCore;
using Atune.Data.Interfaces;

namespace Atune.Data.Repositories
{
    public class ArtistRepository(AppDbContext context) : IArtistRepository
    {
        private readonly AppDbContext _context = context;

        private static string NormalizeArtistName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Приводим к нижнему регистру для регистронезависимого сравнения
            var normalized = name.ToLowerInvariant();

            // Заменяем различные варианты разделителей на стандартный
            normalized = normalized.Replace("-", "/")
                               .Replace("\\", "/")
                               .Replace("&", " and ")
                               .Replace("the ", "") // Удаляем артикль "the" в начале
                               .Trim();

            // Удаляем множественные пробелы
            while (normalized.Contains("  "))
            {
                normalized = normalized.Replace("  ", " ");
            }

            return normalized;
        }

        public async Task<IEnumerable<Artist>> GetAllArtistsAsync()
        {
            return await _context.Artists.ToListAsync();
        }

        public async Task<Artist?> GetArtistByIdAsync(int artistId)
        {
            return await _context.Artists.FindAsync(artistId);
        }

        public async Task<Artist?> GetByNameAsync(string name)
        {
            var normalizedName = NormalizeArtistName(name);
            var artists = await _context.Artists.ToListAsync();
            return artists.FirstOrDefault(a => NormalizeArtistName(a.Name) == normalizedName);
        }

        public async Task<IEnumerable<MediaItem>> GetSongsForArtistAsync(int artistId)
        {
            // Load related Album and TrackArtists with Artist for correct data in ArtistView
            return await _context.MediaItems
                .Include(m => m.Album)
                .Include(m => m.TrackArtists)
                    .ThenInclude(ta => ta.Artist)
                .Where(m => m.TrackArtists.Any(ta => ta.ArtistId == artistId))
                .ToListAsync();
        }

        public async Task<IEnumerable<Artist>> SearchArtistsAsync(string query, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<Artist>();
            
            var normalizedQuery = NormalizeArtistName(query);
            var artists = await _context.Artists.ToListAsync();
            return artists
                .Where(a => NormalizeArtistName(a.Name).Contains(normalizedQuery, System.StringComparison.CurrentCultureIgnoreCase))
                .Take(limit);
        }

        public async Task AddAsync(Artist artist)
        {
            await _context.Artists.AddAsync(artist);
        }
    }
}
