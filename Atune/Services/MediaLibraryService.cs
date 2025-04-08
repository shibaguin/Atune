using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Atune.Models;
using Atune.Data;

namespace Atune.Services
{
    // Сервис для получения метаданных медиа файлов напрямую из хранилища (БД)
    public class MediaLibraryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MediaLibraryService> _logger;

        public MediaLibraryService(AppDbContext context, ILogger<MediaLibraryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Метод для получения списка медиа файлов со всеми связанными данными
        public async Task<List<MediaItem>> GetMediaItemsAsync()
        {
            try
            {
                // Получаем данные из БД с подключением связанных сущностей: Album и TrackArtists с Artist
                return await _context.MediaItems
                    .Include(mi => mi.Album)
                    .Include(mi => mi.TrackArtists)
                        .ThenInclude(ta => ta.Artist)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка медиа файлов из БД");
                return new List<MediaItem>();
            }
        }
    }
} 