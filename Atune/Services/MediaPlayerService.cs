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
using Atune.Services;

namespace Atune.Services
{
    public class MediaPlayerService : IPlaybackEngineService
    {
        private ILibVLC? _libVlc;
        private IMediaPlayer? _player;
        private IMedia? _currentMedia;
        private int _volume = 50;
        private readonly ILogger<MediaPlayerService> _logger;
        private readonly ISettingsService _settingsService;
        private readonly ILibVLCFactory _libVlcFactory;
        private readonly IMediaFactory _mediaFactory;
        private readonly IMediaPlayerFactory _mediaPlayerFactory;
        private readonly IDispatcherService _dispatcher;
        private readonly IMediaPlayer? _preloadPlayer;
        private IMedia? _preloadMedia;

        public event EventHandler? PlaybackEnded;
        public event EventHandler? PlaybackStarted;
        public event EventHandler? PlaybackPaused;

        public MediaPlayerService(
            ISettingsService settingsService,
            ILogger<MediaPlayerService> logger,
            ILibVLCFactory libVlcFactory,
            IMediaFactory mediaFactory,
            IMediaPlayerFactory mediaPlayerFactory,
            IDispatcherService dispatcher)
        {
            _settingsService = settingsService;
            _logger = logger;
            _libVlcFactory = libVlcFactory;
            _mediaFactory = mediaFactory;
            _mediaPlayerFactory = mediaPlayerFactory;
            _dispatcher = dispatcher;

            try
            {
                _logger.LogInformation("Creating LibVLC instance...");
                _libVlc = _libVlcFactory.Create();
                _logger.LogDebug("LibVLC instance created");

                _player = _mediaPlayerFactory.Create(_libVlc!);
                _preloadPlayer = _mediaPlayerFactory.Create(_libVlc!);
                if (_preloadPlayer != null) _preloadPlayer.Mute = true;

                _logger.LogInformation("MediaPlayer initialized successfully");

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
            var lib = _libVlc ?? throw new InvalidOperationException("Media player is not initialized");
            var player = _player ?? throw new InvalidOperationException("Media player is not initialized");
            _currentMedia?.Dispose();
            try
            {
                // Build URI: for local file paths, convert to file:// URI; otherwise use provided URI
                Uri mediaUri;
                if (!Uri.IsWellFormedUriString(path, UriKind.Absolute))
                {
                    var fullPath = Path.GetFullPath(path);
                    if (OperatingSystem.IsWindows())
                    {
                        // На Windows используем специальный формат для локальных файлов
                        mediaUri = new Uri($"file:///{fullPath.Replace("\\", "/")}");
                    }
                    else
                    {
                        // На Unix-системах и Android используем стандартный формат
                        mediaUri = new Uri($"file://{fullPath}");
                    }
                }
                else
                {
                    mediaUri = new Uri(path, UriKind.Absolute);
                }
                var media = _mediaFactory.Create(lib, mediaUri);
                _currentMedia = media;
                // Parse metadata and buffer
                await Task.Run(() => media.Parse(MediaParseOptions.ParseLocal));
                // Enqueue play on UI thread
                await _dispatcher.InvokeAsync(() => player.Play(media));
                // Immediately notify that playback has started
                PlaybackStarted?.Invoke(this, EventArgs.Empty);
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
            if (_currentMedia is not { } currentMedia)
            {
                return new MediaMetadata { Title = "Нет данных", Artist = "Нет данных" };
            }

            return await Task.Run(() =>
            {
                try
                {
                    var title = currentMedia.Meta(MetadataType.Title)
                        ?? Path.GetFileNameWithoutExtension(currentMedia.Mrl)
                        ?? string.Empty;
                    var artist = currentMedia.Meta(MetadataType.Artist) ?? "Неизвестный исполнитель";

                    return new MediaMetadata
                    {
                        Title = title,
                        Artist = artist
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

        public ILibVLC? GetLibVlc() => _libVlc;
        public IMedia? GetCurrentMedia() => _currentMedia;

        private void OnPlaybackEnded(object? sender, EventArgs e)
        {
            PlaybackEnded?.Invoke(this, e);
        }

        public async Task StopAsync()
        {
            if (_player == null) return;

            await _dispatcher.InvokeAsync(() =>
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

        public async Task Preload(string path, int bufferMilliseconds = 1500)
        {
            var lib = _libVlc;
            var preloadPlayer = _preloadPlayer;
            if (lib == null || preloadPlayer == null)
                return;
            _preloadMedia?.Dispose();
            // Build URI for Preload
            Uri mediaUri;
            if (!Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                var fullPath = Path.GetFullPath(path);
                mediaUri = new Uri($"file:///{fullPath.Replace('\\', '/')}");
            }
            else
            {
                mediaUri = new Uri(path, UriKind.Absolute);
            }
            var media = _mediaFactory.Create(lib, mediaUri);
            _preloadMedia = media;
            try
            {
                await Task.Run(() => media.Parse(MediaParseOptions.ParseLocal));
                await _dispatcher.InvokeAsync(() =>
                {
                    preloadPlayer.Media = media;
                    preloadPlayer.Play(media);
                });
                await Task.Delay(bufferMilliseconds);
                await _dispatcher.InvokeAsync(() => preloadPlayer.Pause());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preloading media");
            }
        }

        public async Task Load(string path)
        {
            var lib = _libVlc ?? throw new InvalidOperationException("Media player is not initialized");
            var player = _player ?? throw new InvalidOperationException("Media player is not initialized");
            _currentMedia?.Dispose();
            // Build URI for Load
            Uri mediaUri;
            if (!Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                var fullPath = Path.GetFullPath(path);
                mediaUri = new Uri($"file:///{fullPath.Replace('\\', '/')}");
            }
            else
            {
                mediaUri = new Uri(path, UriKind.Absolute);
            }
            var media = _mediaFactory.Create(lib, mediaUri);
            _currentMedia = media;
            try
            {
                await Task.Run(() => media.Parse(MediaParseOptions.ParseLocal));
                await _dispatcher.InvokeAsync(() => player.Media = media);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Media load error");
                throw new MediaPlaybackException("Failed to load media", ex);
            }
        }
    }

    public class MediaPlaybackException(string message, Exception inner) : Exception(message, inner)
    {
    }
}
