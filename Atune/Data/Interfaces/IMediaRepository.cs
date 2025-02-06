using Atune.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atune.Data.Interfaces
{
    public interface IMediaRepository : IRepository<MediaItem>
    {
        Task<bool> ExistsByPathAsync(string path);
        Task<IEnumerable<MediaItem>> GetAllWithDetailsAsync();
        Task BulkInsertAsync(IEnumerable<MediaItem> items);
    }
} 