using System;
using Atune.Data;
using Atune.Data.Interfaces;
using Atune.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if ANDROID
using Xamarin.Essentials;
using Android.Content;
using Android.Provider;
using Android.App;
using Android.Content.PM;
#endif
using Microsoft.Extensions.Caching.Memory;

namespace Atune.Services
{
    public class MediaStorageService : IMediaStorageService
    {
        private readonly AppDbContext _context;
        private readonly ILoggerService _logger;
        private readonly IMemoryCache _cache;
#if ANDROID
        private readonly Context _androidContext; // Переименовано для ясности
#endif

        public MediaStorageService(AppDbContext context, ILoggerService logger, IMemoryCache cache
#if ANDROID
            , Context androidContext // Внедрение Context для Android
#endif
        )
        {
            _context = context;
            _logger = logger;
            _cache = cache;
#if ANDROID
            this._androidContext = androidContext; // Инициализация контекста
#endif
        }

        public async Task<string> GetMediaPathAsync(string mediaId)
        {
            if (string.IsNullOrWhiteSpace(mediaId))
                throw new ArgumentException("Media ID не может быть null, пустым или состоять только из пробелов.", nameof(mediaId));

            mediaId = mediaId.Trim();

            // Предварительная попытка преобразования mediaId в число для валидации ожидаемого формата
            if (!int.TryParse(mediaId, out int id))
                throw new ArgumentException($"Недопустимый формат media ID: {mediaId}. Ожидается целочисленное значение.", nameof(mediaId));

            string cacheKey = "MediaStorageService_GetMediaPath_" + mediaId;
            if (_cache.TryGetValue(cacheKey, out string? cachedPath) && !string.IsNullOrEmpty(cachedPath))
            {
                return cachedPath;
            }

#if ANDROID
            // Проверяем разрешения для чтения внешнего хранилища
            if (Android.App.Application.Context.CheckSelfPermission(Android.Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
            {
                _logger.LogWarning("ReadExternalStorage permission is not granted.");
                return string.Empty;
            }

            var mediaItem = await _context.MediaItems.FindAsync(id);
            if (mediaItem == null)
            {
                _logger.LogWarning($"Media item with ID {mediaId} not found.");
                return string.Empty;
            }
            
            // Создаем корректный Content URI для доступа к медиафайлу
            Android.Net.Uri contentUri = ContentUris.WithAppendedId(MediaStore.Audio.Media.ExternalContentUri, id);

            // Проверяем доступность файла путем попытки открытия потока
            try
            {
                using (var stream = _androidContext.ContentResolver.OpenInputStream(contentUri))
                {
                    if (stream != null)
                    {
                        cachedPath = contentUri.ToString();
                        _cache.Set(cacheKey, cachedPath, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
                        return cachedPath;
                    }
                    else
                    {
                        _logger.LogWarning($"Unable to open stream for media file with ID {mediaId}.");
                        return string.Empty;
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning($"Error accessing media file with ID {mediaId}: {ex.Message}");
                return string.Empty;
            }
#endif
#if !ANDROID
            var mediaItem = await _context.MediaItems.FindAsync(id);
            if (mediaItem == null)
            {
                _logger.LogWarning($"Media item with ID {mediaId} not found.");
                return string.Empty;
            }
            cachedPath = mediaItem.Path;
            _cache.Set(cacheKey, cachedPath, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
            return cachedPath;
#endif
        }

        public async Task<List<MediaItem>> GetAllMediaItemsAsync()
        {
#if ANDROID
            var mediaItems = new List<MediaItem>();
            var uri = MediaStore.Audio.Media.ExternalContentUri; // URI для медиатеки
            var projection = new[] { 
                MediaStore.Audio.Media.InterfaceConsts.Id, 
                MediaStore.Audio.Media.InterfaceConsts.Data, 
                MediaStore.Audio.Media.InterfaceConsts.Title,
                MediaStore.Audio.Media.InterfaceConsts.Artist // Добавляем колонку Artist
            };

            using (var cursor = _androidContext.ContentResolver.Query(uri, projection, null, null, null))
            {
                if (cursor != null && cursor.MoveToFirst())
                {
                    do
                    {
                        var id = cursor.GetLong(cursor.GetColumnIndexOrThrow(MediaStore.Audio.Media.InterfaceConsts.Id));
                        var path = cursor.GetString(cursor.GetColumnIndexOrThrow(MediaStore.Audio.Media.InterfaceConsts.Data));
                        var title = cursor.GetString(cursor.GetColumnIndexOrThrow(MediaStore.Audio.Media.InterfaceConsts.Title));
                        var artist = cursor.GetString(cursor.GetColumnIndexOrThrow(MediaStore.Audio.Media.InterfaceConsts.Artist)); // Получаем имя артиста

                        mediaItems.Add(new MediaItem
                        {
                            Id = (int)id, // Приведение к int, если необходимо
                            Path = path,
                            Title = title,
                            Album = new Album { Title = "Unknown Album" }, // Устанавливаем альбом по умолчанию
                            Year = 0, // Устанавливаем год по умолчанию
                            Genre = "Unknown Genre", // Устанавливаем жанр по умолчанию
                            TrackArtists = new List<TrackArtist> {
                                new TrackArtist { 
                                    Artist = new Artist { Name = artist ?? "Unknown Artist" } // Устанавливаем имя артиста
                                }
                            }
                        });
                    } while (cursor.MoveToNext());
                }
                else
                {
                    _logger.LogWarning("Cursor is null or empty.");
                }
            }
            return mediaItems;
#else
            return await _context.MediaItems.ToListAsync();
#endif
        }

        public async Task<bool> MediaExistsAsync(string mediaId)
        {
#if ANDROID
            var uri = MediaStore.Audio.Media.ExternalContentUri;
            var projection = new[] { MediaStore.Audio.Media.InterfaceConsts.Id };
            using (var cursor = _androidContext.ContentResolver.Query(uri, projection, 
                $"{MediaStore.Audio.Media.InterfaceConsts.Id} = ?", new[] { mediaId }, null))
            {
                return cursor != null && cursor.MoveToFirst(); // Если курсор не null и содержит данные, файл существует
            }
#else
            return await _context.MediaItems.AnyAsync(m => m.Id.ToString() == mediaId);
#endif
        }
    }
} 