using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Atune.Models;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System.Linq;
using ATL;
using Microsoft.Extensions.Caching.Memory;

namespace Atune.Services
{
    // Сервис для воспроизведения музыки с использованием VLC и поддержкой очереди воспроизведения.
    public class MusicPlaybackService : IDisposable
    {
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;
        private readonly List<MediaItem> _playbackQueue;
        private int _currentIndex;
        private Media? _currentMedia;
        private int _volume = 50;
        private readonly ILogger<MusicPlaybackService> _logger;
        private readonly IMemoryCache _cache;

        // Добавляем необходимые события для уведомления о начале, паузе и окончании воспроизведения.
        public event EventHandler? PlaybackStarted;
        public event EventHandler? PlaybackPaused;
        public event EventHandler? PlaybackEnded;

        // Добавляем свойство для хранения метаданных текущего трека.
        public MediaItem? CurrentTrack { get; private set; }
        public TimeSpan CurrentTime { get; private set; }
        public int Volume { get; private set; }

        // Конструктор – инициализируем LibVLC и создаём MediaPlayer, а также подписываемся на событие окончания трека.
        public MusicPlaybackService(ILogger<MusicPlaybackService> logger, IMemoryCache cache)
        {
            // Инициализация библиотеки VLC (вызывается один раз в приложении).
            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.EndReached += OnMediaEndReached;
            _playbackQueue = new List<MediaItem>();
            _currentIndex = 0;
            _logger = logger;
            _cache = cache;

            // Подписываемся на событие окончания трека и оповещаем подписчиков.
            _mediaPlayer.EndReached += (sender, args) =>
            {
                PlaybackEnded?.Invoke(this, EventArgs.Empty);
            };
        }

        // Добавление одного трека в очередь
        public void Enqueue(MediaItem track)
        {
            if (track != null)
            {
                _playbackQueue.Add(track);
            }
        }

        // Добавление списка треков в очередь
        public void Enqueue(IEnumerable<MediaItem> tracks)
        {
            if (tracks != null)
            {
                _playbackQueue.AddRange(tracks);
            }
        }

        // Получение текущей очереди воспроизведения (только для чтения)
        public IReadOnlyList<MediaItem> GetQueue() => _playbackQueue.AsReadOnly();

        // Очистка очереди
        public void ClearQueue()
        {
            _playbackQueue.Clear();
            _currentIndex = 0;
        }

        // Запуск воспроизведения текущего трека из очереди
        public async Task PlayAsync()
        {
            if (_playbackQueue.Count == 0)
                return;

            // Если индекс выходит за пределы очереди, возвращаемся в начало
            if (_currentIndex < 0 || _currentIndex >= _playbackQueue.Count)
                _currentIndex = 0;

            MediaItem currentTrack = _playbackQueue[_currentIndex];

            // Создаем объект Media, используя путь к файлу трека
            using var media = new Media(_libVLC, currentTrack.Path, FromType.FromPath);

            // Запускаем воспроизведение
            _mediaPlayer.Play(media);

            // Если потребуется ожидание начала воспроизведения, можно добавить дополнительную логику
            await Task.CompletedTask;
        }

        // Метод для запуска воспроизведения по пути к файлу.
        public async Task Play(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            
            _logger.LogInformation("Starting playback for media: {Path}", path);
            try
            {
                // Проверка и инициализация плеера
                if (_libVLC == null || _mediaPlayer == null)
                    throw new InvalidOperationException("Media player is not initialized");

                _currentMedia?.Dispose();
                _currentMedia = new Media(_libVLC, new Uri(path));

                // Парсинг медиа (базовый)
                await Task.Run(() => _currentMedia.Parse(MediaParseOptions.ParseLocal));

                // Получаем метаданные через новый метод (асинхронно)
                var metadata = await ExtractMetadataAsync(path);
                
                // Запускаем воспроизведение на UI‑потоке.
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _mediaPlayer.Play(_currentMedia);
                    Volume = _volume; // Применяем текущую громкость
                    PlaybackStarted?.Invoke(this, EventArgs.Empty);
                });

