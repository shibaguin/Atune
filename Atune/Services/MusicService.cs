using System.Collections.Generic;
using System.Threading.Tasks;
using Atune.Data.Repositories;
using Atune.Models;

namespace Atune.Services
{
    public class MusicService
    {
        private readonly IAlbumRepository _albumRepository;
        private readonly IArtistRepository _artistRepository;
        private readonly IPlaylistRepository _playlistRepository;

        // Если потребуется, можно внедрить и другие репозитории, например для MediaItem или Folders.
        public MusicService(
            IAlbumRepository albumRepository,
            IArtistRepository artistRepository,
            IPlaylistRepository playlistRepository)
        {
            _albumRepository = albumRepository;
            _artistRepository = artistRepository;
            _playlistRepository = playlistRepository;
        }

        // Получение всех альбомов
        public async Task<IEnumerable<Album>> GetAllAlbumsAsync()
        {
            return await _albumRepository.GetAllAlbumsAsync();
        }

        // Получение альбомов по артисту через связывающую таблицу
        public async Task<IEnumerable<Album>> GetAlbumsForArtistAsync(int artistId)
        {
            return await _albumRepository.GetAlbumsForArtistAsync(artistId);
        }

        // Получение всех артистов
        public async Task<IEnumerable<Artist>> GetAllArtistsAsync()
        {
            return await _artistRepository.GetAllArtistsAsync();
        }

        // Поиск артистов по имени
        public async Task<IEnumerable<Artist>> SearchArtistsAsync(string query)
        {
            return await _artistRepository.SearchArtistsAsync(query);
        }

        // Получение всех плейлистов
        public async Task<IEnumerable<Playlist>> GetAllPlaylistsAsync()
        {
            return await _playlistRepository.GetPlaylistsAsync();
        }

        // Пример добавления медиа элементов (треков) в плейлист
        public async Task<int> AddSongsToPlaylistAsync(int playlistId, IEnumerable<int> mediaItemIds)
        {
            return await _playlistRepository.AddToPlaylistAsync(playlistId, mediaItemIds);
        }

        // Другие операции, например удаление элемента из плейлиста, создание нового плейлиста, и т.д.
    }
} 