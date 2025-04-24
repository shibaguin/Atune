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
using Atune.Views;
using Serilog;
using System.ComponentModel;
using Atune.ViewModels;
using Atune.Helpers;
using TagLib;

namespace Atune.ViewModels;

public partial class MediaViewModel : ObservableObject, IDisposable
{
    private readonly IPlaylistService _playlistService;
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService? _logger;
    private readonly MediaPlayerService _mediaPlayerService;
    private readonly MediaDatabaseService _mediaDatabaseService;
    private readonly ISettingsService _settingsService;
    private readonly PlayHistoryService _playHistoryService;

    // Кэш для альбомов
    private List<AlbumInfo>? _albumCache;

    // Cache for artists
    private List<ArtistInfo>? _artistCache;

    [ObservableProperty]
    private List<MediaItem> _mediaContent = [];

    [ObservableProperty]
    private ObservableCollection<MediaItem> _mediaItems = [];

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

    private string? _sortOrder;
    // Backing fields for per-tab sort orders
    private string? _sortOrderTracks;
    private string? _sortOrderAlbums;
    private string? _sortOrderPlaylists;
    private string? _sortOrderArtists;

    public string SortOrder
    {
        get => _sortOrder ?? "A-Z";
        set
        {
            _sortOrder = value;
            OnPropertyChanged(nameof(SortOrder));
            SortMediaItems();
        }
    }

    // New per-tab sort-order properties
    public string SortOrderTracks
    {
        get => _sortOrderTracks ?? "A-Z";
        set
        {
            if (_sortOrderTracks != value)
            {
                _sortOrderTracks = value;
                OnPropertyChanged(nameof(SortOrderTracks));
                SortTracks();
                SaveSettings();
            }
        }
    }
    public string SortOrderAlbums
    {
        get => _sortOrderAlbums ?? "A-Z";
        set
        {
            if (_sortOrderAlbums != value)
            {
                _sortOrderAlbums = value;
                OnPropertyChanged(nameof(SortOrderAlbums));
                SortAlbums();
                SaveSettings();
            }
        }
    }
    public string SortOrderPlaylists
    {
        get => _sortOrderPlaylists ?? "A-Z";
        set
        {
            if (_sortOrderPlaylists != value)
            {
                _sortOrderPlaylists = value;
                OnPropertyChanged(nameof(SortOrderPlaylists));
                SortPlaylists();
                SaveSettings();
            }
        }
    }
    public string SortOrderArtists
    {
        get => _sortOrderArtists ?? "A-Z";
        set
        {
            if (_sortOrderArtists != value)
            {
                _sortOrderArtists = value;
                OnPropertyChanged(nameof(SortOrderArtists));
                SortArtists();
                SaveSettings();
            }
        }
    }

    public Action<string>? UpdateStatusMessage { get; set; }

    public IAsyncRelayCommand<MediaItem> PlayCommand { get; }
    public IRelayCommand StopCommand { get; }

    private bool _disposed;
    private int _currentQueueIndex = -1;
    // Expose current queue index for persistence
    public int CurrentQueueIndex => _currentQueueIndex;

    // Сохраняем отсортированный кэш для ускорения последующих операций сортировки
    private List<MediaItem> _sortedCache = [];

    // Новое свойство для альбомов
    public ObservableCollection<AlbumInfo> Albums { get; } = [];

    // New collection to hold the playback queue
    public ObservableCollection<MediaItem> PlaybackQueue { get; } = [];

    // New collection for playlists
    public ObservableCollection<Playlist> Playlists { get; } = [];

    // Commands to manage playback queue
    public IRelayCommand<MediaItem> AddToQueueCommand { get; }
    public IRelayCommand ClearQueueCommand { get; }
    public IAsyncRelayCommand PlayNextInQueueCommand { get; }
    public IAsyncRelayCommand<AlbumInfo?> PlayAlbumCommand { get; }
    public IAsyncRelayCommand PlayAllTracksCommand { get; }
    public IAsyncRelayCommand<MediaItem> PlayTrackCommand { get; }
    public IAsyncRelayCommand PlayPreviousInQueueCommand { get; }
    public IRelayCommand<int> SetQueuePositionCommand { get; }

