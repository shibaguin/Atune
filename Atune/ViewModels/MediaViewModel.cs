using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Atune.Models;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Linq;
using Atune.Data.Interfaces;
using Avalonia.Threading;

namespace Atune.ViewModels;

public partial class MediaViewModel : ObservableObject
{
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    
    [ObservableProperty]
    private List<MediaItem> _mediaContent = new List<MediaItem>();

    [ObservableProperty]
    private string _statusMessage = "Готово к работе";

    [ObservableProperty]
    private ObservableCollection<MediaItem> _mediaItems = new();

    [ObservableProperty]
    private bool _isBusy;

    public Action<string>? UpdateStatusMessage { get; set; }

    public MediaViewModel(IMemoryCache cache, IUnitOfWork unitOfWork)
    {
        _cache = cache;
        _unitOfWork = unitOfWork;
        LoadMediaContent();
        
        // Добавляем автоматическую загрузку при инициализации
        RefreshMediaCommand.ExecuteAsync(null!);
    }

    private async Task<List<MediaItem>> LoadFromDatabaseAsync()
    {
        var result = await _unitOfWork.Media.GetAllWithDetailsAsync();
        return result.ToList();
    }

    private async void LoadMediaContent()
    {
        if (!_cache.TryGetValue("MediaContent", out List<MediaItem>? content))
        {
            content = await LoadFromDatabaseAsync();
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSize(content.Count * 500 + 1024)
                .SetPriority(CacheItemPriority.Normal)
                .SetSlidingExpiration(TimeSpan.FromMinutes(15));
            
            // Для Android уменьшаем время кэширования
            if (OperatingSystem.IsAndroid())
            {
                cacheOptions.SetSlidingExpiration(TimeSpan.FromMinutes(5));
            }
            
            _cache.Set("MediaContent", content, cacheOptions);
        }
        MediaContent = content ?? new List<MediaItem>();
    }

    private List<MediaItem> LoadFromDataSource()
    {
        // Загрузка данных из внешнего источника
        return new List<MediaItem>();
    }

    [RelayCommand]
    private async Task AddMediaAsync()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var mainWindow = desktop.MainWindow;
            if (mainWindow == null) return;

            var topLevel = TopLevel.GetTopLevel(mainWindow);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Выберите аудиофайлы",
                    AllowMultiple = true,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Аудио файлы")
                        {
                            Patterns = new[] { "*.mp3", "*.flac" },
                            MimeTypes = new[] { "audio/mpeg", "audio/flac" }
                        }
                    }
                });

            int duplicateCount = 0;
            int successCount = 0;
            int errorCount = 0;

            var newItems = new List<MediaItem>(); // Коллекция для bulk-вставки

            foreach (var file in files ?? Enumerable.Empty<IStorageFile>())
            {
                try
                {
                    var realPath = file.Path.LocalPath;
                    
                    if (await _unitOfWork.Media.ExistsByPathAsync(realPath))
                    {
                        duplicateCount++;
                        continue;
                    }

                    var mediaItem = new MediaItem(
                        Path.GetFileNameWithoutExtension(file.Name),
                        "Unknown Artist",
                        "Unknown Album",
                        0,
                        "Unknown Genre",
                        realPath,
                        TimeSpan.Zero);

                    newItems.Add(mediaItem); // Добавляем в коллекцию для массовой вставки
                    successCount++;
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint") == true)
                {
                    duplicateCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }

            try
            {
                if (newItems.Count > 0)
                {
                    await _unitOfWork.Media.BulkInsertAsync(newItems);
                    await _unitOfWork.CommitAsync();
                }

                // Обновление статуса
                var statusParts = new List<string>();
                if (successCount > 0) statusParts.Add($"Добавлено: {successCount}");
                if (duplicateCount > 0) statusParts.Add($"Пропущено дубликатов: {duplicateCount}");
                if (errorCount > 0) statusParts.Add($"Ошибок: {errorCount}");
                
                StatusMessage = string.Join(" • ", statusParts);
                
                if (successCount > 0)
                {
                    // Для Android принудительно обновляем UI
                    if (OperatingSystem.IsAndroid())
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () => 
                        {
                            await RefreshMediaCommand.ExecuteAsync(null);
                        });
                    }
                    else
                    {
                        await RefreshMediaCommand.ExecuteAsync(null);
                    }
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint") == true)
            {
                StatusMessage = "Обнаружены дубликаты при массовой вставке!";
            }
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint") == true)
        {
            StatusMessage = "Файл уже существует в библиотеке!";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshMedia()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Загрузка данных...";
            
            var items = await _unitOfWork.Media.GetAllWithDetailsAsync();
            
            // Универсальный способ обновления для всех платформ
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MediaItems.Clear();
                foreach (var item in items)
                {
                    MediaItems.Add(item);
                }
                // Форсируем обновление UI
                OnPropertyChanged(nameof(MediaItems));
            });
            
            StatusMessage = $"Загружено {items.Count()} записей";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DropMediaRecords()
    {
        try
        {
            var allMedia = await _unitOfWork.Media.GetAllAsync();
            foreach (var media in allMedia)
            {
                await _unitOfWork.Media.DeleteAsync(media);
            }
            await _unitOfWork.CommitAsync();
            StatusMessage = "Все записи удалены из БД";
            await RefreshMediaCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка при удалении: {ex.Message}";
        }
    }
} 