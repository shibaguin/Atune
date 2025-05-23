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
    using AutoMapper;

    public class HomeRepository : IHomeRepository
    {
        private readonly AppDbContext _context;
        private readonly AutoMapper.IMapper _mapper;
        public HomeRepository(AppDbContext context, AutoMapper.IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TopTrackDto>> GetTopTracksAsync(int count = 5)
        {
            // Load play histories with related media and artist information into memory
            var histories = await _context.PlayHistories
                .Include(ph => ph.MediaItem)
                    .ThenInclude(mi => mi.TrackArtists)
                        .ThenInclude(ta => ta.Artist)
                .ToListAsync();
            // If no play history exists, fallback to first tracks sorted by title
            if (!histories.Any())
            {
                var items = await _context.MediaItems
                    .Include(m => m.TrackArtists)
                        .ThenInclude(ta => ta.Artist)
                    .OrderBy(m => m.Title)
                    .Take(count)
                    .ToListAsync();
                return items.Select(m => new TopTrackDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    CoverArtPath = m.CoverArt,
                    ArtistName = m.TrackArtists.FirstOrDefault()?.Artist.Name ?? string.Empty,
                    Duration = m.Duration,
                    PlayCount = 0
                })
                .ToList();
            }
            // Group in memory and project to DTOs
            var topTracks = histories
                .GroupBy(ph => ph.MediaItem)
                .Select(g => new TopTrackDto
                {
                    Id = g.Key.Id,
                    Title = g.Key.Title,
                    CoverArtPath = g.Key.CoverArt,
                    ArtistName = g.Key.TrackArtists.FirstOrDefault()?.Artist.Name ?? string.Empty,
                    Duration = g.Key.Duration,
                    PlayCount = g.Count()
                })
                .OrderByDescending(dto => dto.PlayCount)
                .Take(count)
                .ToList();
            // Корректируем TrackCount: получаем точное число через запрос к БД
            foreach (var dto in topTracks)
            {
                dto.TrackCount = await _context.PlaylistMediaItems.CountAsync(pmi => pmi.MediaItemId == dto.Id);
            }
            return topTracks;
        }

        public async Task<IEnumerable<TopAlbumDto>> GetTopAlbumsAsync(int count = 5)
        {
            // Загружаем все альбомы из БД с подсчетом количества треков и воспроизведений
            var dtos = await _context.Albums
                .Select(a => new TopAlbumDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    CoverArtPath = a.CoverArtPath,
                    ArtistName = a.Tracks
                        .SelectMany(mi => mi.TrackArtists)
                        .Select(ta => ta.Artist.Name)
                        .FirstOrDefault() ?? string.Empty,
                    Year = a.Year != 0
                        ? (uint)a.Year
                        : (uint)a.Tracks.Select(mi => mi.Year).FirstOrDefault(),
                    TrackCount = a.Tracks.Count,
                    PlayCount = _context.PlayHistories.Count(ph => ph.MediaItem.AlbumId == a.Id)
                })
                .ToListAsync();

            // Заполняем AlbumIds для каждого dto
            foreach (var dto in dtos)
            {
                dto.AlbumIds.Add(dto.Id);
            }

            // Устраняем дубли альбомов и суммируем TrackCount и PlayCount
            var result = dtos
                .GroupBy(d => new { d.Title, d.ArtistName, d.Year })
                .Select(g =>
                {
                    var list = g.ToList();
                    var first = list.First();
                    first.TrackCount = list.Sum(x => x.TrackCount);
                    first.PlayCount = list.Sum(x => x.PlayCount);
                    // Объединяем все идентификаторы альбомов для загрузки всех треков
                    first.AlbumIds = list.Select(x => x.Id).ToList();
                    return first;
                })
                .OrderByDescending(d => d.PlayCount)
                .ThenBy(d => d.Title)
                .Take(count)
                .ToList();

            return result;
        }

        public async Task<IEnumerable<TopPlaylistDto>> GetTopPlaylistsAsync(int count = 5)
        {
            // Load play histories with related playlists and media into memory
            var histories = await _context.PlayHistories
                .Include(ph => ph.MediaItem)
                    .ThenInclude(mi => mi.PlaylistMediaItems)
                        .ThenInclude(pmi => pmi.Playlist)
                            .ThenInclude(pl => pl.PlaylistMediaItems)
                                .ThenInclude(pmi2 => pmi2.MediaItem)
                .ToListAsync();
            // If no play history exists, fallback to first playlists
            if (!histories.Any())
            {
                var playlists = await _context.Playlists
                    .Include(p => p.PlaylistMediaItems)
                        .ThenInclude(pmi => pmi.MediaItem)
                    .OrderBy(p => p.Name)
                    .Take(count)
                    .ToListAsync();
                return playlists.Select(p => new TopPlaylistDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CoverArtPath = p.PlaylistMediaItems.FirstOrDefault()?.MediaItem.CoverArt ?? string.Empty,
                    TrackCount = p.PlaylistMediaItems.Count,
                    PlayCount = 0
                })
                .ToList();
            }
            // Group in memory and project to DTOs
            var topPlaylists = histories
                .Where(ph => ph.MediaItem.PlaylistMediaItems.Any())
                .Select(ph => ph.MediaItem.PlaylistMediaItems.First().Playlist)
                .GroupBy(pl => pl)
                .Select(g => new TopPlaylistDto
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    CoverArtPath = g.Key.PlaylistMediaItems.FirstOrDefault()?.MediaItem.CoverArt ?? string.Empty,
                    TrackCount = g.Key.PlaylistMediaItems.Count,
                    PlayCount = g.Count()
                })
                .OrderByDescending(dto => dto.PlayCount)
                .Take(count)
                .ToList();
            // Корректируем TrackCount: получаем точное число через запрос к БД
            foreach (var dto in topPlaylists)
            {
                dto.TrackCount = await _context.PlaylistMediaItems.CountAsync(pmi => pmi.PlaylistId == dto.Id);
            }
            return topPlaylists;
        }

        public async Task<IEnumerable<RecentTrackDto>> GetRecentTracksAsync(int count = 5)
        {
            // Fallback to latest media by ReleaseDate when no history
            if (!await _context.PlayHistories.AnyAsync())
            {
                var items = await _context.MediaItems
                    .Include(mi => mi.TrackArtists)
                        .ThenInclude(ta => ta.Artist)
                    .OrderByDescending(mi => mi.ReleaseDate)
                    .Take(count)
                    .ToListAsync();
                return items.Select(mi => new RecentTrackDto
                {
                    Id = mi.Id,
                    Title = mi.Title,
                    CoverArtPath = mi.CoverArt,
                    ArtistName = mi.TrackArtists.FirstOrDefault()?.Artist.Name ?? string.Empty,
                    LastPlayedAt = DateTime.Now
                })
                .ToList();
            }

            // Load play histories and related artists into memory
            var histories = await _context.PlayHistories
                .Include(ph => ph.MediaItem)
                    .ThenInclude(mi => mi.TrackArtists)
                        .ThenInclude(ta => ta.Artist)
                .OrderByDescending(ph => ph.PlayedAt)
                .ToListAsync();
            // For each media item, pick the most recent play
            var recentHistories = histories
                .GroupBy(ph => ph.MediaItem)
                .Select(g => g.OrderByDescending(ph => ph.PlayedAt).First())
                .OrderByDescending(ph => ph.PlayedAt)
                .Take(count)
                .ToList();
            // Project to DTOs
            return recentHistories.Select(ph => new RecentTrackDto
            {
                Id = ph.MediaItem.Id,
                Title = ph.MediaItem.Title,
                CoverArtPath = ph.MediaItem.CoverArt,
                ArtistName = ph.MediaItem.TrackArtists.FirstOrDefault()?.Artist.Name ?? string.Empty,
                LastPlayedAt = ph.PlayedAt
            })
            .ToList();
        }
    }
}