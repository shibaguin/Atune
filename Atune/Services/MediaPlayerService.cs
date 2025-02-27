using LibVLCSharp.Shared;
using System;
using Atune.Exceptions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

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
                
                _libVlc = new LibVLC(
                    enableDebugLogs: true,
                    "--avcodec-hw=none",
                    "--no-xlib",
                    "--ignore-config",
                    "--no-sub-autodetect-file");
                _logger.LogDebug("LibVLC instance created");

                _player = new MediaPlayer(_libVlc);
                _logger.LogInformation("MediaPlayer initialized successfully");

                _volume = settingsService.Volume;
                
                _player.EndReached += (s, e) => PlaybackEnded?.Invoke(this, EventArgs.Empty);
                _player.Playing += (s, e) => PlaybackStarted?.Invoke(this, EventArgs.Empty);
                _player.Paused += (s, e) => PlaybackPaused?.Invoke(this, EventArgs.Empty);
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
            
            // Создаем медиа объект и парсим синхронно
            _currentMedia = Uri.IsWellFormedUriString(path, UriKind.Absolute) 
                ? new Media(_libVlc, new Uri(path)) 
                : new Media(_libVlc, path, FromType.FromPath);
            
            // Принудительный парсинг перед воспроизведением
            _currentMedia.Parse(MediaParseOptions.ParseLocal | MediaParseOptions.FetchLocal);
            while (!_currentMedia.IsParsed) 
            {
                Task.Delay(100).Wait();
                _currentMedia.Parse(MediaParseOptions.ParseLocal);
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

        public string? GetCoverArtPath()
        {
            if (_currentMedia == null) return null;
            
            _currentMedia.Parse(MediaParseOptions.ParseLocal 
                | MediaParseOptions.FetchLocal 
                | MediaParseOptions.FetchNetwork);
            
            if (!_currentMedia.IsParsed) return null;
            
            var artUrl = _currentMedia.Meta(MetadataType.ArtworkURL) 
                       ?? _currentMedia.Meta(MetadataType.Publisher);
            
            try 
            {
                if (!string.IsNullOrEmpty(artUrl))
                {
                    if (Uri.TryCreate(artUrl, UriKind.Absolute, out var uri))
                    {
                        return uri.IsFile && File.Exists(uri.LocalPath) 
                            ? uri.LocalPath 
                            : null;
                    }
                    return File.Exists(artUrl) ? artUrl : null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error accessing cover art path");
            }
            return null;
        }

        public Stream? GetEmbeddedCoverArt()
        {
            if (_currentMedia == null || _libVlc == null) return null;
            
            try 
            {
                _currentMedia.Parse(MediaParseOptions.ParseLocal);
                var artData = _currentMedia.Meta(MetadataType.ArtworkURL);
                
                if (!string.IsNullOrEmpty(artData))
                {
                    if (artData.StartsWith("data:image"))
                    {
                        var base64Data = artData.Split(',')[1];
                        return new MemoryStream(Convert.FromBase64String(base64Data));
                    }
                    
                    if (File.Exists(artData))
                    {
                        return File.OpenRead(artData);
                    }
                }
                
                // Быстрый снимок первого кадра
                using var mp = new MediaPlayer(_libVlc);
                mp.Media = _currentMedia;
                mp.Play();
                
                var tempFile = Path.GetTempFileName() + ".png";
                if (mp.TakeSnapshot(0u, tempFile, 200u, 200u))
                {
                    var imageBytes = File.ReadAllBytes(tempFile);
                    File.Delete(tempFile);
                    return new MemoryStream(imageBytes);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting embedded cover art");
                return null;
            }
        }

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