    // Playlist commands
    public IAsyncRelayCommand CreatePlaylistCommand { get; }
    public IAsyncRelayCommand<Playlist?> OpenPlaylistCommand { get; }
    public IAsyncRelayCommand<Playlist?> AddToPlaylistCommand { get; }

    // New collection for artists
    public ObservableCollection<ArtistInfo> Artists { get; } = [];

    public MediaViewModel(
        IMemoryCache cache,
        IUnitOfWork unitOfWork,
        ILoggerService logger,
        MediaPlayerService mediaPlayerService,
        MediaDatabaseService mediaDatabaseService,
        IPlaylistService playlistService,
        ISettingsService settingsService,
        PlayHistoryService playHistoryService)
    {
        _cache = cache;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mediaPlayerService = mediaPlayerService;
        _mediaDatabaseService = mediaDatabaseService;
        _playlistService = playlistService;
        _settingsService = settingsService;
        _playHistoryService = playHistoryService;

        // Load saved sort orders
        var settings = _settingsService.LoadSettings();
        _sortOrderTracks = settings.SortOrderTracks;
        _sortOrderAlbums = settings.SortOrderAlbums;
        _sortOrderPlaylists = settings.SortOrderPlaylists;
        _sortOrderArtists = settings.SortOrderArtists;
        // Notify initial values for bindings
        OnPropertyChanged(nameof(SortOrderTracks));
        OnPropertyChanged(nameof(SortOrderAlbums));
        OnPropertyChanged(nameof(SortOrderPlaylists));
        OnPropertyChanged(nameof(SortOrderArtists));

        // Заменяем команды на релейтед команды из методов
        PlayCommand = new AsyncRelayCommand<MediaItem>(PlayMediaItem);
        StopCommand = new RelayCommand(StopPlayback);

        // Initialize playback queue commands
        AddToQueueCommand = new RelayCommand<MediaItem>(item => { if (item != null) PlaybackQueue.Add(item); });
        ClearQueueCommand = new RelayCommand(() => { PlaybackQueue.Clear(); _currentQueueIndex = -1; });
        PlayNextInQueueCommand = new AsyncRelayCommand(PlayNextInQueue);
        PlayAlbumCommand = new AsyncRelayCommand<AlbumInfo?>(PlayAlbum);
        PlayAllTracksCommand = new AsyncRelayCommand(PlayAllTracks);
        PlayTrackCommand = new AsyncRelayCommand<MediaItem>(async track =>
        {
            if (track == null) return;
            // Clear existing queue
            ClearQueueCommand.Execute(null);
            // Enqueue tracks starting from the selected one
            var tracks = MediaItems.ToList();
            int startIndex = tracks.IndexOf(track);
            if (startIndex < 0) startIndex = 0;
            foreach (var t in tracks.Skip(startIndex))
            {
                AddToQueueCommand.Execute(t);
            }
            // Start playback
            await PlayNextInQueueCommand.ExecuteAsync(null);
        });
        PlayPreviousInQueueCommand = new AsyncRelayCommand(PlayPreviousInQueue);
        SetQueuePositionCommand = new RelayCommand<int>(index => { _currentQueueIndex = index - 1; });

        _mediaPlayerService.PlaybackEnded += OnPlaybackEnded;

        // Заменяем вызов LoadMediaContent на прямое обновление
        Dispatcher.UIThread.Post(async () =>
        {
            await RefreshMedia();
        });

        MediaItems = [];
        SortOrder = "A-Z"; // Установите значение по умолчанию

        // Load existing playlists
        _ = LoadPlaylistsAsync();

        // Initialize playlist commands
        CreatePlaylistCommand = new AsyncRelayCommand(CreatePlaylistAsync);
        OpenPlaylistCommand = new AsyncRelayCommand<Playlist?>(OpenPlaylistAsync);
        AddToPlaylistCommand = new AsyncRelayCommand<Playlist?>(AddToPlaylistAsync);
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

        MediaContent = content ?? [];

        // Синхронизируем с MediaItems
        await UpdateMediaItemsGradually(MediaContent);
    }

