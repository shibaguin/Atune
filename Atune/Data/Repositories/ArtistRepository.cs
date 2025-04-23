using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atune.Data;
using Atune.Models;
using Microsoft.EntityFrameworkCore;

namespace Atune.Data.Repositories
{
    public class ArtistRepository : IArtistRepository
    {
        private readonly AppDbContext _context;
        public ArtistRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Artist>> GetAllArtistsAsync()
        {
            return await _context.Artists.ToListAsync();
        }

        public async Task<Artist?> GetArtistByIdAsync(int artistId)
        {
            return await _context.Artists.FindAsync(artistId);
        }

        public async Task<IEnumerable<Artist>> SearchArtistsAsync(string query, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<Artist>();
            var normalized = query.ToLowerInvariant();
            return await _context.Artists
                .Where(a => a.Name.ToLower().Contains(normalized))
                .Take(limit)
                .ToListAsync();
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
    }
} 