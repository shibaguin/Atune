using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Atune.Models;
using Atune.Data;
using Atune.Services;
using Microsoft.Extensions.Logging;
using Atune.Exceptions;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace Atune.Services
{
    // Сервис для работы с БД медиа-записей, инкапсулирующий логику обращения к AppDbContext.
    // Service for working with the media database, encapsulating the logic of accessing AppDbContext.  
    public class MediaDatabaseService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILoggerService _logger;
        private readonly IMemoryCache _cache;

        public MediaDatabaseService(IDbContextFactory<AppDbContext> dbContextFactory, ILoggerService logger, IMemoryCache cache)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _cache = cache;
        }

        public async Task<bool> CanConnectAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            bool canConnect = await dbContext.Database.CanConnectAsync();
            _logger.LogInformation($"Database connection status: {canConnect}");
            return canConnect;
        }

        public async Task<bool> ExistsByPathAsync(string path)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            bool exists = await dbContext.ExistsByPathAsync(path);
            _logger.LogInformation($"Checking existence of media item by path: {path}, Exists: {exists}");
            return exists;
        }

        public async Task AddMediaItemAsync(MediaItem item)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            try
            {
                // Проверка: ищем существующий альбом по названию
                var existingAlbum = await dbContext.Albums.FirstOrDefaultAsync(a => a.Title == item.Album.Title);
                if (existingAlbum != null)
                {
                    // Если альбом найден, используем его
                    item.Album = existingAlbum;
                    item.AlbumId = existingAlbum.Id;
                }

                // Если есть путь обложки, сохраняем его в сущности Album
                if (!string.IsNullOrEmpty(item.Album.CoverArtPath))
                {
                    if (existingAlbum != null)
                    {
                        existingAlbum.CoverArtPath = item.Album.CoverArtPath;
                    }
                    // Для новой сущности Album путь уже установлен в item.Album
                }
                
                // Проверка для каждого артиста в списке TrackArtists
                foreach (var trackArtist in item.TrackArtists)
                {
                    if (trackArtist.Artist != null)
                    {
                        var existingArtist = await dbContext.Artists.FirstOrDefaultAsync(a => a.Name == trackArtist.Artist.Name);
                        if (existingArtist != null)
                        {
                            // Если артист найден, используем его
                            trackArtist.Artist = existingArtist;
                        }
                    }
                }
                
                // Добавляем объект в базу через метод AddMediaAsync (который выполняет await MediaItems.AddAsync(media))
                await dbContext.AddMediaAsync(item);
                
                var artistNames = string.Join(", ", item.TrackArtists
                                            .Select(ta => ta.Artist?.Name)
                                            .Where(name => !string.IsNullOrEmpty(name)));
                _logger.LogInformation($"Media item added successfully: Title='{item.Title}', Album='{(item.Album?.Title ?? "Unknown Album")}', Artist(s)='{artistNames}', Path='{item.Path}'");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding media item: Title='{item.Title}', Path='{item.Path}'", ex);
            }
        }

        public async Task ValidateDatabaseRecordsAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var invalidRecords = await dbContext.MediaItems
                .Where(m => string.IsNullOrEmpty(m.Path) || !File.Exists(m.Path))
                .ToListAsync();
            _logger.LogInformation($"Found {invalidRecords.Count} invalid media records in the database.");
            
            foreach (var record in invalidRecords)
            {
                var artistNames = string.Join(", ", record.TrackArtists
                                            .Select(ta => ta.Artist?.Name)
                                            .Where(name => !string.IsNullOrEmpty(name)));
                _logger.LogInformation($"Removing invalid record: Title='{record.Title}', Album='{(record.Album?.Title ?? "Unknown Album")}', Artist(s)='{artistNames}', Path='{record.Path}'");
                dbContext.MediaItems.Remove(record);
            }
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Database records validated and changes saved.");
        }

        public string GetDatabasePath()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            string dbPath = dbContext.Database.GetDbConnection().DataSource ?? "not defined";
            _logger.LogInformation($"Database path retrieved: {dbPath}");
            return dbPath;
        }

        public Task<bool> ExistsByArtistAsync(string artist)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.MediaItems
                .AnyAsync(m => m.TrackArtists
                    .Any(ta => ta.Artist != null && ta.Artist.Name == artist));
        }

        // Новая реализация: получение медиа-объекта по пути
        public async Task<MediaItem?> GetMediaItemByPathAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            string cacheKey = "MediaDatabaseService_GetMediaItemByPath_" + path;
            if (_cache.TryGetValue(cacheKey, out MediaItem? cachedItem) && cachedItem is not null)
            {
                return cachedItem;
            }
            
            using var dbContext = _dbContextFactory.CreateDbContext();
            var mediaItem = await dbContext.MediaItems
                .AsNoTracking()
                .Include(m => m.Album)
                .Include(m => m.TrackArtists)
                    .ThenInclude(ta => ta.Artist)
                .FirstOrDefaultAsync(m => m.Path == path);
            
            if (mediaItem != null)
            {
                _cache.Set(cacheKey, mediaItem, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
            }
            return mediaItem;
        }

        // Новый метод для получения медиа-объектов с включенными сущностями Album и Artist через TrackArtists:
        public async Task<List<MediaItem>> GetAllMediaItemsAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            // Используем Include для загрузки связей с Album и Artist (через TrackArtists)
            return await dbContext.MediaItems
                .Include(m => m.Album)
                .Include(m => m.TrackArtists)
                    .ThenInclude(ta => ta.Artist)
                .OrderBy(m => m.Title)
                .ToListAsync();
        }
    }
} 
