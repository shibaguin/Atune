using Atune.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atune.Services.Interfaces
{
    public interface IMediaStorageService
    {
        // Получить путь к медиафайлу по его идентификатору
        Task<string> GetMediaPathAsync(string mediaId);

        // Получить все медиафайлы
        Task<List<MediaItem>> GetAllMediaItemsAsync();

        // Проверить, существует ли медиафайл по его идентификатору
        Task<bool> MediaExistsAsync(string mediaId);
    }
} 