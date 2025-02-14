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

        public async Task BulkInsertAsync(IEnumerable<MediaItem> items)
        {
            // Уменьшаем размер пакета для Android
            var batchSize = OperatingSystem.IsAndroid() ? 20 : 100;
            
            var batches = items
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / batchSize)
                .Select(g => g.Select(x => x.Value).ToList());

            foreach (var batch in batches)
            {
                await _context.MediaItems.AddRangeAsync(batch);
                await _context.SaveChangesAsync(); 
                
                // Для Android принудительно освобождаем память
                if (OperatingSystem.IsAndroid())
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }
    }
} 