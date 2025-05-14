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
                if (platformPathService == null)
                {
                    Log.Warning("Failed to save playback state: PlatformPathService is not available");
                    return;
                }

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    && desktop.MainWindow?.DataContext is MainViewModel mainVm)
                {
                    var mediaVm = mainVm.MediaViewModelInstance;
                    if (mediaVm == null)
                    {
                        Log.Warning("Failed to save playback state: MediaViewModel is not available");
                        return;
                    }

                    int currentIndex = -1;
                    if (mediaVm.SelectedMediaItem != null)
                        currentIndex = mediaVm.PlaybackQueue.IndexOf(mediaVm.SelectedMediaItem);
                    if (currentIndex < 0 && mediaVm.CurrentQueueIndex >= 0)
                        currentIndex = mediaVm.CurrentQueueIndex;
                    double position = playbackService?.Position.TotalSeconds ?? 0;
                    bool isPaused = !(playbackService?.IsPlaying ?? false);

                    var filePath = platformPathService.GetSettingsPath("playbackstate.txt");
                    Log.Information("Saving playback state to {Path}", filePath);
                    var dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrWhiteSpace(dir))
                        Directory.CreateDirectory(dir);

                    try
                    {
                        using var writer = new StreamWriter(filePath);
                        foreach (var item in mediaVm.PlaybackQueue)
                        {
                            var path = item.Path?.Replace("\r", string.Empty).Replace("\n", string.Empty) ?? string.Empty;
                            writer.WriteLine(path.Replace("|", "\\|"));
                        }
                        writer.WriteLine($"__INDEX__:{currentIndex}");
                        writer.WriteLine($"__POSITION__:{position.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                        writer.WriteLine($"__PAUSED__:{isPaused}");

                        Log.Information("Playback state saved, exists={Exists}", File.Exists(filePath));
                    }
                    catch (IOException ex)
                    {
                        Log.Error(ex, "Failed to write playback state file: {Path}", filePath);
                    }
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
                var playbackService = services.GetService<IPlaybackService>();
                if (platformPathService == null)
                {
                    Log.Warning("Failed to restore playback state: PlatformPathService is not available");
                    return;
                }
                if (playbackService == null)
                {
                    Log.Warning("Failed to restore playback state: PlaybackService is not available");
                    return;
                }

                var filePath = platformPathService.GetSettingsPath("playbackstate.txt");
                Log.Information("Restoring playback state from {Path}", filePath);
                if (!File.Exists(filePath))
                {
                    Log.Warning("Playback state file not found: {Path}", filePath);
                    return;
                }

                string[] lines;
                try
                {
                    lines = await File.ReadAllLinesAsync(filePath);
                }
                catch (IOException ex)
                {
                    Log.Error(ex, "Failed to read playback state file: {Path}", filePath);
                    return;
                }

                var queuePaths = new System.Collections.Generic.List<string>();
                int stateIndex = -1;
                double statePos = 0;
                bool wasPaused = false;

                try
                {
                    foreach (var raw in lines)
                    {
                        if (raw.StartsWith("__INDEX__:"))
                            int.TryParse(raw["__INDEX__:".Length..], out stateIndex);
                        else if (raw.StartsWith("__POSITION__:"))
                            double.TryParse(raw["__POSITION__:".Length..], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out statePos);
                        else if (raw.StartsWith("__PAUSED__:"))
                            bool.TryParse(raw["__PAUSED__:".Length..], out wasPaused);
                        else
                            queuePaths.Add(raw.Replace("\\|", "|"));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to parse playback state file");
                    return;
                }

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    && desktop.MainWindow?.DataContext is MainViewModel mainVm)
                {
                    var mediaVm = mainVm.MediaViewModelInstance;
                    if (mediaVm == null)
                    {
                        Log.Warning("Failed to restore playback state: MediaViewModel is not available");
                        return;
                    }

                    try
                    {
                        // Очищаем текущую очередь
                        playbackService.ClearQueue();

                        // Восстанавливаем очередь
                        foreach (var path in queuePaths)
                        {
                            var item = mediaVm.MediaItems.FirstOrDefault(mi => mi.Path == path);
                            if (item != null)
                                playbackService.Enqueue(item);
                        }

                        // Если есть активный трек, восстанавливаем его воспроизведение
                        if (stateIndex >= 0 && stateIndex < queuePaths.Count)
                        {
                            try
                            {
                                // Устанавливаем позицию в очереди
                                await playbackService.PlayAtIndex(stateIndex);

                                // Даем время на инициализацию воспроизведения
                                await Task.Delay(500);

                                // Сначала устанавливаем позицию воспроизведения
                                playbackService.Position = TimeSpan.FromSeconds(statePos);

                                // Если трек был на паузе, ставим на паузу
                                if (wasPaused)
                                {
                                    playbackService.Pause();
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Failed to restore playback position and state");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to restore playback queue");
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