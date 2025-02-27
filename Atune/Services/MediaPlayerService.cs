using LibVLCSharp.Shared;
using System;
using Atune.Exceptions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Threading;

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
                    enableDebugLogs: false,
                    "--avcodec-hw=none",
                    "--no-xlib",
                    "--ignore-config",
                    "--no-sub-autodetect-file",
                    "--network-caching=5000",
                    "--file-caching=3000",
                    "--no-audio-time-stretch"
                );
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

        public async Task Play(string path)
        {
            if (_libVlc == null || _player == null)
                throw new InvalidOperationException("Media player is not initialized");

            try
            {
                _currentMedia?.Dispose();
                _currentMedia = new Media(_libVlc, new Uri(path));
                
                // Асинхронная подготовка медиа
                await Task.Run(() => {
                    _currentMedia.Parse(MediaParseOptions.ParseLocal | MediaParseOptions.FetchLocal);
                    while (!_currentMedia.IsParsed)
                    {
                        Task.Delay(10).Wait(); // Уменьшаем задержку
                        _currentMedia.Parse(MediaParseOptions.ParseLocal);
                    }
                });

                // Асинхронное воспроизведение
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    _player.Play(_currentMedia);
                    Volume = _volume; // Применяем текущую громкость
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Playback error");
                throw new MediaPlaybackException("Failed to play media", ex);
            }
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
                Task.Delay(50).Wait();
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

        public string? CurrentPath => _currentMedia?.Mrl;

        public async Task<MediaMetadata> GetCurrentMetadataAsync()
        {
            if (_currentMedia == null) 
                return new MediaMetadata { Title = "Нет данных", Artist = "Нет данных" };

            return await Task.Run(() =>
            {
                try
                {
                    return new MediaMetadata {
                        Title = _currentMedia.Meta(MetadataType.Title) ?? Path.GetFileNameWithoutExtension(_currentMedia.Mrl),
                        Artist = _currentMedia.Meta(MetadataType.Artist) ?? "Неизвестный исполнитель"
                    };
                }
                catch
                {
                    return new MediaMetadata { Title = "Ошибка", Artist = "Ошибка" };
                }
            });
        }

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        public LibVLC? GetLibVlc() => _libVlc;
        public Media? GetCurrentMedia() => _currentMedia;

        private void OnPlaybackEnded(object? sender, EventArgs e)
        {
            PlaybackEnded?.Invoke(this, e);
        }

        public async Task StopAsync()
        {
            if (_player == null) return;
            
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                try
                {
                    _player.Stop();
                    _currentMedia?.Dispose();
                    _currentMedia = null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stop error");
                }
            });
        }
    }

    public class MediaPlaybackException : Exception
    {
        public MediaPlaybackException(string message, Exception inner) 
            : base(message, inner) { }
    }
} 