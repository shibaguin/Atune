using System;
using System.Threading.Tasks;
using Atune.Models;

namespace Atune.Services
{
    public interface IPlaybackEngineService : IDisposable
    {
        event EventHandler? PlaybackEnded;
        event EventHandler? PlaybackStarted;
        event EventHandler? PlaybackPaused;

        Task Play(string path);
        Task StopAsync();
        void Pause();
        void Resume();

        bool IsPlaying { get; }
        int Volume { get; set; }
        TimeSpan Position { get; set; }
        TimeSpan Duration { get; }
        bool IsNetworkStream { get; }
        string? CurrentPath { get; }

        Task<MediaMetadata> GetCurrentMetadataAsync();
        ILibVLC? GetLibVlc();
        IMedia? GetCurrentMedia();
    }
} 