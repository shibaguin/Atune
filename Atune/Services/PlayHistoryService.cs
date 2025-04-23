namespace Atune.Services;

using System;
using System.Threading.Tasks;
using Atune.Data.Interfaces;
using Atune.Models;

public class PlayHistoryService : IDisposable
{
    private readonly IPlayHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MusicPlaybackService _playbackService;
    private readonly Guid _sessionId;
    private readonly string _deviceId;
    private readonly string _appVersion;
    private readonly string _os;

    public PlayHistoryService(
        IPlayHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        MusicPlaybackService playbackService)
    {
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _playbackService = playbackService;

        // Initialize session and device info
        _sessionId = Guid.NewGuid();
        _deviceId = Environment.MachineName;
        _appVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? string.Empty;
        _os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

        // Subscribe to playback events
        _playbackService.PlaybackEnded += OnPlaybackEnded;
    }

    private async void OnPlaybackEnded(object? sender, EventArgs e)
    {
        try
        {
            var track = _playbackService.CurrentTrack;
            if (track != null)
            {
                var duration = _playbackService.CurrentTime;
                var total = _playbackService.Duration;

                var entry = new PlayHistory
                {
                    MediaItemId = track.Id,
                    PlayedAt = DateTime.UtcNow,
                    DurationSeconds = (int)duration.TotalSeconds,
                    SessionId = _sessionId,
                    DeviceId = _deviceId,
                    PercentPlayed = total.TotalSeconds > 0
                        ? 100.0
                        : 0.0,
                    AppVersion = _appVersion,
                    OS = _os
                };

                await _historyRepository.AddAsync(entry);
                await _unitOfWork.CommitAsync();
            }
        }
        catch
        {
            // Swallow exceptions to avoid interrupting playback
        }
    }

    public void Dispose()
    {
        _playbackService.PlaybackEnded -= OnPlaybackEnded;
    }
} 