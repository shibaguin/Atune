using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System;
using Atune.Models;
using Avalonia;
using TagLib;
using Avalonia.Markup.Xaml;
using Atune.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if ANDROID
using Android.Net;
using Android.App;
using Android.Provider;
#endif

namespace Atune.Views;

public partial class MediaView : UserControl
{
    private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;

    public MediaView()
    {
        InitializeComponent();
    }
    
    public MediaView(MediaViewModel vm, IDbContextFactory<AppDbContext> dbContextFactory) : this()
    {
        DataContext = vm;
        _dbContextFactory = dbContextFactory;
    }

    private async void AddToLibrary_Click(object sender, RoutedEventArgs e)
    {
        const string logHeader = "[MediaView]";
        Console.WriteLine($"{logHeader} Кнопка нажата!");
        
        var dbContext = _dbContextFactory?.CreateDbContext();
        if (dbContext is null)
        {
            Console.WriteLine($"{logHeader} Ошибка: Не удалось создать контекст БД");
            return;
        }
        using (dbContext)
        {
            try
            {
                // Добавляем проверку доступности БД
                if (!await dbContext.Database.CanConnectAsync())
                {
                    Console.WriteLine($"{logHeader} Нет подключения к БД");
                    return;
                }

                var topLevel = TopLevel.GetTopLevel(this);
                var storageProvider = topLevel?.StorageProvider;
                
                if (storageProvider is null)
                {
                    Console.WriteLine($"{logHeader} StorageProvider недоступен");
                    return;
                }

                var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Выберите аудиофайлы",
                    AllowMultiple = true,
                    FileTypeFilter = new[] 
                    {
                        new FilePickerFileType("Audio files")
                        {
                            Patterns = new[] { "*.mp3", "*.flac", "*.wav" },
                            MimeTypes = new[] { "audio/*" }
                        }
                    }
                });

                int errorCount = 0;
                int successCount = 0;
                int duplicateCount = 0;

                foreach (var file in files ?? Enumerable.Empty<IStorageFile>())
                {
                    string? realPath = null;
                    
                    try
                    {
                        realPath = file.Path.LocalPath;
                        
                        // Добавляем проверку на null для фабрики
                        if (_dbContextFactory == null)
                        {
                            Console.WriteLine($"{logHeader} Ошибка: Фабрика контекста БД не инициализирована");
                            errorCount++;
                            continue;
                        }
                        
                        // Проверка на существование в БД
                        using (var checkDb = _dbContextFactory.CreateDbContext())
                        {
                            if (await checkDb.ExistsByPathAsync(realPath))
                            {
                                Console.WriteLine($"{logHeader} Файл уже существует: {realPath}");
                                duplicateCount++;
                                continue;
                            }
                        }
                        
                        // Для десктопных систем проверяем существование файла
                        if (!OperatingSystem.IsAndroid())
                        {
                            if (!System.IO.File.Exists(realPath))
                            {
                                Console.WriteLine($"{logHeader} Файл не существует: {realPath}");
                                errorCount++;
                                continue;
                            }
                        }
                        else 
                        {
                            // Оригинальная обработка для Android
                            #if ANDROID
                            if (file.Path.Scheme != "content")
                            {
                                realPath = await ConvertFileUriToContentUri(file.Path.LocalPath);
                            }
                            
                            if (realPath.StartsWith("content://"))
                            {
                                realPath = await GetAndroidRealPath(file);
                            }

                            var fileExists = await FileExists(realPath);
                            if (!fileExists)
                            {
                                Console.WriteLine($"{logHeader} Файл не существует: {realPath}");
                                errorCount++;
                                continue;
                            }
                            #endif
                        }

                        Console.WriteLine($"{logHeader} Обработка файла: {realPath}");

                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(realPath);
                        var duration = TimeSpan.Zero;
                        string artist = "Unknown Artist";
                        string album = "Unknown Album";
                        int year = 0;
                        string genre = "Unknown Genre";

    #if ANDROID
                        duration = await GetDuration(realPath);
                        var (a, al, y, g) = await GetAndroidTagInfo(realPath);
                        artist = a;
                        album = al;
                        year = y;
                        genre = g;
    #endif

                        var mediaItem = new MediaItem(
                            fileNameWithoutExtension,
                            artist,
                            album,
                            year,
                            genre,
                            realPath, 
                            duration);

                        var validationResults = new List<ValidationResult>();
                        if (!Validator.TryValidateObject(mediaItem, new ValidationContext(mediaItem), validationResults))
                        {
                            Console.WriteLine($"{logHeader} Ошибки валидации для файла {realPath}:");
                            foreach (var error in validationResults)
                            {
                                Console.WriteLine($"- {error.ErrorMessage}");
                            }
                            errorCount++;
                            continue;
                        }

                        using (var transaction = await dbContext.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                await dbContext.MediaItems.AddAsync(mediaItem);
                                await dbContext.SaveChangesAsync();
                                await transaction.CommitAsync();
                                successCount++;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"{logHeader} Ошибка транзакции: {ex}");
                                await transaction.RollbackAsync();
                                errorCount++;
                            }
                        }
                    }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint") == true)
                    {
                        Console.WriteLine($"{logHeader} Файл уже существует: {realPath ?? "unknown_path"}");
                        duplicateCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{logHeader} Общая ошибка: {ex}");
                        errorCount++;
                    }
                }

                if (successCount > 0)
                {
                    Console.WriteLine($"{logHeader} Успешно добавлено {successCount} файлов");
                    
                    if (DataContext is MediaViewModel vm)
                    {
                        await vm.RefreshMediaCommand.ExecuteAsync(null);
                    }
                }

                Console.WriteLine($"{logHeader} Обработка завершена. Успешно: {successCount}, Ошибок: {errorCount}, Дубликатов: {duplicateCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{logHeader} Критическая ошибка:");
                Console.WriteLine(ex);
            }
        }
    }

    private async Task ValidateDatabaseRecords()
    {
        if (_dbContextFactory is null)
        {
            Console.WriteLine("[MediaView] Ошибка: DbContextFactory не инициализирована");
            return;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        if (dbContext is null)
        {
            Console.WriteLine("[MediaView] Ошибка создания контекста БД");
            return;
        }

        Console.WriteLine($"[MediaView] Валидация записей в БД...");
        
        var records = await dbContext.MediaItems
            .OrderByDescending(x => x.Id)
            .Take(5)
            .ToListAsync();

        Console.WriteLine($"Последние {records.Count} записей:");
        foreach (var item in records)
        {
            Console.WriteLine($"- {item.Id}: {item.Title} | {item.Artist}");
            Console.WriteLine($"  Path: {item.Path} (exists: {System.IO.File.Exists(item.Path ?? "")})");
        }
    }

    private async void TestDbConnection_Click(object sender, RoutedEventArgs e)
    {
        const string logHeader = "[DB TEST]";
        Console.WriteLine($"{logHeader} Начало теста БД");
        
        try
        {
            using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=media_library.db")
                .Options);
            
            // Простая транзакция
            await db.Database.EnsureCreatedAsync();
            
            // Добавление тестовой записи
            var testItem = new MediaItem(
                "Test Title", 
                "Test Artist", 
                "Test Album", 
                2024, 
                "Test Genre",
                "/test/path.mp3", 
                TimeSpan.FromSeconds(123));
            
            db.MediaItems.Add(testItem);
            var result = await db.SaveChangesAsync();
            
            Console.WriteLine($"{logHeader} Добавлено записей: {result}");
            Console.WriteLine($"{logHeader} ID новой записи: {testItem.Id}");

            // Чтение записи
            var item = await db.MediaItems.FindAsync(testItem.Id);
            Console.WriteLine($"{logHeader} Прочитано: {item?.Title ?? "NULL"}");

            // Удаление записи
            if (item != null)
            {
                db.MediaItems.Remove(item);
                await db.SaveChangesAsync();
                Console.WriteLine($"{logHeader} Запись удалена");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{logHeader} ОШИБКА:");
            Console.WriteLine(ex);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Переносим методы внутрь класса
#if ANDROID
    private async Task<string> GetAndroidRealPath(IStorageFile file)
    {
        try 
        {
            var uri = Android.Net.Uri.Parse(file.Path.AbsoluteUri);
            var cursor = Android.App.Application.Context.ContentResolver.Query(
                uri, null, null, null, null);
            cursor?.MoveToFirst();
            var index = cursor?.GetColumnIndex(MediaStore.MediaColumns.Data);
            return cursor?.GetString(index ?? 0) ?? file.Path.LocalPath;
        }
        catch 
        {
            return file.Path.LocalPath;
        }
    }

    private async Task<(string Artist, string Album, int Year, string Genre)> GetAndroidTagInfo(string path)
    {
        try 
        {
            var uri = Android.Net.Uri.Parse(path);
            var projection = new[] { 
                MediaStore.Audio.AudioColumns.Artist,
                MediaStore.Audio.AudioColumns.Album,
                MediaStore.Audio.AudioColumns.Years,
                MediaStore.Audio.AudioColumns.Genre
            };
            
            using var cursor = await Task.Run(() => 
                Android.App.Application.Context.ContentResolver.Query(
                    uri, projection, null, null, null));
            
            if (cursor?.MoveToFirst() == true)
            {
                return (
                    cursor.GetString(0) ?? "Unknown Artist",
                    cursor.GetString(1) ?? "Unknown Album",
                    cursor.GetInt(2),
                    cursor.GetString(3) ?? "Unknown Genre"
                );
            }
        }
        catch 
        {
            // Логирование ошибок
        }
        return ("Unknown Artist", "Unknown Album", 0, "Unknown Genre");
    }

    private async Task<TimeSpan> GetDuration(string path)
    {
        try 
        {
            var uri = Android.Net.Uri.Parse(path);
            var projection = new[] { MediaStore.Audio.AudioColumns.Duration };
            using var cursor = await Task.Run(() => 
                Android.App.Application.Context.ContentResolver.Query(
                    uri, projection, null, null, null));
            
            cursor?.MoveToFirst();
            return TimeSpan.FromMilliseconds(cursor?.GetLong(0) ?? 0);
        }
        catch 
        {
            return TimeSpan.Zero;
        }
    }

    private async Task<string> ConvertFileUriToContentUri(string filePath)
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
#endif

    [Obsolete("Метод будет удален в будущих версиях")]
    private Task<bool> FileExists(string path)
    {
#if ANDROID
        if (path.StartsWith("content://"))
        {
            try 
            {
                var uri = Android.Net.Uri.Parse(path);
                var contentResolver = Android.App.Application.Context.ContentResolver;
                using var stream = contentResolver.OpenInputStream(uri);
                return Task.FromResult(stream != null);
            }
            catch 
            {
                return Task.FromResult(false);
            }
        }
#endif
        return Task.FromResult(System.IO.File.Exists(path));
    }

    private void ShowDbPath_Click(object sender, RoutedEventArgs e)
    {
        // Используем фабрику для создания контекста
        if (_dbContextFactory is null)
        {
            Console.WriteLine("DbContextFactory не доступна");
            return;
        }

        using var db = _dbContextFactory.CreateDbContext();
        var path = db?.Database.GetDbConnection().DataSource ?? "не определен";
        
        Console.WriteLine($"Текущий путь к БД: {path}");
        
        var vm = DataContext as MediaViewModel;
        vm?.UpdateStatusMessage?.Invoke($"Путь к БД: {path}");
    }

    private (string Artist, string Album, int Year, string Genre) GetDesktopTagInfo(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path);
            return (
                file.Tag.FirstPerformer ?? file.Tag.AlbumArtists.FirstOrDefault() ?? "Unknown Artist",
                file.Tag.Album ?? "Unknown Album",
                (int)(file.Tag.Year > 0 ? file.Tag.Year : 0),
                file.Tag.FirstGenre ?? "Unknown Genre"
            );
        }
        catch
        {
            return ("Unknown Artist", "Unknown Album", 0, "Unknown Genre");
        }
    }
}