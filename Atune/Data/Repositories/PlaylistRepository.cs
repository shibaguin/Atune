using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atune.Data;
using Atune.Models;
using Microsoft.EntityFrameworkCore;

namespace Atune.Data.Repositories
{
    public class PlaylistRepository : IPlaylistRepository
    {
        private readonly AppDbContext _context;
        public PlaylistRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<long> CreatePlaylistAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;

            // Проверяем, существует ли уже плейлист с таким именем
            var existing = await _context.Playlists.FirstOrDefaultAsync(p => p.Name == name);
            if (existing != null)
                return -1;

            var playlist = new Playlist { Name = name };
            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();
            return playlist.Id;
        }

        public async Task<IEnumerable<Playlist>> GetPlaylistsAsync()
        {
            return await _context.Playlists.ToListAsync();
        }

        public async Task<int> AddToPlaylistAsync(int playlistId, IEnumerable<int> mediaItemIds)
        {
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistMediaItems)
                .FirstOrDefaultAsync(p => p.Id == playlistId);
            if (playlist == null)
                return 0;

            int count = 0;
            foreach(var mediaId in mediaItemIds)
            {
                if (!playlist.PlaylistMediaItems.Any(pmi => pmi.MediaItemId == mediaId))
                {
                    playlist.PlaylistMediaItems.Add(new PlaylistMediaItem { MediaItemId = mediaId, PlaylistId = playlistId });
                    count++;
                }
            }
            await _context.SaveChangesAsync();
            return count;
        }

        public async Task<IEnumerable<MediaItem>> GetSongsInPlaylistAsync(int playlistId)
        {
            return await _context.PlaylistMediaItems
                .Where(pmi => pmi.PlaylistId == playlistId)
                .Include(pmi => pmi.MediaItem)
                .Select(pmi => pmi.MediaItem)
                .ToListAsync();
        }

        public async Task RemoveFromPlaylistAsync(int playlistId, int mediaItemId)
        {
            var item = await _context.PlaylistMediaItems
                .FirstOrDefaultAsync(pmi => pmi.PlaylistId == playlistId && pmi.MediaItemId == mediaItemId);
            if (item != null)
            {
                _context.PlaylistMediaItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> DeletePlaylistAsync(int playlistId)
        {
            var playlist = await _context.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return 0;
            _context.Playlists.Remove(playlist);
            return await _context.SaveChangesAsync();
        }
    }
} 