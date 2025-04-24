using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atune.Models;

namespace Atune.Services
{
    public interface IPlaybackService : IDisposable
    {
        event EventHandler? PlaybackStarted;
        event EventHandler? PlaybackPaused;
        event EventHandler? PlaybackEnded;

        void Enqueue(MediaItem track);
        void Enqueue(IEnumerable<MediaItem> tracks);
        void ClearQueue();
        IReadOnlyList<MediaItem> GetQueue();

        Task PlayAsync();
        Task Play(string path);
        Task StopAsync();
        Task NextAsync();
        Task PreviousAsync();

        bool IsPlaying { get; }
        int Volume { get; set; }
        TimeSpan CurrentTime { get; }
        MediaItem? CurrentTrack { get; }
    }
} 