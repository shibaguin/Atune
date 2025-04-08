using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Atune.Models;
using Atune.Data;
using Atune.Services;
using Microsoft.Extensions.Logging;
using System;
using Atune.Exceptions;

namespace Atune.Services
{
    // Сервис для работы с БД медиа-записей, инкапсулирующий логику обращения к AppDbContext.
    // Service for working with the media database, encapsulating the logic of accessing AppDbContext.  
    public class MediaDatabaseService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILoggerService _logger;

        public MediaDatabaseService(IDbContextFactory<AppDbContext> dbContextFactory, ILoggerService logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
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
                await dbContext.AddMediaAsync(item);
                _logger.LogInformation($"Added media item: {item.Title} by {item.TrackArtists.FirstOrDefault()?.Artist.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding media item: {Message}", ex);
            }
        }

        public async Task ValidateDatabaseRecordsAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var invalidRecords = await dbContext.MediaItems
                .Where(m => string.IsNullOrEmpty(m.Path) || !File.Exists(m.Path))
                .ToListAsync();
            _logger.LogInformation($"Found {invalidRecords.Count} invalid records in the database");
            foreach (var record in invalidRecords)
            {
                dbContext.MediaItems.Remove(record);
                _logger.LogInformation($"Removed invalid record: {record.Title} by {record.TrackArtists.FirstOrDefault()?.Artist.Name}");
            }
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Database records validated and changes saved");
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
            using var dbContext = _dbContextFactory.CreateDbContext();
            try
            {
                return await dbContext.MediaItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Path == path);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving media item by path '{path}': {ex.Message}");
                throw new InvalidOperationException("Error retrieving media item by path", ex);
            }
        }
    }
} 