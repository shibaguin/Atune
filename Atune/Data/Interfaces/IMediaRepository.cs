using Atune.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System;

namespace Atune.Data.Interfaces
{
    public interface IMediaRepository : IRepository<MediaItem>
    {
        Task<IEnumerable<MediaItem>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsByPathAsync(string path);
        Task BulkInsertAsync(IEnumerable<MediaItem> items, Action<IEnumerable<MediaItem>>? onBatchProcessed = null);
        Task<HashSet<string>> GetExistingPathsAsync(IEnumerable<string> paths);
        Task BulkUpdateAsync(IEnumerable<MediaItem> items, Action<IEnumerable<MediaItem>>? onBatchProcessed = null);
        Task BulkDeleteAsync(IEnumerable<MediaItem> items, Action<IEnumerable<MediaItem>>? onBatchProcessed = null);
        Task<List<MediaItem>> GetAllMediaItemsAsync();
    }
} 