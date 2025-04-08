using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Atune.Models;
using Atune.Data;

namespace Atune.Services
{
    // Сервис для получения метаданных медиа файлов напрямую из хранилища (БД)
    public class MediaLibraryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MediaLibraryService> _logger;
        private readonly IMemoryCache _cache;

        public MediaLibraryService(AppDbContext context, ILogger<MediaLibraryService> logger, IMemoryCache memoryCache)
        {
            _context = context;
            _logger = logger;
            _cache = memoryCache;
        }

        // Метод для получения списка медиа файлов со всеми связанными данными
        public async Task<List<MediaItem>> GetMediaItemsAsync()
        {
            var cacheKey = "MediaLibraryService_GetMediaItems";

            if (_cache.TryGetValue(cacheKey, out List<MediaItem>? cachedMediaItems) && cachedMediaItems is not null)
            {
                return cachedMediaItems;
            }

            try
            {
                // Получаем данные из БД с подключением связанных сущностей: Album и TrackArtists с Artist
                var mediaItems = await _context.MediaItems
                    .Include(mi => mi.Album)
                    .Include(mi => mi.TrackArtists)
                        .ThenInclude(ta => ta.Artist)
                    .ToListAsync();

                // Устанавливаем опции кэширования (например, абсолютное время жизни 5 минут)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                // Сохраняем результат в кэше
                _cache.Set(cacheKey, mediaItems, cacheOptions);

                return mediaItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка медиа файлов из БД");
                return new List<MediaItem>();
            }
        }
    }
} 