    private List<MediaItem> LoadFromDataSource()
    {
        // Loading data from an external source
        // Загрузка данных из внешнего источника
        return [];
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
                            Patterns = new[] { "*.mp3", "*.flac", "*.wav", ".ogg", "*.aac", "*.wma", "*.alac", "*.ape" },
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
                        title: Path.GetFileNameWithoutExtension(file.Name),
                        album: new Album { Title = "Unknown Album" },
                        year: 0,
                        genre: "Unknown Genre",
                        path: realPath,
                        duration: TimeSpan.Zero,
                        trackArtists: new List<TrackArtist> {
                            new() { Artist = new Artist { Name = "Unknown Artist" } }
                        }
                    );

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
                await Task.Run(async () =>
                {
                    // Bulk-insert with incremental UI updates to avoid freezing
                    await _unitOfWork.Media.BulkInsertAsync(newItems, batch =>
                    {
                        _ = Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            int count = 0;
                            const int subBatch = 50;
                            foreach (var item in batch)
                            {
                                MediaItems.Insert(0, item);
                                count++;
                                if (count % subBatch == 0)
                                    await Task.Delay(10);
                            }
                            OnPropertyChanged(nameof(MediaItems));
                        }, Avalonia.Threading.DispatcherPriority.Background);
                    });

