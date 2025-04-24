namespace Atune.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Atune.Data;
    using Atune.Data.Interfaces;
    using Atune.Models.Dtos;
    using Microsoft.EntityFrameworkCore;

    public class HomeRepository : IHomeRepository
    {
        private readonly AppDbContext _context;
        public HomeRepository(AppDbContext context) => _context = context;

        public async Task<IEnumerable<TopTrackDto>> GetTopTracksAsync(int count = 5)
        {
            return await _context.PlayHistories
                .Include(ph => ph.MediaItem)
                    .ThenInclude(mi => mi.TrackArtists)
                        .ThenInclude(ta => ta.Artist)
                .GroupBy(ph => ph.MediaItem)
                .Select(g => new TopTrackDto
                {
                    Id = g.Key.Id,
                    Title = g.Key.Title,
                    CoverArtPath = g.Key.CoverArt,
                    ArtistName = g.Key.TrackArtists.FirstOrDefault()!.Artist.Name,
                    Duration = g.Key.Duration,
                    PlayCount = g.Count()
                })
                .OrderByDescending(dto => dto.PlayCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<TopAlbumDto>> GetTopAlbumsAsync(int count = 5)
        {
            return await _context.PlayHistories
                .Include(ph => ph.MediaItem)
                    .ThenInclude(mi => mi.Album)
                        .ThenInclude(a => a.AlbumArtists)
                            .ThenInclude(aa => aa.Artist)
                .GroupBy(ph => ph.MediaItem.Album)
                .Select(g => new TopAlbumDto
                {
                    Id = g.Key.Id,
                    Title = g.Key.Title,
                    CoverArtPath = g.Key.CoverArtPath,
                    ArtistName = g.Key.AlbumArtists.FirstOrDefault()!.Artist.Name,
                    Year = (uint)g.Key.Year,
                    TrackCount = g.Key.Tracks.Count,
                    PlayCount = g.Count()
                })
                .OrderByDescending(dto => dto.PlayCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<TopPlaylistDto>> GetTopPlaylistsAsync(int count = 5)
        {
            return await _context.PlayHistories
                .Include(ph => ph.MediaItem)
                    .ThenInclude(mi => mi.PlaylistMediaItems)
                        .ThenInclude(pmi => pmi.Playlist)
                .Where(ph => ph.MediaItem.PlaylistMediaItems.Any())
                .Select(ph => new { ph, Playlist = ph.MediaItem.PlaylistMediaItems.First().Playlist })
                .GroupBy(x => x.Playlist)
                .Select(g => new TopPlaylistDto
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    CoverArtPath = g.Key.PlaylistMediaItems.FirstOrDefault()!.MediaItem.CoverArt,
                    TrackCount = g.Key.PlaylistMediaItems.Count,
                    PlayCount = g.Count()
                })
                .OrderByDescending(dto => dto.PlayCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<RecentTrackDto>> GetRecentTracksAsync(int count = 5)
        {
            return await _context.PlayHistories
                .Include(ph => ph.MediaItem)
                    .ThenInclude(mi => mi.TrackArtists)
                        .ThenInclude(ta => ta.Artist)
                .OrderByDescending(ph => ph.PlayedAt)
                .Select(ph => new RecentTrackDto
                {
                    Id = ph.MediaItem.Id,
                    Title = ph.MediaItem.Title,
                    CoverArtPath = ph.MediaItem.CoverArt,
                    ArtistName = ph.MediaItem.TrackArtists.FirstOrDefault()!.Artist.Name,
                    LastPlayedAt = ph.PlayedAt
                })
                .Distinct()
                .Take(count)
                .ToListAsync();
        }
    }
} 