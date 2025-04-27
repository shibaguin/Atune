using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atune.Models;

namespace Atune.Services
{
    public interface IPlaybackService : IDisposable
    {
        // Queue management
        void ClearQueue();
        void Enqueue(MediaItem item);
        void Enqueue(IEnumerable<MediaItem> items);

        // Playback control
        Task Play();
        Task Play(MediaItem item);
        Task Next();
        Task Previous();
        void Pause();
        void Resume();
        void Stop();

        // Playback properties
        TimeSpan Position { get; set; }
        TimeSpan Duration { get; }
        int Volume { get; set; }

        // Events for UI/ViewModels
        event EventHandler<MediaItem?> TrackChanged;
        event EventHandler<bool> PlaybackStateChanged;
        event EventHandler<TimeSpan> PositionChanged;
        event EventHandler<IReadOnlyList<MediaItem>> QueueChanged;

        // Engine events (legacy)
        event EventHandler? PlaybackStarted;
        event EventHandler? PlaybackPaused;
        event EventHandler? PlaybackEnded;

        // Convenience read-only view of the queue
        IReadOnlyList<MediaItem> GetQueue();

        // Current playback info (legacy)
        bool IsPlaying { get; }
        TimeSpan CurrentTime { get; }
        MediaItem? CurrentTrack { get; }

        // Current media path (engine provider)
        string? CurrentPath { get; }
    }
}