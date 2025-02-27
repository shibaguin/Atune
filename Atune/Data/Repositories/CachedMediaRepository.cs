using System;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Atune.Data.Interfaces;
using Atune.Models;

namespace Atune.Data.Repositories
{
    public class CachedMediaRepository : IMediaRepository
    {
        private readonly IMediaRepository _decorated;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "AllMediaCached";

        public CachedMediaRepository(IMediaRepository decorated, IMemoryCache cache)
        {
            _decorated = decorated;
            _cache = cache;
        }

        public async Task<IEnumerable<MediaItem>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            return await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(15))
                     .SetSize(1);
                return await _decorated.GetAllWithDetailsAsync(cancellationToken);
            }) ?? new List<MediaItem>();
        }

        // Other methods transparently delegate
        public async Task<MediaItem?> GetByIdAsync(int id) => await _decorated.GetByIdAsync(id);
        public async Task<IEnumerable<MediaItem>> GetAllAsync() => await _decorated.GetAllAsync();
        public async Task AddAsync(MediaItem entity)
        {
            await _decorated.AddAsync(entity);
            _cache.Remove(CacheKey);
        }
        public async Task UpdateAsync(MediaItem entity)
        {
            await _decorated.UpdateAsync(entity);
            _cache.Remove(CacheKey);
        }
        public async Task DeleteAsync(MediaItem entity)
        {
            await _decorated.DeleteAsync(entity);
            _cache.Remove(CacheKey);
        }
        public async Task<bool> ExistsByPathAsync(string path) => await _decorated.ExistsByPathAsync(path);
        public async Task BulkInsertAsync(IEnumerable<MediaItem> items, Action<IEnumerable<MediaItem>>? onBatchProcessed = null)
        {
            await _decorated.BulkInsertAsync(items, onBatchProcessed);
            _cache.Remove(CacheKey);
        }
        public async Task<HashSet<string>> GetExistingPathsAsync(IEnumerable<string> paths)
        {
            return await _decorated.GetExistingPathsAsync(paths);
        }
    }
} 