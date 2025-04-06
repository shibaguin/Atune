using Atune.Data;
using Atune.Data.Interfaces;
using Atune.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if ANDROID
using Android.Content;
using Android.Provider;
#endif

namespace Atune.Services
{
    public class MediaStorageService : IMediaStorageService
    {
        private readonly AppDbContext _context;
        private readonly ILoggerService _logger;
#if ANDROID
        private readonly Context _context; // Добавлено для доступа к ContentResolver
#endif

        public MediaStorageService(AppDbContext context, ILoggerService logger
#if ANDROID
            , Context context // Внедрение Context для Android
#endif
        )
        {
            _context = context;
            _logger = logger;
#if ANDROID
            this._context = context; // Инициализация контекста
#endif
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
#if ANDROID
            var mediaItems = new List<MediaItem>();
            var uri = MediaStore.Audio.Media.ExternalContentUri; // URI для медиатеки
            var projection = new[] { MediaStore.Audio.Media.InterfaceConsts.Id, MediaStore.Audio.Media.InterfaceConsts.Data, MediaStore.Audio.Media.InterfaceConsts.Title };

            using (var cursor = _context.ContentResolver.Query(uri, projection, null, null, null))
            {
                if (cursor != null)
                {
                    while (cursor.MoveToNext())
                    {
                        var id = cursor.GetLong(cursor.GetColumnIndexOrThrow(MediaStore.Audio.Media.InterfaceConsts.Id));
                        var path = cursor.GetString(cursor.GetColumnIndexOrThrow(MediaStore.Audio.Media.InterfaceConsts.Data));
                        var title = cursor.GetString(cursor.GetColumnIndexOrThrow(MediaStore.Audio.Media.InterfaceConsts.Title));

                        mediaItems.Add(new MediaItem
                        {
                            Id = (int)id, // Приведение к int, если необходимо
                            Path = path,
                            Title = title
                        });
                    }
                }
            }
            return mediaItems;
#else
            return await _context.MediaItems.ToListAsync();
#endif
        }

        public async Task<bool> MediaExistsAsync(string mediaId)
        {
            return await _context.MediaItems.AnyAsync(m => m.Id.ToString() == mediaId);
        }
    }
} 