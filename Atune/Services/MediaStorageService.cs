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

namespace Atune.Services
{
    public class MediaStorageService : IMediaStorageService
    {
        private readonly AppDbContext _context;
        private readonly ILoggerService _logger;
#if ANDROID
        private readonly Context _androidContext; // Переименовано для ясности
#endif

        public MediaStorageService(AppDbContext context, ILoggerService logger
#if ANDROID
            , Context androidContext // Внедрение Context для Android
#endif
        )
        {
            _context = context;
            _logger = logger;
#if ANDROID
            this._androidContext = androidContext; // Инициализация контекста
#endif
        }

        public async Task<string> GetMediaPathAsync(string mediaId)
        {
            // Проверяем, является ли это Android
#if ANDROID
            if (Android.App.Application.Context.CheckSelfPermission(Android.Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
            {
                _logger.LogWarning("ReadExternalStorage permission is not granted.");
                return string.Empty; // Или выбросьте исключение, если это более уместно
            }

            var mediaItem = await _context.MediaItems.FindAsync(mediaId);
            if (mediaItem == null)
            {
                _logger.LogWarning($"Media item with ID {mediaId} not found.");
                return string.Empty; // Или выбросьте исключение, если это более уместно
            }

            // Получаем путь к медиафайлу через ContentResolver
            var uri = MediaStore.Audio.Media.ExternalContentUri;
            var projection = new[] { MediaStore.Audio.Media.InterfaceConsts.Data };
            using (var cursor = _androidContext.ContentResolver.Query(uri, projection, 
                $"{MediaStore.Audio.Media.InterfaceConsts.Id} = ?", new[] { mediaId }, null))
            {
                if (cursor != null && cursor.MoveToFirst())
                {
                    return cursor.GetString(cursor.GetColumnIndexOrThrow(MediaStore.Audio.Media.InterfaceConsts.Data));
                }
                else
                {
                    _logger.LogWarning($"Media file with ID {mediaId} not found in ContentResolver.");
                    return string.Empty; // Или выбросьте исключение, если это более уместно
                }
            }
#else
            var mediaItem = await _context.MediaItems.FindAsync(mediaId);
            if (mediaItem == null)
            {
                _logger.LogWarning($"Media item with ID {mediaId} not found.");
                return string.Empty; // Или выбросьте исключение, если это более уместно
            }
            return mediaItem.Path;
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