using Atune.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Atune.Data.Interfaces
{
    public interface IMediaRepository : IRepository<MediaItem>
    {
        Task<IEnumerable<MediaItem>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsByPathAsync(string path);
        Task BulkInsertAsync(IEnumerable<MediaItem> items);
    }
} 