                // Обновляем метаданные текущего трека на основе извлечённых данных
                CurrentTrack = new MediaItem(
                    title: metadata.Title,
                    album: new Album { Title = metadata.Album ?? "Unknown Album" },
                    year: uint.TryParse(metadata.Year, out uint years) ? years : 0,
                    genre: metadata.Genre ?? "Unknown Genre",
                    path: path,
                    duration: TimeSpan.FromMilliseconds(_currentMedia.Duration),
                    trackArtists: new List<TrackArtist>
                    {
                        new TrackArtist 
                        { 
                            Artist = new Artist { Name = metadata.Artist ?? "Unknown Artist" }
                        }
                    });
                
                // Логирование успешного старта воспроизведения с правильными метаданными
                var artistNames = string.Join(", ", CurrentTrack.TrackArtists
                                    .Select(ta => ta.Artist?.Name)
                                    .Where(name => !string.IsNullOrEmpty(name))
                                    .Select(name => name!));
                _logger.LogInformation("Playback started successfully for media: File={Path}, Title={Title}, Album={Album}, Artist(s)={Artists}", 
                    path, CurrentTrack.Title, CurrentTrack.Album?.Title ?? "Unknown Album", artistNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play media: {Path}", path);
                throw new MediaPlaybackException("Failed to play media", ex);
            }
        }

        // Метод для постановки на паузу.
        public void Pause()
        {
            if (_mediaPlayer == null)
                return;
            _logger.LogInformation("Pausing playback");
            _mediaPlayer.Pause();
            PlaybackPaused?.Invoke(this, EventArgs.Empty);
        }

        // Метод для возобновления воспроизведения.
        public void Resume()
        {
            _logger.LogInformation("Resuming playback");
            _mediaPlayer?.Play();
            PlaybackStarted?.Invoke(this, EventArgs.Empty);
        }

        // Метод для остановки воспроизведения.
        public void Stop()
        {
            if (_mediaPlayer == null)
                return;
            _mediaPlayer.Stop();
            _currentMedia?.Dispose();
            _currentMedia = null;
        }

        // Свойство, возвращающее состояние воспроизведения.
        public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;

        // Свойство для получения/установки текущей позиции воспроизведения.
        public TimeSpan Position
        {
            get => TimeSpan.FromMilliseconds(_mediaPlayer?.Time ?? 0);
            set
            {
                if (_mediaPlayer != null)
                    _mediaPlayer.Time = (long)value.TotalMilliseconds;
            }
        }

        // Свойство для получения длительности текущего трека.
        public TimeSpan Duration => TimeSpan.FromMilliseconds(_mediaPlayer?.Length ?? 0);

        // Метод для перемотки на указанное время в миллисекундах.
        public void Seek(long milliseconds)
        {
            _logger.LogInformation("Seeking to {Milliseconds} ms", milliseconds);
            if (_mediaPlayer != null)
                _mediaPlayer.Time = milliseconds;
        }

        // Методы для получения времени и длительности (используются в PlaybackMetadataService)
        public double GetCurrentTime() => _mediaPlayer?.Time ?? 0;
        public double GetDuration() => _mediaPlayer?.Length ?? 0;

        // Метод для получения актуальных метаданных текущего трека.
        public Task<MediaItem?> GetCurrentMetadataAsync() => Task.FromResult(CurrentTrack);

        // Обработчик события окончания воспроизведения трека – автоматическая смена на следующий трек
        private async void OnMediaEndReached(object? sender, EventArgs e)
        {
            await NextAsync();
        }

        // Переход к следующему треку в очереди и запуск воспроизведения
        public async Task NextAsync()
        {
            if (_playbackQueue.Count == 0)
                return;

            _currentIndex++;
            if (_currentIndex >= _playbackQueue.Count)
                _currentIndex = 0; // Зацикливание очереди (можно изменить на остановку)

            await PlayAsync();
        }