                    await _unitOfWork.CommitAsync();

                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SortMediaItems();
                        OnPropertyChanged(nameof(MediaItems));
                    }, Avalonia.Threading.DispatcherPriority.Background);
                });
                // Refresh all tracks and albums after insert
                await RefreshMedia();
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
                // Apply sorting after update
                SortMediaItems();
                OnPropertyChanged(nameof(MediaItems));

                // Reset album cache for grouping
                _albumCache = null;

                // Update albums after loading tracks
                UpdateAlbums();
                SortAlbums();
                // Update artists after loading tracks
                UpdateArtists();
                SortArtists();
            });

            _cache.Set("MediaContent", items, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetSize(items.Count * 500 + 1024));
            // Restore playback state now that media content is loaded
            if (Atune.App.Current is Atune.App app)
            {
                await app.RestorePlaybackStateAsync();
            }
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

    #region Новые методы для обработки артистов и создания MediaItem

    // Новый метод для разбора строки артистов, разделённых запятыми или точками с запятой
    private List<string> ParseArtists(string artistString)
    {
        if (string.IsNullOrWhiteSpace(artistString))
            return ["Unknown Artist"];

        var separators = new[] { ',', ';' };
        return artistString
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrEmpty(a))
                .ToList();
    }

    // Обновлённый метод для создания MediaItem с учётом метаданных и списка артистов
    private MediaItem CreateMediaItemFromPath(string path)
    {
        var tagInfo = GetDesktopTagInfo(path); // Existing metadata parsing

        // Extract embedded cover art via TagLibSharp
        string coverArtPath = string.Empty;
        try
        {
            var tfile = TagLib.File.Create(path);
            var pictures = tfile.Tag.Pictures;
            if (pictures != null && pictures.Length > 0)
            {
                var pic = pictures[0];
                var data = pic.Data.Data;
                var mimeType = pic.MimeType ?? "image/jpeg";
                var ext = mimeType.Contains("png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
                var coversDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atune", "Covers");
                Directory.CreateDirectory(coversDir);
                var fileName = Guid.NewGuid().ToString() + ext;
                var fullPath = Path.Combine(coversDir, fileName);
                System.IO.File.WriteAllBytes(fullPath, data);
                coverArtPath = fullPath;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error extracting embedded cover art for file {path}", ex);
        }

        var artists = ParseArtists(tagInfo.Artist);
        var trackArtists = artists.Select(artistName => new TrackArtist
        {
            Artist = new Artist { Name = artistName }
        }).ToList();

        var mediaItem = new MediaItem(
            title: Path.GetFileNameWithoutExtension(path),
            album: new Album { Title = tagInfo.Album ?? "Unknown Album", CoverArtPath = coverArtPath },
            year: tagInfo.Year,
            genre: tagInfo.Genre ?? "Unknown Genre",
            path: path,
            duration: tagInfo.Duration,
            trackArtists: trackArtists
        )
        {
            CoverArt = coverArtPath
        };
        return mediaItem;
    }

    #endregion

    #region Обновлённый метод AddFolderAsync

    [RelayCommand]
    private async Task AddFolderAsync()
    {
        try
        {
            IsBusy = true;
            // Получаем StorageProvider из верхнего уровня приложения
            var storageProvider = Application.Current?.ApplicationLifetime?.TryGetTopLevel()?.StorageProvider;
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
                    var files = await Task.Run(() =>
                        Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => _supportedFormats.Contains(Path.GetExtension(f).ToLower()))
                            .ToList());
                    allFiles.AddRange(files);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error accessing folder: {ex.Message}");
                }
            }

            // Получаем пути уже существующих элементов из базы
            var existingPaths = await _unitOfWork.Media.GetExistingPathsAsync(allFiles);
            var newPaths = allFiles.Except(existingPaths).ToList();

            if (newPaths.Count > 0)
            {
                await Task.Run(async () =>
                {
                    var newItems = newPaths.Select(path => CreateMediaItemFromPath(path)).ToList();

                    await _unitOfWork.Media.BulkInsertAsync(newItems, batch =>
                    {
                        _ = Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            int count = 0;
                            const int subBatch = 50;
                            foreach (var item in batch)
                            {
                                MediaItems.Insert(0, item);
                                count++;
                                if (count % subBatch == 0)
                                    await Task.Delay(10);
                            }
                            OnPropertyChanged(nameof(MediaItems));
                        }, Avalonia.Threading.DispatcherPriority.Background);
                    });

                    await _unitOfWork.CommitAsync();

                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UpdateAlbums();
                        SortAlbums();
                    }, Avalonia.Threading.DispatcherPriority.Background);
                });
                // Refresh all tracks and albums after insert
                await RefreshMedia();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    private static (string Artist, string Album, uint Year, string Genre, TimeSpan Duration) GetDesktopTagInfo(string path)
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

    private static readonly string[] _supportedFormats = [".mp3", ".flac", ".wav", ".ogg"];

    [RelayCommand]
    private async Task SearchMedia(string query)
    {
        try
        {
            IsBusy = true;
            var allItems = await _unitOfWork.Media.GetAllWithDetailsAsync();
            var filtered = allItems.Where(item =>
                (!string.IsNullOrEmpty(item.Title) && item.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)) ||
                (item.TrackArtists.Any(ta =>
                    ta.Artist != null &&
                    ta.Artist.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))) ||
                (!string.IsNullOrEmpty(item.Album.Title) && item.Album.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)) ||
                (!string.IsNullOrEmpty(item.Genre) && item.Genre.Contains(query, StringComparison.CurrentCultureIgnoreCase)) ||
                (!string.IsNullOrEmpty(item.Path) && item.Path.Contains(query, StringComparison.CurrentCultureIgnoreCase))
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
                // Ensure current volume applied before any playback
                _mediaPlayerService.Volume = _settingsService.LoadSettings().Volume;
                await _mediaPlayerService.StopAsync();
                SelectedMediaItem = mediaItem;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OnPropertyChanged(nameof(SelectedMediaItem));
                });

                await _mediaPlayerService.Play(mediaItem.Path);
                // Re-apply volume in case Play resets it
                _mediaPlayerService.Volume = _settingsService.LoadSettings().Volume;

                // Update UI
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
            else if (PlaybackQueue.Count > 0)
            {
                PlayNextInQueueCommand.Execute(null);
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
    private static async Task MoreInfo(MediaItem mediaItem)
    {
        if (mediaItem == null)
        {
            Log.Warning("MediaItem is null");
            return;
        }
        Log.Information($"Title: {mediaItem.Title}, Artist: {mediaItem.TrackArtists.FirstOrDefault()?.Artist.Name}, Album: {mediaItem.Album.Title}");

        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (mainWindow == null)
        {
            Log.Warning("MainWindow is null");
            return;
        }

        var infoWindow = new Window
        {
            Title = "Информация о медиа",
            Width = 400,
            Height = 300,
            Content = new MediaInfoView(mediaItem),
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        await infoWindow.ShowDialog(mainWindow);
    }

    private void SortMediaItems()
    {
        // Выполняем обновление коллекции в UI-потоке
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            List<MediaItem> sortedList = SortOrderTracks switch
            {
                "A-Z" => [.. MediaItems.OrderBy(item => item.Title, new CustomTitleComparer(true))],
                "Z-A" => [.. MediaItems.OrderBy(item => item.Title, new CustomTitleComparer(false))],
                "Сначала старые" => [.. MediaItems.OrderBy(item => item.Year)],
                "Сначала новые" => [.. MediaItems.OrderByDescending(item => item.Year)],
                _ => [.. MediaItems.OrderBy(item => item.Title, new CustomTitleComparer(true))],
            };
            MediaItems.Clear();
            foreach (var item in sortedList)
            {
                MediaItems.Add(item);
            }
            _logger?.LogInformation($"MediaItems has been sorted in {SortOrderTracks} order.");
            // Обновляем кэш отсортированных элементов после сортировки
            _sortedCache = new List<MediaItem>(MediaItems);
        });
    }

    // Новый метод для инкрементального добавления нового элемента в отсортированное представление
    public void InsertItemSorted(MediaItem newItem)
    {
        var titleComparer = new CustomTitleComparer(SortOrderTracks == "A-Z");
        // Если кэш не синхронизирован с MediaItems, пересчитываем его
        if (_sortedCache.Count != MediaItems.Count)
        {
            _sortedCache = [.. MediaItems];
            _sortedCache.Sort(new MediaItemComparer(titleComparer));
        }

        // Ищем индекс для вставки нового элемента с помощью бинарного поиска
        int index = _sortedCache.BinarySearch(newItem, new MediaItemComparer(titleComparer));
        if (index < 0)
        {
            index = ~index;
        }

        _sortedCache.Insert(index, newItem);
        MediaItems.Insert(index, newItem);
        _logger?.LogInformation($"Inserted new item '{newItem.Title}' at index {index}, SortOrderTracks={SortOrderTracks}");
    }

    // Новый асинхронный RelayCommand для открытия карточки альбома
    [RelayCommand]
    private Task OpenAlbum(AlbumInfo album)
    {
        if (album == null)
            return Task.CompletedTask;

        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow?.DataContext is MainViewModel mainVm)
        {
            var albumControl = new AlbumView { DataContext = new AlbumViewModel(album) };
            mainVm.CurrentView = albumControl;
            mainVm.HeaderText = album.AlbumName;
        }
        else
        {
            _logger?.LogWarning("MainViewModel not found, cannot open album view.");
        }
        return Task.CompletedTask;
    }

    // Метод для обновления списка альбомов на основе MediaItems
    private void UpdateAlbums()
    {
        // Проверяем, есть ли кэш альбомов
        if (_albumCache == null)
        {
            var albumGroups = MediaItems
                .GroupBy(m => m.Album.Title)
                .Select(g =>
                {
                    var first = g.First();
                    return new AlbumInfo(
                        albumTitle: g.Key,
                        artistName: first.TrackArtists.FirstOrDefault()?.Artist?.Name ?? "Unknown Artist",
                        year: first.Year,
                        tracks: [.. g]
                    );
                })
                .Where(album => album.Tracks.Count >= 3) // Фильтруем альбомы с 3 или более треками
                .OrderBy(a => a.AlbumName)
                .ToList();

            Albums.Clear();
            foreach (var album in albumGroups)
            {
                Albums.Add(album);
                _logger?.LogInformation($"Added album: {album.AlbumName} ({album.TrackCount} tracks)");
            }

            // Сохраняем альбомы в кэш
            _albumCache = albumGroups;
        }
        else
        {
            // Если кэш уже существует, просто обновляем ObservableCollection
            Albums.Clear();
            foreach (var album in _albumCache)
            {
                Albums.Add(album);
            }
        }
    }

    // Добавляем новый метод для вывода содержимого БД в терминал:
    [RelayCommand]
    private async Task PrintDatabaseAsync()
    {
        try
        {
            // Получаем все медиа-объекты с заполненными навигационными свойствами
            var items = await _unitOfWork.Media.GetAllMediaItemsAsync();
            Console.WriteLine("Содержимое базы данных:");
            foreach (var item in items)
            {
                try
                {
                    // Формируем строку с информацией о медиа-объекте
                    var artists = string.Join(", ", item.TrackArtists.Select(ta => ta.Artist?.Name ?? "Unknown Artist"));
                    var album = item.Album?.Title ?? "No Album";
                    Console.WriteLine($"ID: {item.Id}, Title: {item.Title}, Album: {album}, Artists: {artists}, Path: {item.Path}");
                }
                catch (Exception innerEx)
                {
                    // Логируем ошибку для конкретного элемента и продолжаем обработку остальных
                    _logger?.LogError($"Ошибка при выводе информации для медиа-объекта с ID: {item.Id}", innerEx);
                }
            }
        }
        catch (Exception ex)
        {
            // Логируем ошибку получения данных из базы
            _logger?.LogError("Ошибка при получении медиа-объектов из базы данных", ex);
            Console.WriteLine($"Ошибка при выводе содержимого БД: {ex.Message}");
        }
    }

    // New method: play next item from the queue
    private async Task PlayNextInQueue()
    {
        if (PlaybackQueue.Count == 0) return;
        if (_currentQueueIndex < PlaybackQueue.Count - 1)
            _currentQueueIndex++;
        else
            _currentQueueIndex = 0;
        await PlayMediaItem(PlaybackQueue[_currentQueueIndex]);
    }

    // New method: enqueue all tracks and play
    private async Task PlayAllTracks()
    {
        _currentQueueIndex = -1;
        PlaybackQueue.Clear();
        foreach (var item in MediaItems)
            PlaybackQueue.Add(item);
        await PlayNextInQueue();
    }

    // New method: enqueue album tracks and play
    private async Task PlayAlbum(AlbumInfo? album)
    {
        if (album == null) return;
        _currentQueueIndex = -1;
        PlaybackQueue.Clear();
        foreach (var item in album.Tracks)
            PlaybackQueue.Add(item);
        await PlayNextInQueue();
    }

    // Add PlayPreviousInQueue implementation
    private async Task PlayPreviousInQueue()
    {
        if (PlaybackQueue.Count == 0) return;
        if (_currentQueueIndex > 0)
            _currentQueueIndex--;
        else
            _currentQueueIndex = PlaybackQueue.Count - 1;
        await PlayMediaItem(PlaybackQueue[_currentQueueIndex]);
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
            _playHistoryService.Dispose();
        }

        _disposed = true;
    }

    // Persist all sort settings
    private void SaveSettings()
    {
        var s = _settingsService.LoadSettings();
        s.SortOrderTracks = SortOrderTracks;
        s.SortOrderAlbums = SortOrderAlbums;
        s.SortOrderPlaylists = SortOrderPlaylists;
        s.SortOrderArtists = SortOrderArtists;
        _settingsService.SaveSettings(s);
    }

    // Sorting implementations per tab
    private void SortTracks() => SortMediaItems();
    private void SortAlbums()
    {
        List<AlbumInfo> sorted = SortOrderAlbums switch
        {
            "A-Z" => [.. Albums.OrderBy(a => a.AlbumName, new CustomTitleComparer(true))],
            "Z-A" => [.. Albums.OrderBy(a => a.AlbumName, new CustomTitleComparer(false))],
            "Сначала старые" => [.. Albums.OrderBy(a => a.Year)],
            "Сначала новые" => [.. Albums.OrderByDescending(a => a.Year)],
            _ => [.. Albums.OrderBy(a => a.AlbumName, new CustomTitleComparer(true))],
        };
        Albums.Clear();
        foreach (var album in sorted) Albums.Add(album);
    }
    private void SortPlaylists()
    {
        // Sort playlists according to SortOrderPlaylists
        List<Playlist> sorted = SortOrderPlaylists switch
        {
            "A-Z" => [.. Playlists.OrderBy(p => p.Name, new CustomTitleComparer(true))],
            "Z-A" => [.. Playlists.OrderBy(p => p.Name, new CustomTitleComparer(false))],
            _ => [.. Playlists.OrderBy(p => p.Name, new CustomTitleComparer(true))],
        };
        Playlists.Clear();
        foreach (var p in sorted)
            Playlists.Add(p);
    }
    private void SortArtists()
    {
        List<ArtistInfo> sorted = SortOrderArtists switch
        {
            "A-Z" => [.. Artists.OrderBy(a => a.ArtistName, new CustomTitleComparer(true))],
            "Z-A" => [.. Artists.OrderBy(a => a.ArtistName, new CustomTitleComparer(false))],
            _ => [.. Artists.OrderBy(a => a.ArtistName, new CustomTitleComparer(true))],
        };
        Artists.Clear();
        foreach (var artist in sorted)
            Artists.Add(artist);
    }

    // Playlists commands
    private async Task LoadPlaylistsAsync()
    {
        Playlists.Clear();
        var list = await _playlistService.GetPlaylistsAsync();
        foreach (var p in list)
            Playlists.Add(p);
    }

    private async Task CreatePlaylistAsync()
    {
        // Load current playlists to check for name conflicts
        await LoadPlaylistsAsync();
        const string baseName = "New Playlist";
        string newName = baseName;
        int counter = 1;
        // Increment suffix until name is unique
        while (Playlists.Any(pl => pl.Name == newName))
        {
            newName = $"{baseName} ({counter})";
            counter++;
        }
        try
        {
            var id = await _playlistService.CreatePlaylistAsync(newName);
            if (id > 0)
            {
                await LoadPlaylistsAsync();
                var newPl = Playlists.FirstOrDefault(pl => pl.Id == id);
                if (newPl != null)
                    await OpenPlaylistAsync(newPl);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error creating playlist '{newName}'", ex);
        }
    }

    private Task OpenPlaylistAsync(Playlist? playlist)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = desktop.MainWindow?.DataContext as MainViewModel;
            mainVm?.GoPlaylistCommand.Execute(playlist);
        }
        return Task.CompletedTask;
    }

    private async Task AddToPlaylistAsync(Playlist? playlist)
    {
        if (playlist == null)
        {
            _logger?.LogWarning("AddToPlaylistAsync called with null playlist");
            return;
        }
        if (SelectedMediaItem == null)
        {
            _logger?.LogWarning("AddToPlaylistAsync: SelectedMediaItem is null");
            return;
        }
        try
        {
            _logger?.LogInformation($"Adding track '{SelectedMediaItem.Title}' to playlist '{playlist.Name}' (Id={playlist.Id})");
            var count = await _playlistService.AddToPlaylistAsync(playlist.Id, SelectedMediaItem.Id);
            _logger?.LogInformation($"Added {count} item(s) into playlist '{playlist.Name}'");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error adding track '{SelectedMediaItem.Title}' to playlist '{playlist.Name}'", ex);
            return;
        }
        // Navigate to the playlist view to display updated track list
        await OpenPlaylistAsync(playlist);
    }

    // Method to update artists based on MediaItems
    private void UpdateArtists()
    {
        if (_artistCache == null)
        {
            var artistGroups = MediaItems
                .SelectMany(m => m.TrackArtists.Select(ta => new { Track = m, ArtistName = ta.Artist?.Name }))
                .Where(x => !string.IsNullOrWhiteSpace(x.ArtistName))
                .GroupBy(x => x.ArtistName)
                .Select(g => new ArtistInfo(
                    artistName: g.Key!,
                    tracks: g.Select(x => x.Track).ToList()
                ))
                .OrderBy(a => a.ArtistName)
                .ToList();

            Artists.Clear();
            foreach (var artist in artistGroups)
                Artists.Add(artist);

            _artistCache = artistGroups;
        }
        else
        {
            Artists.Clear();
            foreach (var artist in _artistCache)
                Artists.Add(artist);
        }
    }

    // Новый асинхронный RelayCommand для открытия карточки артиста
    [RelayCommand]
    private Task OpenArtist(ArtistInfo artist)
    {
        if (artist == null)
            return Task.CompletedTask;

        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow?.DataContext is MainViewModel mainVm)
        {
            var artistControl = new ArtistView { DataContext = new ArtistViewModel(artist) };
            mainVm.CurrentView = artistControl;
            mainVm.HeaderText = artist.ArtistName;
        }
        else
        {
            _logger?.LogWarning("MainViewModel not found, cannot open artist view.");
        }
        return Task.CompletedTask;
    }
}

