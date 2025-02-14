using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Atune.Data.Interfaces;
using Atune.Models;
using System.Threading;

namespace Atune.Data.Repositories
{
    public class MediaRepository : IMediaRepository
    {
        private readonly AppDbContext _context;

        public MediaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MediaItem?> GetByIdAsync(int id)
        {
            return await _context.MediaItems.FindAsync(id);
        }

        public async Task<IEnumerable<MediaItem>> GetAllAsync()
        {
            return await _context.MediaItems.ToListAsync();
        }

        public async Task AddAsync(MediaItem entity)
        {
            await _context.MediaItems.AddAsync(entity);
        }

        public async Task UpdateAsync(MediaItem entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(MediaItem entity)
        {
            _context.MediaItems.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsByPathAsync(string path)
        {
            return await _context.MediaItems.AnyAsync(m => m.Path == path);
        }

        public async Task<IEnumerable<MediaItem>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.MediaItems
                .AsNoTracking()
                .OrderBy(m => m.Title)
                .ToListAsync(cancellationToken);
        }

        public async Task BulkInsertAsync(IEnumerable<MediaItem> items, Action<IEnumerable<MediaItem>>? onBatchProcessed = null)
        {
            // Настройки для разных платформ
            var (batchSize, delayMs) = OperatingSystem.IsAndroid() 
                ? (200, 20)    // Большие батчи с сохранением задержки
                : (500, 50);  // Крупные батчи для десктопа

            var batches = items.Chunk(batchSize);
            
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var batch in batches)
                {
                    await _context.BulkInsertAsync(batch, options => 
                    {
                        options.InsertKeepIdentity = true;
                        options.BatchSize = batchSize;
                    });
                }
                await transaction.CommitAsync();
                onBatchProcessed?.Invoke(items);
                
                if (OperatingSystem.IsAndroid())
                    await Task.Delay(delayMs); // Плавность UI
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }

        public async Task<HashSet<string>> GetExistingPathsAsync(IEnumerable<string> paths)
        {
            return new HashSet<string>(await _context.MediaItems
                .Where(m => paths.Contains(m.Path))
                .Select(m => m.Path)
                .ToListAsync());
        }
    }
} 