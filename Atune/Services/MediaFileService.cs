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

namespace Atune.Services
{
    // Сервис для выполнения операций с файлами, таких как копирование, проверка существования и конвертация пути.
    // Service for performing file operations such as copying, checking existence, and path conversion.
    public class MediaFileService
    {
        #if ANDROID
        public async Task<string> GetRealPathAsync(IStorageFile file)
        {
            Android.Util.Log.Debug("MediaFileService", $"Start copying file: {file.Name}");
            var destFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AtuneMedia");
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            var destPath = Path.Combine(destFolder, file.Name);
            using (var sourceStream = await file.OpenReadAsync())
            using (var destStream = File.Create(destPath))
            {
                await sourceStream.CopyToAsync(destStream);
            }
            Android.Util.Log.Debug("MediaFileService", $"File copied to: {destPath}");
            return destPath;
        }
        #else
        public Task<string> GetRealPathAsync(IStorageFile file)
        {
            return Task.FromResult(file.Path.LocalPath);
        }
        #endif

        public Task<bool> FileExistsAsync(string path)
        {
            return Task.Run(() => File.Exists(path));
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