using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Atune.Models;
using Atune.ViewModels;
using Atune.Services;

namespace Atune.Startup
{
    public static class PlaybackStateManager
    {
        public static void SaveState(IServiceProvider services)
        {
            try
            {
                var platformPathService = services.GetService<IPlatformPathService>();
                var playbackService = services.GetService<MediaPlayerService>();
                if (platformPathService == null) return;

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    && desktop.MainWindow?.DataContext is MainViewModel mainVm)
                {
                    var mediaVm = mainVm.MediaViewModelInstance;
                    if (mediaVm == null) return;

                    int currentIndex = -1;
                    if (mediaVm.SelectedMediaItem != null)
                        currentIndex = mediaVm.PlaybackQueue.IndexOf(mediaVm.SelectedMediaItem);
                    if (currentIndex < 0 && mediaVm.CurrentQueueIndex >= 0)
                        currentIndex = mediaVm.CurrentQueueIndex;
                    double position = playbackService?.Position.TotalSeconds ?? 0;

                    var filePath = platformPathService.GetSettingsPath("playbackstate.txt");
                    Log.Information("Saving playback state to {Path}", filePath);
                    var dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrWhiteSpace(dir))
                        Directory.CreateDirectory(dir);

                    using var writer = new StreamWriter(filePath);
                    foreach (var item in mediaVm.PlaybackQueue)
                    {
                        var path = item.Path?.Replace("\r", string.Empty).Replace("\n", string.Empty) ?? string.Empty;
                        writer.WriteLine(path.Replace("|", "\\|"));
                    }
                    writer.WriteLine($"__INDEX__:{currentIndex}");
                    writer.WriteLine($"__POSITION__:{position.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

                    Log.Information("Playback state saved, exists={Exists}", File.Exists(filePath));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save playback state");
            }
        }

        public static async Task RestoreStateAsync(IServiceProvider services)
        {
            try
            {
                var platformPathService = services.GetService<IPlatformPathService>();
                if (platformPathService == null) return;

                var filePath = platformPathService.GetSettingsPath("playbackstate.txt");
                Log.Information("Restoring playback state from {Path}", filePath);
                if (!File.Exists(filePath))
                {
                    Log.Warning("Playback state file not found: {Path}", filePath);
                    return;
                }

                var lines = await File.ReadAllLinesAsync(filePath);
                var queuePaths = new System.Collections.Generic.List<string>();
                int stateIndex = -1;
                double statePos = 0;

                foreach (var raw in lines)
                {
                    if (raw.StartsWith("__INDEX__:"))
                        int.TryParse(raw["__INDEX__:".Length..], out stateIndex);
                    else if (raw.StartsWith("__POSITION__:"))
                        double.TryParse(raw["__POSITION__:".Length..], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out statePos);
                    else
                        queuePaths.Add(raw.Replace("\\|", "|"));
                }

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    && desktop.MainWindow?.DataContext is MainViewModel mainVm)
                {
                    mainVm.GoMediaCommand.Execute(null);
                    var mediaVm = mainVm.MediaViewModelInstance;
                    if (mediaVm == null) return;

                    mediaVm.ClearQueueCommand.Execute(null);
                    foreach (var path in queuePaths)
                    {
                        var item = mediaVm.MediaItems.FirstOrDefault(mi => mi.Path == path);
                        if (item != null)
                            mediaVm.AddToQueueCommand.Execute(item);
                    }

                    if (stateIndex >= 0 && stateIndex < mediaVm.PlaybackQueue.Count)
                        mediaVm.SetQueuePositionCommand.Execute(stateIndex + 1);

                    // Restore via playback engine interface
                    var engine = services.GetService<Atune.Services.IPlaybackEngineService>();
                    if (engine != null && stateIndex >= 0 && stateIndex < mediaVm.PlaybackQueue.Count)
                    {
                        var item = mediaVm.PlaybackQueue[stateIndex];
                        // Play via MediaViewModel command for proper integration
                        await mediaVm.PlayTrackCommand.ExecuteAsync(item);
                        // Allow time for PlaybackStarted to fire and UI to update
                        await Task.Delay(200);
                        // Pause and seek to saved position
                        engine.Pause();
                        engine.Position = TimeSpan.FromSeconds(statePos);
                        // No further UI updates required; events handled by MainViewModel
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in RestorePlaybackStateAsync");
            }
        }
    }
} 