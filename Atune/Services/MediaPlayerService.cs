using LibVLCSharp.Shared;
using System;
using Atune.Exceptions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Atune.Services
{
    public class MediaPlayerService : IDisposable
    {
        private LibVLC? _libVlc;
        private MediaPlayer? _player;
        private Media? _currentMedia;
        private int _volume = 50;
        private readonly ILogger<MediaPlayerService> _logger;
        
        private const string NetworkCaching = ":network-caching=300";
        private const string RtspTcp = ":rtsp-tcp";

        public event EventHandler? PlaybackEnded;
        public event EventHandler? PlaybackStarted;
        public event EventHandler? PlaybackPaused;

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
            
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                var uri = new Uri(path);
                _currentMedia = new Media(_libVlc, uri, 
                    uri.Scheme.Equals("rtsp", StringComparison.OrdinalIgnoreCase) 
                        ? $"{NetworkCaching} {RtspTcp}" 
                        : NetworkCaching);
            }
            else
            {
                _currentMedia = new Media(_libVlc, path, FromType.FromPath);
            }

            _player.Media = _currentMedia;
            _player.Play();
            PlaybackStarted?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            if (_player == null) return;
            _player.Pause();
            PlaybackPaused?.Invoke(this, EventArgs.Empty);
        }
        
        public void Resume()
        {
            if (_player == null) return;
            _player.Play();
            PlaybackStarted?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            if (_player == null) return;
            
            try
            {
                _player.Stop();
                Task.Delay(100).Wait();
            }
            finally
            {
                _currentMedia?.Dispose();
                _currentMedia = null;
            }
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

        public bool IsNetworkStream => 
            _currentMedia?.Mrl != null && 
            Uri.IsWellFormedUriString(_currentMedia.Mrl, UriKind.Absolute);

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