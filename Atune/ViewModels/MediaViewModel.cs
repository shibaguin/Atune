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

namespace Atune.ViewModels;

public partial class MediaViewModel : ObservableObject
{
    private readonly IMemoryCache _cache;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    
    [ObservableProperty]
    private List<MediaItem> _mediaContent = new List<MediaItem>();

    [ObservableProperty]
    private string _statusMessage = "Готово к работе";

    [ObservableProperty]
    private ObservableCollection<MediaItem> _mediaItems = new();

    public Action<string>? UpdateStatusMessage { get; set; }

    public MediaViewModel(IMemoryCache cache, IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _cache = cache;
        _dbContextFactory = dbContextFactory;
        LoadMediaContent();
        
        // Добавляем автоматическую загрузку при инициализации
        RefreshMediaCommand.ExecuteAsync(null);
    }

    private void LoadMediaContent()
    {
        if (!_cache.TryGetValue("MediaContent", out List<MediaItem>? content))
        {
            // Если в кэше нет данных - загружаем из БД
            content = LoadFromDatabase();
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSize(content.Count * 500 + 1024)
                .SetPriority(CacheItemPriority.Normal)
                .SetSlidingExpiration(TimeSpan.FromMinutes(15));
            _cache.Set("MediaContent", content, cacheOptions);
        }
        MediaContent = content ?? new List<MediaItem>();
    }

    private List<MediaItem> LoadFromDataSource()
    {
        // Загрузка данных из внешнего источника
        return new List<MediaItem>();
    }

    private List<MediaItem> LoadFromDatabase()
    {
        // Загрузка данных из базы данных
        using var db = _dbContextFactory.CreateDbContext();
        return db.MediaItems.ToList();
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

            if (files.Count > 0)
            {
                using var db = _dbContextFactory.CreateDbContext();
                foreach (var file in files)
                {
                    var mediaItem = new MediaItem(
                        Path.GetFileNameWithoutExtension(file.Name),
                        "Unknown Artist",
                        file.Path.LocalPath,
                        TimeSpan.Zero);

                    await db.AddMediaAsync(mediaItem);
                }
                
                await RefreshMediaCommand.ExecuteAsync(null);
                StatusMessage = $"Успешно добавлено {files.Count} файлов";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshMedia()
    {
        using var db = _dbContextFactory.CreateDbContext();
        var items = await db.GetAllMediaAsync();
        MediaItems = new ObservableCollection<MediaItem>(items);
        
        // Обновляем кэш после загрузки новых данных
        _cache.Set("MediaContent", items, new MemoryCacheEntryOptions()
            .SetSize(items.Count * 500 + 1024)
            .SetPriority(CacheItemPriority.Normal)
            .SetSlidingExpiration(TimeSpan.FromMinutes(15)));
    }
} 