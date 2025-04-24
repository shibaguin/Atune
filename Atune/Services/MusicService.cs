using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atune.Data.Repositories;
using Atune.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Atune.Services
{
    public class MusicService(
        IAlbumRepository albumRepository,
        IArtistRepository artistRepository,
        IPlaylistRepository playlistRepository,
        IMemoryCache cache)
    {
        private readonly IAlbumRepository _albumRepository = albumRepository;
        private readonly IArtistRepository _artistRepository = artistRepository;
        private readonly IPlaylistRepository _playlistRepository = playlistRepository;
        private readonly IMemoryCache _cache = cache;

        // Получение всех альбомов
        public async Task<IEnumerable<Album>> GetAllAlbumsAsync()
        {
            const string cacheKey = "MusicService_GetAllAlbums";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Album>? cachedAlbums) && cachedAlbums is not null)
            {
                return cachedAlbums;
            }

            var albums = await _albumRepository.GetAllAlbumsAsync();
            _cache?.Set(cacheKey, albums, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
            return albums;
        }

        // Получение альбомов по артисту через связывающую таблицу
        public async Task<IEnumerable<Album>> GetAlbumsForArtistAsync(int artistId)
        {
            string cacheKey = "MusicService_GetAlbumsForArtist_" + artistId;
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Album>? cachedAlbums) && cachedAlbums is not null)
            {
                return cachedAlbums;
            }

            var albums = await _albumRepository.GetAlbumsForArtistAsync(artistId);
            _cache?.Set(cacheKey, albums, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
            return albums;
        }

        // Получение всех артистов
        public async Task<IEnumerable<Artist>> GetAllArtistsAsync()
        {
            const string cacheKey = "MusicService_GetAllArtists";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Artist>? cachedArtists) && cachedArtists is not null)
            {
                return cachedArtists;
            }

            var artists = await _artistRepository.GetAllArtistsAsync();
            _cache?.Set(cacheKey, artists, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
            return artists;
        }

        // Поиск артистов по имени
        public async Task<IEnumerable<Artist>> SearchArtistsAsync(string query)
        {
            string cacheKey = "MusicService_SearchArtists_" + query;
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Artist>? cachedArtists) && cachedArtists is not null)
            {
                return cachedArtists;
            }

            var artists = await _artistRepository.SearchArtistsAsync(query);
            _cache?.Set(cacheKey, artists, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
            return artists;
        }

        // Получение всех плейлистов
        public async Task<IEnumerable<Playlist>> GetAllPlaylistsAsync()
        {
            string cacheKey = "MusicService_GetAllPlaylists";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Playlist>? cachedPlaylists) && cachedPlaylists is not null)
            {
                return cachedPlaylists;
            }

            var playlists = await _playlistRepository.GetPlaylistsAsync();
            _cache?.Set(cacheKey, playlists, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
            return playlists;
        }

        // Пример добавления медиа элементов (треков) в плейлист
        public async Task<int> AddSongsToPlaylistAsync(int playlistId, IEnumerable<int> mediaItemIds)
        {
            return await _playlistRepository.AddToPlaylistAsync(playlistId, mediaItemIds);
        }

        // Другие операции, например удаление элемента из плейлиста, создание нового плейлиста, и т.д.
    }
}
