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
            return await _context.Artists
                .Where(a => a.Name.Contains(query))
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<MediaItem>> GetSongsForArtistAsync(int artistId)
        {
            // Предполагается, что MediaItem имеет связь с артистами через коллекцию TrackArtists
            return await _context.MediaItems
                .Where(m => m.TrackArtists.Any(ta => ta.Artist.Id == artistId))
                .ToListAsync();
        }
    }
} 