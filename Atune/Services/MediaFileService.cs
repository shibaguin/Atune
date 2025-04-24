using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
#if ANDROID
using Android.Net;
using Android.App;
using Android.Provider;
using Android.Util;
#endif
using Microsoft.Extensions.Caching.Memory;

namespace Atune.Services
{
    // Сервис для выполнения операций с файлами, таких как копирование, проверка существования и конвертация пути.
    // Service for performing file operations such as copying, checking existence, and path conversion.
    public class MediaFileService(IMemoryCache cache)
    {
        // Поле для кеша
        private readonly IMemoryCache _cache = cache;

#if ANDROID
        public async Task<string> GetRealPathAsync(IStorageFile file)
        {
            if (file == null)
                return string.Empty;

            string cacheKey = "MediaFileService_RealPath_" + file.Path.LocalPath;
            if (_cache.TryGetValue(cacheKey, out string cachedRealPath) && !string.IsNullOrEmpty(cachedRealPath))
            {
                return cachedRealPath;
            }
            
            string realPath = await Task.FromResult(file.Path.LocalPath);
            _cache.Set(cacheKey, realPath, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
            return realPath;
        }
#else
        public static Task<string> GetRealPathAsync(IStorageFile file)
        {
            return Task.FromResult(file.Path.LocalPath);
        }
#endif

        public Task<bool> FileExistsAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Task.FromResult(false);

            string cacheKey = "MediaFileService_FileExists_" + path;
            if (_cache.TryGetValue(cacheKey, out bool exists))
            {
                return Task.FromResult(exists);
            }

            bool fileExists = File.Exists(path);
            _cache.Set(cacheKey, fileExists, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
            return Task.FromResult(fileExists);
        }

#if ANDROID
        public async Task<string> ConvertFileUriToContentUriAsync(string filePath)
        {
            try
            {
                var mediaFile = new Java.IO.File(filePath);
                var uri = Android.Net.Uri.FromFile(mediaFile);
                return uri.ToString();
            }
            catch
            {
                return filePath;
            }
        }

        public async Task<string> GetAndroidRealPathAsync(IStorageFile file)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var uri = Android.Net.Uri.Parse(file.Path.AbsoluteUri);
                    using var cursor = Android.App.Application.Context.ContentResolver.Query(uri, null, null, null, null);
                    cursor?.MoveToFirst();
                    var index = cursor?.GetColumnIndex(MediaStore.MediaColumns.Data);
                    return cursor?.GetString(index ?? 0) ?? file.Path.LocalPath;
                }
                catch
                {
                    return file.Path.LocalPath;
                }
            });
        }
#endif
    }
}
