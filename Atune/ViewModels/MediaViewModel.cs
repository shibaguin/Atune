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
using ATL;
using Atune.Services;
using Atune.Extensions;

namespace Atune.ViewModels;

public partial class MediaViewModel : ObservableObject, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService? _logger;
    private readonly MediaPlayerService _mediaPlayerService;
    
    [ObservableProperty]
    private List<MediaItem> _mediaContent = new List<MediaItem>();

    [ObservableProperty]
    private ObservableCollection<MediaItem> _mediaItems = new();

    [ObservableProperty]
    private MediaItem? _selectedMediaItem;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isShuffleEnabled;

    [ObservableProperty]
    private bool _isRepeatEnabled;

    [ObservableProperty]
    private string _playPauseIcon = "fa-solid fa-play";

    public Action<string>? UpdateStatusMessage { get; set; }

    public IAsyncRelayCommand PlayCommand { get; }
    public IRelayCommand StopCommand { get; }

    private bool _disposed;

    public MediaViewModel(
        IMemoryCache cache, 
        IUnitOfWork unitOfWork, 
        ILoggerService logger,
        MediaPlayerService mediaPlayerService)
    {
        _cache = cache;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mediaPlayerService = mediaPlayerService;
        
        // Заменяем команды на релейтед команды из методов
        PlayCommand = new AsyncRelayCommand<MediaItem>(PlayMediaItem);
        StopCommand = new RelayCommand(StopPlayback);

        _mediaPlayerService.PlaybackEnded += OnPlaybackEnded;
        
        // Заменяем вызов LoadMediaContent на прямое обновление
        Dispatcher.UIThread.Post(async () => 
        {
            await RefreshMedia();
        });
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
            
            // For Android, we reduce the caching time
            // Для Android мы уменьшаем время кэширования
            if (OperatingSystem.IsAndroid())
            {
                cacheOptions.SetSlidingExpiration(TimeSpan.FromMinutes(5));
            }
            
            _cache.Set("MediaContent", content, cacheOptions);
        }
        
        MediaContent = content ?? new List<MediaItem>();
        
        // Синхронизируем с MediaItems
        await UpdateMediaItemsGradually(MediaContent);
    }

    private List<MediaItem> LoadFromDataSource()
    {
        // Loading data from an external source
        // Загрузка данных из внешнего источника
        return new List<MediaItem>();
    }

    [RelayCommand]
    private async Task AddMediaAsync()
    {
        try
        {
            IsBusy = true;
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var mainWindow = desktop.MainWindow;
            if (mainWindow == null)
            {
                _logger?.LogError("MainWindow is null");
                return;
            }

            var topLevel = TopLevel.GetTopLevel(mainWindow);
            if (topLevel == null)
            {
                _logger?.LogError("TopLevel is null");
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Select audio files",
                    AllowMultiple = true,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Audio files")
                        {
                            Patterns = new[] { "*.mp3", "*.flac", "*.wav", "*.ogg", "*.aac", "*.wma", "*.alac", "*.ape" },
                            MimeTypes = new[] { "audio/mpeg", "audio/flac", "audio/wav", "audio/ogg", "audio/aac", "audio/wma", "audio/alac", "audio/ape" }
                        }
                    }
                });

            int duplicateCount = 0;
            int successCount = 0;
            int errorCount = 0;

            var newItems = new List<MediaItem>();

            var allPaths = files.Select(f => f.Path.LocalPath).ToList();
            var existingPaths = await _unitOfWork.Media.GetExistingPathsAsync(allPaths);

            foreach (var file in files)
            {
                if (existingPaths.Contains(file.Path.LocalPath))
                {
                    duplicateCount++;
                    continue;
                }

                try
                {
                    var realPath = file.Path.LocalPath;
                    
                    var mediaItem = new MediaItem(
                        Path.GetFileNameWithoutExtension(file.Name),
                        "Unknown Artist",
                        "Unknown Album",
                        0,
                        "Unknown Genre",
                        realPath,
                        TimeSpan.Zero);

                    newItems.Add(mediaItem);
                    successCount++;
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint") == true)
                {
                    duplicateCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            if (newItems.Count > 0)
            {
                await _unitOfWork.Media.BulkInsertAsync(newItems, batch => 
                {
                    Dispatcher.UIThread.Post(() => 
                    {
                        foreach (var item in batch)
                        {
                            MediaItems.Insert(0, item);
                        }
                    });
                });
                
                await _unitOfWork.CommitAsync();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateMediaItemsGradually(IEnumerable<MediaItem> items)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            MediaItems.Clear();
            
            // Batch addition for gradual updating
            // Добавление пакета для постепенной обновления
            var batchSize = OperatingSystem.IsAndroid() ? 50 : 200;
            var count = 0;
            
            foreach (var item in items)
            {
                MediaItems.Add(item);
                count++;
                
                if (count % batchSize == 0)
                {
                    OnPropertyChanged(nameof(MediaItems));
                    await Task.Delay(10); // Delay for rendering / Задержка для рендеринга
                }
            }
            
            OnPropertyChanged(nameof(MediaItems));
        });
    }

    [RelayCommand]
    private async Task RefreshMedia()
    {
        try
        {
            IsBusy = true;
            var items = (await _unitOfWork.Media.GetAllWithDetailsAsync()).ToList();
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MediaItems.Clear();
                foreach (var item in items)
                {
                    MediaItems.Add(item);
                }
                OnPropertyChanged(nameof(MediaItems));
            });
            
            _cache.Set("MediaContent", items, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetSize(items.Count * 500 + 1024));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading: {ex.Message}");
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
            // Выполняем удаление в фоновом потоке, чтобы не блокировать UI
            // Perform deletion in a background thread to prevent UI blocking
            await Task.Run(async () =>
            {
                var allMedia = await _unitOfWork.Media.GetAllAsync();
                foreach (var media in allMedia)
                {
                    await _unitOfWork.Media.DeleteAsync(media);
                }
                await _unitOfWork.CommitAsync();
            });
            // Обновляем UI после завершения операции удаления
            // Update UI after deletion operation is complete
            await RefreshMediaCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddFolderAsync()
    {
        try
        {
            IsBusy = true;
            var mainWindow = Application.Current?.ApplicationLifetime?.TryGetTopLevel();
            if (mainWindow == null)
            {
                _logger?.LogError("MainWindow is null");
                return;
            }

            var storageProvider = mainWindow.StorageProvider;
            if (storageProvider == null)
            {
                _logger?.LogError("StorageProvider is not available");
                return;
            }

            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select folders with music",
                AllowMultiple = true
            });

            var allFiles = new List<string>();
            foreach (var folder in folders)
            {
                try 
                {
                    var folderPath = folder.Path.LocalPath;
                    // Асинхронное перечисление файлов для предотвращения блокировки UI
                    // Asynchronous enumeration of files to prevent UI blocking
                    var files = await Task.Run(() =>
                        Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => _supportedFormats.Contains(Path.GetExtension(f).ToLower()))
                            .ToList());
                    allFiles.AddRange(files);
                }
                catch(Exception ex)
                {
                    _logger?.LogError($"Error accessing folder: {ex.Message}");
                }
            }

            var existingPaths = await _unitOfWork.Media.GetExistingPathsAsync(allFiles);
            var newItems = allFiles.Except(existingPaths)
                .Select(path => CreateMediaItemFromPath(path))
                .ToList();

            if (newItems.Count > 0)
            {
                // Переносим тяжелые операции в отдельный поток
                // Move heavy operations to a separate thread
                await Task.Run(async () =>
                {
                    await _unitOfWork.Media.BulkInsertAsync(newItems, batch =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            foreach (var item in batch)
                            {
                                MediaItems.Insert(0, item);
                            }
                        });
                    });
                    await _unitOfWork.CommitAsync();
                });
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private MediaItem CreateMediaItemFromPath(string path)
    {
        var tagInfo = GetDesktopTagInfo(path); // Use the method from MediaView / Используем метод из MediaView
        return new MediaItem(
            Path.GetFileNameWithoutExtension(path),
            tagInfo.Artist ?? "Unknown Artist",
            tagInfo.Album ?? "Unknown Album",
            tagInfo.Year,
            tagInfo.Genre ?? "Unknown Genre",
            path,
            tagInfo.Duration);
    }

    private (string Artist, string Album, uint Year, string Genre, TimeSpan Duration) GetDesktopTagInfo(string path)
    {
        try
        {
            var track = new ATL.Track(path);
            return (
                track.Artist ?? "Unknown Artist",
                track.Album ?? "Unknown Album",
                (uint)(track.Year > 0 ? track.Year : DateTime.Now.Year),
                track.Genre ?? "Unknown Genre",
                TimeSpan.FromMilliseconds(track.DurationMs)
            );
        }
        catch
        {
            return ("Unknown Artist", "Unknown Album", (uint)DateTime.Now.Year, "Unknown Genre", TimeSpan.Zero);
        }
    }

    private static readonly string[] _supportedFormats = { ".mp3", ".flac", ".wav", ".ogg" };

    [RelayCommand]
    private async Task SearchMedia(string query)
    {
        try
        {
            IsBusy = true;
            var allItems = await _unitOfWork.Media.GetAllWithDetailsAsync();
            var filtered = allItems.Where(item =>
                (!string.IsNullOrEmpty(item.Title) && item.Title.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(item.Artist) && item.Artist.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(item.Album) && item.Album.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(item.Genre) && item.Genre.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(item.Path) && item.Path.ToLower().Contains(query))
            ).ToList();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MediaItems.Clear();
                foreach (var item in filtered)
                {
                    MediaItems.Add(item);
                }
                OnPropertyChanged(nameof(MediaItems));
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error searching: {ex.Message}", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NextMediaItem()
    {
        Dispatcher.UIThread.VerifyAccess();
        
        if (MediaItems == null || MediaItems.Count == 0) 
            return;

        var currentIndex = SelectedMediaItem != null ? MediaItems.IndexOf(SelectedMediaItem) : -1;
        var nextIndex = currentIndex + 1;

        if (nextIndex < MediaItems.Count)
        {
            SelectedMediaItem = MediaItems[nextIndex];
            await PlayMediaItem(SelectedMediaItem);
        }
        else if (IsShuffleEnabled)
        {
            await PlayRandomMediaItem();
        }
        else 
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                SelectedMediaItem = null;
                PlayPauseIcon = "fa-solid fa-play";
            });
        }
    }

    [RelayCommand]
    private void PreviousMediaItem()
    {
        if (MediaItems.Count == 0) return;
        
        int currentIndex = SelectedMediaItem != null 
            ? MediaItems.IndexOf(SelectedMediaItem) 
            : 0;
        
        int newIndex = (currentIndex - 1 + MediaItems.Count) % MediaItems.Count;
        var previousItem = MediaItems[newIndex];
        PlayMediaItemCommand.Execute(previousItem);
    }

    [RelayCommand]
    private async Task PlayMediaItem(MediaItem? mediaItem)
    {
        if (mediaItem != null && !string.IsNullOrWhiteSpace(mediaItem.Path))
        {
            try 
            {
                await _mediaPlayerService.StopAsync(); // Добавляем асинхронную остановку
                SelectedMediaItem = mediaItem;
                
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    OnPropertyChanged(nameof(SelectedMediaItem));
                });
                
                await _mediaPlayerService.Play(mediaItem.Path);
                
                // Обновление интерфейса через главный поток
                Dispatcher.UIThread.Post(() => 
                {
                    OnPropertyChanged(nameof(MediaItems));
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Playback error: {ex.Message}", ex);
            }
        }
    }

    [RelayCommand]
    private void StopPlayback()
    {
        _mediaPlayerService.Stop();
        SelectedMediaItem = null;
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        // Обновляем через Dispatcher для работы с UI элементами
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (IsRepeatEnabled)
            {
                PlayCurrentMediaItem();
            }
            else
            {
                NextMediaItemCommand.Execute(null);
            }
        });
    }

    [RelayCommand]
    private async Task PlayRandomMediaItem()
    {
        if (MediaItems.Count == 0) return;
        
        var random = new Random();
        int index = random.Next(MediaItems.Count);
        await PlayMediaItem(MediaItems[index]);
    }

    [RelayCommand]
    private void PlayCurrentMediaItem()
    {
        if (SelectedMediaItem != null)
        {
            PlayMediaItemCommand.Execute(SelectedMediaItem);
        }
    }

    [RelayCommand]
    private void MoreInfo(MediaItem mediaItem)
    {
        // Пустая реализация команды
        // Здесь можно добавить логику, если потребуется
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            _mediaPlayerService.PlaybackEnded -= OnPlaybackEnded;
        }
        
        _disposed = true;
    }
} 