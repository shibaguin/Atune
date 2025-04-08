using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using Atune.Models;
using Microsoft.Extensions.Logging;

namespace Atune.Services
{
    // Сервис для опроса состояния воспроизведения: метаданные трека, позиция, длительность, громкость
    public class PlaybackMetadataService : INotifyPropertyChanged, IDisposable
    {
        private readonly MusicPlaybackService _playbackService;
        // Удалена зависимость от MediaDatabaseService, так как метаданные будут обновляться напрямую из плеера.
        // private readonly MediaDatabaseService _mediaDatabaseService;
        private readonly ILogger<PlaybackMetadataService> _logger;
        private readonly DispatcherTimer _timer;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Обновлённый конструктор без MediaDatabaseService
        public PlaybackMetadataService(MusicPlaybackService playbackService, ILogger<PlaybackMetadataService> logger)
        {
            _playbackService = playbackService;
            _logger = logger;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // обновляем каждые 500 мс
            };
            _timer.Tick += TimerTick;
            _timer.Start();
        }

        private async void TimerTick(object? sender, EventArgs e)
        {
            try 
            {
                if (CurrentTrack != null)
                {
                    // Получаем актуальные метаданные напрямую из плеера,
                    // а не из БД, чтобы избежать пустых значений.
                    var currentMetadata = await _playbackService.GetCurrentMetadataAsync();
                    if (currentMetadata != null)
                    {
                        _playbackService.UpdateCurrentTrack(currentMetadata);
                        OnPropertyChanged(nameof(CurrentTrack));
                        _logger.LogInformation("Playback metadata updated for track: {Title}", currentMetadata.Title);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating playback metadata");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Текущая позиция воспроизведения в виде TimeSpan
        public TimeSpan CurrentTime => _playbackService.CurrentTime;

        // Длительность текущего трека
        public TimeSpan Duration => TimeSpan.FromMilliseconds(_playbackService.GetDuration());

        // Текущая громкость (0–100)
        public int Volume
        {
            get => _playbackService.Volume;
            set
            {
                _logger.LogInformation("Setting volume to {Volume}", value);
                _playbackService.SetVolume(value);
                OnPropertyChanged(nameof(Volume));
            }
        }

        // Метаданные текущего трека, берутся из модели MediaItem напрямую из MusicPlaybackService.
        public MediaItem? CurrentTrack => _playbackService.CurrentTrack;

        // Метод перемотки: принимает новое время в миллисекундах
        public void Seek(TimeSpan time)
        {
            _logger.LogInformation("Seeking to {Time} ms", time.TotalMilliseconds);
            _playbackService.Seek((long)time.TotalMilliseconds);
            OnPropertyChanged(nameof(CurrentTime));
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= TimerTick;
        }
    }
} 