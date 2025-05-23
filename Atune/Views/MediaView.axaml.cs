using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System;
using Atune.Models;
using Avalonia;
using Avalonia.Markup.Xaml;
using Atune.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
#if ANDROID
using Android.Net;
using Android.App;
using Android.Provider;
using Android.Util;
#endif
using ATL;
using Microsoft.Extensions.Logging;
using Atune.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Atune.Views;

public partial class MediaView : UserControl
{
    public new MediaViewModel DataContext
    {
        get => (MediaViewModel)base.DataContext!;
        set => base.DataContext = value;
    }

    public IRelayCommand PlayMediaItemCommand => DataContext.PlayMediaItemCommand;
    public IRelayCommand MoreInfoCommand => DataContext.MoreInfoCommand;
    public IAsyncRelayCommand<AlbumInfo> OpenAlbumCommand => DataContext.OpenAlbumCommand;
    public IAsyncRelayCommand<MediaItem> PlayTrackCommand => DataContext.PlayTrackCommand;
    public IAsyncRelayCommand<Playlist> OpenPlaylistCommand => DataContext.OpenPlaylistCommand;
    public IAsyncRelayCommand<Playlist> AddToPlaylistCommand => DataContext.AddToPlaylistCommand;
    public IAsyncRelayCommand<AlbumInfo?> PlayAlbumCommand => DataContext.PlayAlbumCommand;
    public IAsyncRelayCommand<ArtistInfo> OpenArtistCommand => DataContext.OpenArtistCommand;

    private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;
    private readonly ILoggerService? _logger;
    private readonly IMemoryCache _cache = default!;
    private readonly MediaDatabaseService _mediaDatabaseService = default!;
    private readonly MediaFileService _mediaFileService = default!;

    public MediaView()
    {
        InitializeComponent();
    }

    public MediaView(MediaViewModel vm, IDbContextFactory<AppDbContext> dbContextFactory, ILoggerService logger, IMemoryCache cache) : this()
    {
        DataContext = vm;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _cache = cache;
        _mediaDatabaseService = new MediaDatabaseService(dbContextFactory, logger, cache);
        _mediaFileService = new MediaFileService(cache);
    }

    private async void AddToLibrary_Click(object sender, RoutedEventArgs e)
    {
        const string logHeader = "[MediaView]";
        _logger?.LogInformation($"{logHeader} Button clicked");

        if (!await _mediaDatabaseService.CanConnectAsync())
        {
            _logger?.LogWarning($"{logHeader} No database connection");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        var storageProvider = topLevel?.StorageProvider;

        if (storageProvider is null)
        {
            _logger?.LogWarning($"{logHeader} StorageProvider is not available");
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select audio files",
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
            string realPath;
            try
            {
                if (OperatingSystem.IsAndroid())
                {
#if ANDROID
                    Android.Util.Log.Debug("MediaView", $"Start copying file: {file.Name}");
#endif
                    var destFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AtuneMedia");
                    if (!Directory.Exists(destFolder))
                        Directory.CreateDirectory(destFolder);
                    var destPath = Path.Combine(destFolder, file.Name);

                    // Проверяем, существует ли уже файл с таким именем
                    if (File.Exists(destPath))
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                        var extension = Path.GetExtension(file.Name);
                        var counter = 1;
                        while (File.Exists(destPath))
                        {
                            destPath = Path.Combine(destFolder, $"{fileNameWithoutExt}_{counter}{extension}");
                            counter++;
                        }
                    }

                    using (var sourceStream = await file.OpenReadAsync())
                    using (var destStream = File.Create(destPath))
                    {
                        await sourceStream.CopyToAsync(destStream);
                    }
#if ANDROID
                    Android.Util.Log.Debug("MediaView", $"File copied to: {destPath}");
#endif
                    realPath = destPath;
                }
                else
                {
                    realPath = file.Path.LocalPath;
                    // Проверяем корректность пути для текущей ОС
                    if (OperatingSystem.IsWindows())
                    {
                        realPath = realPath.Replace("/", "\\");
                    }
                    else
                    {
                        realPath = realPath.Replace("\\", "/");
                    }
                }

                _logger?.LogInformation($"{logHeader} Processing file: {realPath}");

                if (await _mediaDatabaseService.ExistsByPathAsync(realPath))
                {
                    duplicateCount++;
                    continue;
                }

                if (!OperatingSystem.IsAndroid())
                {
                    if (!await _mediaFileService.FileExistsAsync(realPath))
                    {
                        _logger?.LogWarning($"{logHeader} File does not exist: {realPath}");
                        errorCount++;
                        continue;
                    }
                }
                else
                {
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
                        _logger?.LogWarning($"{logHeader} File does not exist: {realPath}");
                        errorCount++;
                        continue;
                    }
#endif
                }

                var tagInfo = GetDesktopTagInfo(realPath);
#if ANDROID
                Android.Util.Log.Debug("MediaView", $"Tags received: Artist={tagInfo.Artist}, Album={tagInfo.Album}, Year={tagInfo.Year}");
#endif
                var duration = tagInfo.Duration;

                uint year = tagInfo.Year > 0
                    ? tagInfo.Year
                    : (uint)DateTime.Now.Year;

                var mediaItem = new MediaItem(
                    title: Path.GetFileNameWithoutExtension(file.Name),
                    album: new Album { Title = tagInfo.Album ?? "Unknown Album" },
                    year: year,
                    genre: tagInfo.Genre ?? "Unknown Genre",
                    path: realPath,
                    duration: duration,
                    trackArtists: new List<TrackArtist> {
                        new() {
                            Artist = new Artist { Name = tagInfo.Artist ?? "Unknown Artist" }
                        }
                    }
                );
                await _mediaDatabaseService.AddMediaItemAsync(mediaItem);
                successCount++;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint") == true)
            {
                duplicateCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger?.LogError($"Error: {ex.Message}", ex);
            }
        }

        if (successCount > 0)
        {
            _logger?.LogInformation($"{logHeader} Successfully added {successCount} files");

            if (DataContext is MediaViewModel vm)
            {
                await vm.RefreshMediaCommand.ExecuteAsync(null);
            }
        }

        _logger?.LogInformation($"{logHeader} Processing completed. Success: {successCount}, Errors: {errorCount}, Duplicates: {duplicateCount}");
    }

