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

namespace Atune.Views;

public partial class MediaView : UserControl
{
    private readonly AppDbContext? _dbContext;

    public MediaView()
    {
        InitializeComponent();
    }
    
    public MediaView(MediaViewModel vm, AppDbContext dbContext) : this()
    {
        DataContext = vm;
        _dbContext = dbContext;
        Console.WriteLine($"[MediaView] DbContext инициализирован: {_dbContext?.Database?.GetDbConnection()?.DataSource}");
    }

    private async void AddToLibrary_Click(object sender, RoutedEventArgs e)
    {
        const string logHeader = "[MediaView]";
        Console.WriteLine($"{logHeader} Кнопка нажата!");
        
        if (_dbContext == null)
        {
            Console.WriteLine($"{logHeader} Ошибка: DbContext не инициализирован");
            return;
        }

        try
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider == null)
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
                        Patterns = new[] { "*.mp3", "*.wav", "*.flac", "*.ogg", "*.aac" } 
                    },
                    new FilePickerFileType("Все файлы") { Patterns = new[] { "*" } }
                }
            });

            if (files.Count == 0)
            {
                Console.WriteLine($"{logHeader} Файлы не выбраны");
                return;
            }

            Console.WriteLine($"{logHeader} Выбрано файлов: {(files != null ? files.Count : 0)}");
            
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var importedDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Atune_Imported");
                    
                    Directory.CreateDirectory(importedDir);
                    Console.WriteLine($"{logHeader} Целевая директория: {importedDir}");

                    var successCount = 0;
                    var errorCount = 0;

                    var canConnect = await _dbContext.Database.CanConnectAsync();
                    Console.WriteLine($"{logHeader} Подключение к БД: {canConnect}");

                    var tables = await _dbContext.Database.SqlQueryRaw<string>(
                        "SELECT name FROM sqlite_master WHERE type='table'").ToListAsync();
                    Console.WriteLine($"{logHeader} Таблицы в БД: {string.Join(", ", tables)}");

                    foreach (var file in files ?? Enumerable.Empty<IStorageFile>())
                    {
                        if (file?.Path?.LocalPath == null)
                        {
                            Console.WriteLine($"{logHeader} Неверный путь к файлу");
                            errorCount++;
                            continue;
                        }
                        
                        var sourcePath = file.Path.LocalPath;
                        if (sourcePath.StartsWith("~/"))
                        {
                            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            sourcePath = sourcePath.Replace("~", homeDir).Replace("/", Path.DirectorySeparatorChar.ToString());
                            Console.WriteLine($"Преобразованный путь: {sourcePath}");
                        }

                        if (!System.IO.File.Exists(sourcePath))
                        {
                            Console.WriteLine($"{logHeader} Файл не существует: {sourcePath}");
                            errorCount++;
                            continue;
                        }

                        Console.WriteLine($"{logHeader} Обработка файла: {sourcePath}");

                        try
                        {
                            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourcePath);
                            var duration = TimeSpan.Zero; // По умолчанию

                        var mediaItem = new MediaItem(
                                fileNameWithoutExtension, 
                                "N/A", // Заглушка для артиста
                                sourcePath, 
                                duration);

                            var validationResults = new List<ValidationResult>();
                            if (!Validator.TryValidateObject(mediaItem, 
                                new ValidationContext(mediaItem), 
                                validationResults, 
                                true))
                            {
                                Console.WriteLine($"{logHeader} Ошибка валидации:");
                                foreach (var error in validationResults)
                                {
                                    Console.WriteLine($"- {error.ErrorMessage}");
                                }
                                errorCount++;
                                continue;
                            }

                            // Логирование
                            Console.WriteLine($"{logHeader} Добавляем:");
                            Console.WriteLine($"- Название: {mediaItem.Title}");
                            Console.WriteLine($"- Путь: {mediaItem.Path}");

                            // Добавление в БД
                            await _dbContext.MediaItems.AddAsync(mediaItem);
                            await _dbContext.SaveChangesAsync();
                            
                            successCount++;

                            Console.WriteLine($"{logHeader} Успешно: {mediaItem}");
                            
                            // Для Linux используем альтернативную проверку прав
                            Console.WriteLine($"Права доступа: {(Environment.OSVersion.Platform == PlatformID.Win32NT ? "Windows" : "Unix")}");

                            // После добавления
                            Console.WriteLine($"Состояние сущности: {_dbContext.Entry(mediaItem).State}");
                            if (mediaItem.Id > 0)
                            {
                                Console.WriteLine("Запись успешно добавлена с ID: " + mediaItem.Id);
                            }
                            else
                            {
                                Console.WriteLine("ОШИБКА: ID не назначен");
                            }

                            // Обновляем коллекцию в ViewModel
                            if (DataContext is MediaViewModel vm)
                            {
                                vm.MediaItems = new ObservableCollection<MediaItem>(await _dbContext.MediaItems.ToListAsync());
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            Console.WriteLine($"{logHeader} Ошибка обработки файла {sourcePath}:");
                            Console.WriteLine(ex);
                        }
                    }

                    // Добавляем проверку перед сохранением
                    if (successCount > 0)
                    {
                        await transaction.CommitAsync();
                        Console.WriteLine($"{logHeader} Успешно сохранено: {successCount} записей");
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"{logHeader} Нет данных для сохранения");
                    }

                    // Добавляем принудическое обновление контекста
                    _dbContext.ChangeTracker.Clear();
                    var totalCount = await _dbContext.MediaItems.CountAsync();
                    Console.WriteLine($"{logHeader} Всего записей в БД: {totalCount}");

                    // Валидация
                    await ValidateDatabaseRecords();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"{logHeader} Критическая ошибка транзакции:");
                    Console.WriteLine(ex);
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{logHeader} Необработанная ошибка:");
            Console.WriteLine(ex);
        }
    }

    private async Task ValidateDatabaseRecords()
    {
        if (_dbContext == null)
        {
            Console.WriteLine("[MediaView] Контекст БД недоступен");
            return;
        }

        Console.WriteLine($"[MediaView] Валидация записей в БД...");
        
        var records = await _dbContext.MediaItems
            .OrderByDescending(x => x.Id)
            .Take(5)
            .ToListAsync();

        Console.WriteLine($"Последние {records.Count} записей:");
        foreach (var item in records ?? Enumerable.Empty<MediaItem>())
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
}