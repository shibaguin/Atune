using LibVLCSharp.Shared;
using System;
using Atune.Exceptions;
using Microsoft.Extensions.Logging;

namespace Atune.Services
{
    public class MediaPlayerService : IDisposable
    {
        private LibVLC? _libVlc;
        private MediaPlayer? _player;
        private Media? _currentMedia;
        private int _volume = 50;
        private readonly ILogger<MediaPlayerService> _logger;
        
        public event EventHandler? PlaybackEnded;

        public MediaPlayerService(
            ISettingsService settingsService, 
            ILogger<MediaPlayerService> logger)
        {
            _logger = logger;
            
            try
            {
                _logger.LogInformation("Initializing LibVLC...");
                Core.Initialize();
                
                _libVlc = new LibVLC(enableDebugLogs: true);
                _logger.LogDebug("LibVLC instance created");

                _player = new MediaPlayer(_libVlc);
                _logger.LogInformation("MediaPlayer initialized successfully");

                _volume = settingsService.Volume;
                
                _player.EndReached += (s, e) => PlaybackEnded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize LibVLC");
                DisposeInternal();
                throw new MediaPlayerInitializationException(
                    "Failed to initialize media player components", ex);
            }
        }

        private void DisposeInternal()
        {
            _player?.Dispose();
            _libVlc?.Dispose();
            _player = null;
            _libVlc = null;
        }

        public void Play(string path)
        {
            if (_libVlc == null || _player == null)
                throw new InvalidOperationException("Media player is not initialized");

            Stop();
            _currentMedia = new Media(_libVlc, new Uri(path));
            _player.Media = _currentMedia;
            _player.Play();
        }

        public void Pause()
        {
            if (_player == null) return;
            _player.Pause();
        }
        
        public void Resume()
        {
            if (_player == null) return;
            _player.Play();
        }

        public void Stop()
        {
            if (_player == null) return;
            
            _player.Stop();
            _currentMedia?.Dispose();
            _currentMedia = null;
        }

        public bool IsPlaying => _player?.IsPlaying ?? false;

        public int Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                if (_player != null)
                    _player.Volume = value;
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (_player?.Media?.Duration == null) 
                    return TimeSpan.Zero;
                    
                return TimeSpan.FromMilliseconds(_player.Position * _player.Media.Duration);
            }
            set
            {
                if (_player?.Media?.Duration > 0)
                    _player.Position = (float)(value.TotalMilliseconds / _player.Media.Duration);
            }
        }

        public TimeSpan Duration => 
            _currentMedia != null ? 
            TimeSpan.FromMilliseconds(_currentMedia.Duration) : 
            TimeSpan.Zero;

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        private void OnPlaybackEnded(object? sender, EventArgs e)
        {
            PlaybackEnded?.Invoke(this, e);
        }
    }
} 