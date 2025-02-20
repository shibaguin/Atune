using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Atune.Models;
using Atune.Data;
using Atune.Services;
using Microsoft.Extensions.Logging;

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
            return await dbContext.Database.CanConnectAsync();
        }

        public async Task<bool> ExistsByPathAsync(string path)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.ExistsByPathAsync(path);
        }

        public async Task AddMediaItemAsync(MediaItem item)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            await dbContext.AddMediaAsync(item);
        }

        public async Task ValidateDatabaseRecordsAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var invalidRecords = await dbContext.MediaItems
                .Where(m => string.IsNullOrEmpty(m.Path) || !File.Exists(m.Path))
                .ToListAsync();
            foreach (var record in invalidRecords)
            {
                dbContext.MediaItems.Remove(record);
            }
            await dbContext.SaveChangesAsync();
        }

        public string GetDatabasePath()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.Database.GetDbConnection().DataSource ?? "not defined";
        }
    }
} 