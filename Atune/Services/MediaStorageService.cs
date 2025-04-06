using Atune.Data;
using Atune.Data.Interfaces;
using Atune.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atune.Services
{
    public class MediaStorageService : IMediaStorageService
    {
        private readonly AppDbContext _context;
        private readonly ILoggerService _logger;

        public MediaStorageService(AppDbContext context, ILoggerService logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GetMediaPathAsync(string mediaId)
        {
            var mediaItem = await _context.MediaItems.FindAsync(mediaId);
            if (mediaItem == null)
            {
                _logger.LogWarning($"Media item with ID {mediaId} not found.");
                return string.Empty; // Или выбросьте исключение, если это более уместно
            }
            return mediaItem.Path;
        }

        public async Task<List<MediaItem>> GetAllMediaItemsAsync()
        {
            return await _context.MediaItems.ToListAsync();
        }

        public async Task<bool> MediaExistsAsync(string mediaId)
        {
            return await _context.MediaItems.AnyAsync(m => m.Id.ToString() == mediaId);
        }
    }
} 