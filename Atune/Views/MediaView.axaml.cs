using Avalonia.Controls;
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
#if ANDROID
using Android.Net;
using Android.App;
using Android.Provider;
using Android.Util;
#endif
using ATL;
using Microsoft.Extensions.Logging;
using Atune.Services;

namespace Atune.Views;

public partial class MediaView : UserControl
{
    private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;
    private readonly ILoggerService? _logger;

    public MediaView()
    {
        InitializeComponent();
    }
    
    public MediaView(MediaViewModel vm, IDbContextFactory<AppDbContext> dbContextFactory, ILoggerService logger) : this()
    {
        DataContext = vm;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    private async void AddToLibrary_Click(object sender, RoutedEventArgs e)
    {
        const string logHeader = "[MediaView]";
        _logger?.LogInformation($"{logHeader} Button clicked");
        
        var dbContext = _dbContextFactory?.CreateDbContext();
        if (dbContext is null)
        {
            _logger?.LogError($"{logHeader} Failed to create database context");
            return;
        }
        using (dbContext)
        {
            try
            {
                // Add a check for the database availability
                if (!await dbContext.Database.CanConnectAsync())
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
                            // Form the path to the AtuneMedia folder inside MyDocuments
                            var destFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AtuneMedia");
                            if (!Directory.Exists(destFolder))
                                Directory.CreateDirectory(destFolder);
                            // Form the destination path with the file name
                            var destPath = Path.Combine(destFolder, file.Name);
                            // Copy the file using a stream
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
                        }
                        
                        _logger?.LogInformation($"{logHeader} Processing file: {realPath}");
                        
                        if (await dbContext.ExistsByPathAsync(realPath))
                        {
                            duplicateCount++;
                            continue;
                        }
                        
                        // For desktop systems, check if the file exists
                        if (!OperatingSystem.IsAndroid())
                        {
                            if (!System.IO.File.Exists(realPath))
                            {
                                _logger?.LogWarning($"{logHeader} File does not exist: {realPath}");
                                errorCount++;
                                continue;
                            }
                        }
                        else 
                        {
                            // Original processing for Android
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

                        // For Android, you can use the universal method GetDesktopTagInfo,
                        // since now realPath points to the locally copied file
                        var tagInfo = GetDesktopTagInfo(realPath);
#if ANDROID
                        Android.Util.Log.Debug("MediaView", $"Tags received: Artist={tagInfo.Artist}, Album={tagInfo.Album}, Year={tagInfo.Year}");
#endif
                        var duration = tagInfo.Duration;
                       
                        var mediaItem = new MediaItem(
                            Path.GetFileNameWithoutExtension(file.Name),
                            tagInfo.Artist ?? "Unknown Artist",
                            tagInfo.Album ?? "Unknown Album",
                            tagInfo.Year,
                            tagInfo.Genre ?? "Unknown Genre",
                            realPath,
                            duration
                        );
                        await dbContext.AddMediaAsync(mediaItem);
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
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.LogError($"{logHeader} Critical error", ex);
                }
            }
        }
    }

    private async Task ValidateDatabaseRecords()
    {
        if (_dbContextFactory is null) return;

        await using var dbContext = _dbContextFactory.CreateDbContext();
        if (dbContext is null) return;

        var invalidRecords = await dbContext.MediaItems
            .Where(m => string.IsNullOrEmpty(m.Path) || !File.Exists(m.Path))
            .ToListAsync();

        foreach (var record in invalidRecords)
        {
            dbContext.MediaItems.Remove(record);
        }

        await dbContext.SaveChangesAsync();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Move methods inside the class
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

    private async Task<bool> FileExists(string path)
    {
        #if ANDROID
        return await Task.Run(() => System.IO.File.Exists(path));
        #else
        return await Task.FromResult(System.IO.File.Exists(path));
        #endif
    }

    private void ShowDbPath_Click(object sender, RoutedEventArgs e)
    {
        // Use the factory to create the context
        if (_dbContextFactory is null)
        {
            _logger?.LogWarning("DbContextFactory is not available");
            return;
        }

        using var db = _dbContextFactory.CreateDbContext();
        var path = db?.Database.GetDbConnection().DataSource ?? "not defined";
        
        if (_logger != null)
        {
            _logger.LogInformation($"Current database path: {path}");
        }
        
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

    private async Task<string> ProcessAndroidFile(string path)
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
}