    private async Task ValidateDatabaseRecords()
    {
        await _mediaDatabaseService.ValidateDatabaseRecordsAsync();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Move methods inside the class
    // Переместить методы внутри класса
#if ANDROID
    private async Task<string> GetAndroidRealPath(IStorageFile file)
    {
        return await Task.Run(async () =>
        {
            try 
            {
                var uri = Android.Net.Uri.Parse(file.Path.AbsoluteUri);
                using var cursor = Android.App.Application.Context.ContentResolver.Query(
                    uri, null, null, null, null);
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

    private async Task<(string Artist, string Album, uint Year, string Genre)> GetAndroidTagInfo(string path)
    {
        try 
        {
            var track = new Track(path);
            return (
                track.Artist ?? "Unknown Artist",
                track.Album ?? "Unknown Album",
                (uint)(track.Year > 0 ? track.Year : DateTime.Now.Year),
                track.Genre ?? "Unknown Genre"
            );
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error reading ATL tags", ex);
            return ("Unknown Artist", "Unknown Album", (uint)DateTime.Now.Year, "Unknown Genre");
        }
    }

    private async Task<TimeSpan> GetDuration(string path)
    {
        try 
        {
            var track = new Track(path);
            return TimeSpan.FromMilliseconds(track.DurationMs);
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

    private static async Task<bool> FileExists(string path)
    {
        return await Task.Run(() => File.Exists(path));
    }

    private void ShowDbPath_Click(object sender, RoutedEventArgs e)
    {
        var path = _mediaDatabaseService.GetDatabasePath();

        _logger?.LogInformation($"Current database path: {path}");

        var vm = DataContext as MediaViewModel;
        vm?.UpdateStatusMessage?.Invoke($"Current database path: {path}");
    }

    private (string Artist, string Album, uint Year, string Genre, TimeSpan Duration) GetDesktopTagInfo(string path)
    {
        try
        {
            var track = new Track(path);
            return (
                track.Artist ?? "Unknown Artist",
                track.Album ?? "Unknown Album",
                (uint)(track.Year > 0 ? track.Year : DateTime.Now.Year),
                track.Genre ?? "Unknown Genre",
                TimeSpan.FromMilliseconds(track.DurationMs)
            );
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error reading ATL tags", ex);
            return ("Unknown Artist", "Unknown Album", (uint)DateTime.Now.Year, "Unknown Genre", TimeSpan.Zero);
        }
    }

    private static async Task<string> ProcessAndroidFile(string path)
    {
#if ANDROID
        var destFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AtuneMedia");
        Directory.CreateDirectory(destFolder);
        
        var destPath = Path.Combine(destFolder, Path.GetFileName(path));
        using (var source = File.OpenRead(path))
        using (var dest = File.Create(destPath))
        {
            await source.CopyToAsync(dest);
        }
        return destPath;
#else
        return await Task.FromResult(path);
#endif
    }

    private void PlayMediaItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is MediaItem mediaItem)
        {
            // Route through MainViewModel to clear and rebuild the playback queue
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            mainVm?.PlayAlbumFromTrackCommand.Execute(mediaItem);
        }
    }

    private void PlaylistContextMenu_Opened(object? sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu menu && menu.PlacementTarget is Button btn && btn.DataContext is MediaItem)
        {
            if (DataContext is MediaViewModel vm)
            {
                menu.Items.Clear();
                foreach (var pl in vm.Playlists)
                {
                    var item = new MenuItem
                    {
                        Header = pl.Name,
                        Command = vm.AddToPlaylistCommand,
                        CommandParameter = pl
                    };
                    menu.Items.Add(item);
                }
            }
        }
    }

    // Handle click on the ⋮ button to open the context menu
    private void PlaylistMenuButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            // Select this track so AddToPlaylistAsync uses the correct MediaItem
            if (DataContext is MediaViewModel vm && btn.DataContext is MediaItem track)
            {
                vm.SelectedMediaItem = track;
            }
            if (btn.ContextMenu is ContextMenu contextMenu)
            {
                contextMenu.PlacementTarget = btn;
                contextMenu.Open();
            }
        }
    }
}