public class CustomTitleComparer(bool ascending) : IComparer<string>
{
    private readonly bool _ascending = ascending;

    public int Compare(string? s1, string? s2)
    {
        // Если обе строки равны
        if (ReferenceEquals(s1, s2)) return 0;
        if (s1 is null) return _ascending ? -1 : 1;
        if (s2 is null) return _ascending ? 1 : -1;

        int minLen = Math.Min(s1.Length, s2.Length);
        for (int i = 0; i < minLen; i++)
        {
            char c1 = s1[i];
            char c2 = s2[i];
            int cat1 = GetCategory(c1);
            int cat2 = GetCategory(c2);

            if (cat1 != cat2)
            {
                int cmp = cat1.CompareTo(cat2);
                return _ascending ? cmp : -cmp;
            }
            // Если символы из одной категории, сравниваем их без учета регистра
            int cmpChar = char.ToUpperInvariant(c1).CompareTo(char.ToUpperInvariant(c2));
            if (cmpChar != 0)
            {
                return _ascending ? cmpChar : -cmpChar;
            }
        }
        // Если один текст является префиксом другого, более короткая строка считается "меньшей"
        int lenDiff = s1.Length.CompareTo(s2.Length);
        return _ascending ? lenDiff : -lenDiff;
    }

    // Метод возвращает числовую категорию символа:
    // 0 - цифры, 1 - латинские буквы, 2 - кириллические буквы, 3 - все остальное
    private int GetCategory(char c)
    {
        if (char.IsDigit(c))
            return 0;
        else if (IsLatin(c))
            return 1;
        else if (IsCyrillic(c))
            return 2;
        else
            return 3;
    }

    private static bool IsLatin(char c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
    }

    private static bool IsCyrillic(char c)
    {
        // Основной диапазон кириллических символов: U+0400 — U+04FF
        return c >= '\u0400' && c <= '\u04FF';
    }
}

// Добавляем новый компаратор для MediaItem, использующий наш компаратор по Title
public class MediaItemComparer(IComparer<string> titleComparer) : IComparer<MediaItem>
{
    private readonly IComparer<string> _titleComparer = titleComparer;

    public int Compare(MediaItem? x, MediaItem? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        int result = _titleComparer.Compare(x.Title, y.Title);

        return result;
    }
}