        // Переход к предыдущему треку в очереди и запуск воспроизведения
        public async Task PreviousAsync()
        {
            if (_playbackQueue.Count == 0)
                return;

            _currentIndex--;
            if (_currentIndex < 0)
                _currentIndex = _playbackQueue.Count - 1; // Зацикливание

            await PlayAsync();
        }

        // Освобождение ресурсов
        public void Dispose()
        {
            _mediaPlayer.EndReached -= OnMediaEndReached;
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
            _currentMedia?.Dispose();
        }

        // Получить и установить громкость
        public int GetVolume() => _mediaPlayer.Volume;
        public void SetVolume(int value)
        {
            _logger.LogInformation("Setting volume to {Volume}", value);
            if (_mediaPlayer != null)
                _mediaPlayer.Volume = value;
            _volume = value;
        }

        // Добавляем публичный метод для обновления текущего трека
        public void UpdateCurrentTrack(MediaItem? track)
        {
            CurrentTrack = track;
        }

        // Добавьте в класс MusicPlaybackService новый метод для извлечения метаданных.
        // Пример использования ATL (или другого метода) для асинхронного получения данных.
        private Task<MediaMetadata> ExtractMetadataAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Task.FromResult(new MediaMetadata
                {
                    Title = string.Empty,        // или, например, "Unknown Title"
                    Artist = "Unknown Artist",
                    Album = "Unknown Album",
                    Genre = "Unknown Genre",
                    Year = "0"
                });

            string cacheKey = "MusicPlaybackService_Metadata_" + path;
            if (_cache.TryGetValue(cacheKey, out MediaMetadata cachedMetadata) && cachedMetadata != null)
            {
                return Task.FromResult(cachedMetadata);
            }

            try
            {
                var track = new ATL.Track(path);
                MediaMetadata metadata = new MediaMetadata
                {
                    Title = string.IsNullOrWhiteSpace(track.Title)
                                ? System.IO.Path.GetFileNameWithoutExtension(path)
                                : track.Title!,
                    Artist = track.Artist ?? "Unknown Artist",
                    Album = track.Album ?? "Unknown Album",
                    Genre = track.Genre ?? "Unknown Genre",
                    Year = track.Year > 0 ? track.Year.ToString() : "0"
                };

                _cache.Set(cacheKey, metadata, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10)));
                return Task.FromResult(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting metadata from file");
                return Task.FromResult(new MediaMetadata
                {
                    Title = System.IO.Path.GetFileNameWithoutExtension(path),
                    Artist = "Unknown Artist",
                    Album = "Unknown Album",
                    Genre = "Unknown Genre",
                    Year = "0"
                });
            }
        }

        public Task<MediaMetadata> GetMetadataFromPathAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Task.FromResult(new MediaMetadata());
            
            string cacheKey = "MusicPlaybackService_Metadata_" + path;
            if (_cache.TryGetValue(cacheKey, out MediaMetadata? cachedMetadata) && cachedMetadata is not null)
            {
                return Task.FromResult(cachedMetadata);
            }
            
            try
            {
                var track = new ATL.Track(path);
                MediaMetadata metadata = new MediaMetadata
                {
                    Title = string.IsNullOrWhiteSpace(track.Title)
                        ? System.IO.Path.GetFileNameWithoutExtension(path)
                        : track.Title!,
                    Artist = track.Artist ?? "Unknown Artist",
                    Album = track.Album ?? "Unknown Album",
                    Genre = track.Genre ?? "Unknown Genre",
                    Year = track.Year > 0 ? track.Year.ToString() : "0"
                };

                _cache.Set(cacheKey, metadata, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10)));
                return Task.FromResult(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting metadata from file");
                return Task.FromResult(new MediaMetadata
                {
                    Title = System.IO.Path.GetFileNameWithoutExtension(path),
                    Artist = "Unknown Artist",
                    Album = "Unknown Album",
                    Genre = "Unknown Genre",
                    Year = "0"
                });
            }
        }
    }
} 