namespace Atune.Services;

using System;
using System.Threading.Tasks;
using Atune.Data.Interfaces;
using Atune.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using Serilog;
using Microsoft.Extensions.DependencyInjection;

public class PlayHistoryService : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MediaPlayerService _playbackService;
    private readonly Guid _sessionId;
    private readonly string _deviceId;
    private readonly string _appVersion;
    private readonly string _os;
    private readonly ILogger<PlayHistoryService> _logger;

    public PlayHistoryService(
        IServiceScopeFactory scopeFactory,
        MediaPlayerService playbackService,
        ILogger<PlayHistoryService> logger)
    {
        _scopeFactory = scopeFactory;
        _playbackService = playbackService;
        _logger = logger;

        // Initialize session and device info
        _sessionId = Guid.NewGuid();
        _deviceId = Environment.MachineName;
        _logger.LogInformation("PlayHistoryService initialized: SessionId={SessionId}, Device={Device}", _sessionId, _deviceId);
        _appVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? string.Empty;
        _os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

        // Subscribe to playback events
        _playbackService.PlaybackEnded += OnPlaybackEnded;
    }

    private async void OnPlaybackEnded(object? sender, EventArgs e)
    {
        Log.Information("[PlayHistoryService] OnPlaybackEnded event received");
        _logger.LogInformation("PlaybackEnded event received");
        try
        {
            // Get the raw playback path and normalize to local file path
            var rawPath = _playbackService.CurrentPath ?? string.Empty;
            var path = rawPath;
            if (rawPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                try { path = new Uri(rawPath).LocalPath; }
                catch { /* leave rawPath if URI parsing fails */ }
            }
            Log.Information("[PlayHistoryService] Playback finished for path={Path}", rawPath);
            _logger.LogInformation("Playback finished for path: {Path}", path);
            if (string.IsNullOrEmpty(path))
                return;

            // Lookup media item by path with a new scope
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var historyRepository = scope.ServiceProvider.GetRequiredService<IPlayHistoryRepository>();
            var mediaItem = (await unitOfWork.Media.GetAllAsync())
                .FirstOrDefault(m => string.Equals(m.Path, path, StringComparison.OrdinalIgnoreCase));
            if (mediaItem == null)
            {
                Log.Warning("[PlayHistoryService] No MediaItem found for path={Path}", path);
                _logger.LogWarning("No MediaItem found for path: {Path}", path);
                return;
            }
            var durationSeconds = (int)_playbackService.Position.TotalSeconds;
            var totalSeconds = _playbackService.Duration.TotalSeconds;

            var entry = new PlayHistory
            {
                MediaItemId = mediaItem.Id,
                PlayedAt = DateTime.UtcNow,
                DurationSeconds = durationSeconds,
                SessionId = _sessionId,
                DeviceId = _deviceId,
                PercentPlayed = totalSeconds > 0
                    ? durationSeconds / totalSeconds * 100.0
                    : 0.0,
                AppVersion = _appVersion,
                OS = _os
            };

            Log.Information("[PlayHistoryService] Saving entry: MediaItemId={MediaItemId}, Duration={Duration}", entry.MediaItemId, entry.DurationSeconds);
            _logger.LogInformation("Saving history entry: MediaItemId={MediaItemId}, Duration={Duration}s, PercentPlayed={PercentPlayed}%", entry.MediaItemId, entry.DurationSeconds, entry.PercentPlayed);
            await historyRepository.AddAsync(entry);
            await unitOfWork.CommitAsync();
            Log.Information("[PlayHistoryService] Entry committed to database");
            _logger.LogInformation("Play history entry committed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[PlayHistoryService] Error recording play history");
            _logger.LogError(ex, "Failed to record play history");
        }
    }

    public void Dispose()
    {
        _playbackService.PlaybackEnded -= OnPlaybackEnded;
    }
}