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

namespace Atune.ViewModels;

public partial class MediaViewModel : ObservableObject
{
    private readonly IMemoryCache _cache;
    
    [ObservableProperty]
    private List<MediaItem> _mediaContent = new List<MediaItem>();

    [ObservableProperty]
    private string _statusMessage = "Готово к работе";

    [ObservableProperty]
    private ObservableCollection<MediaItem> _mediaItems = new();

    public MediaViewModel(IMemoryCache cache)
    {
        _cache = cache;
        LoadMediaContent();
    }

    private void LoadMediaContent()
    {
        if (!_cache.TryGetValue("MediaContent", out List<MediaItem>? content))
        {
            content = LoadFromDataSource();
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSize(content.Count * 500 + 1024) // Базовый размер + 1KB на служебные данные
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
                StatusMessage = $"Добавлено файлов: {files.Count}";
                // Здесь будет логика добавления в БД
                await Task.CompletedTask;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
    }
} 