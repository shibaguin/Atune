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
        private readonly MediaDatabaseService _mediaDatabaseService;
        private readonly ILogger<PlaybackMetadataService> _logger;
        private readonly DispatcherTimer _timer;

        public event PropertyChangedEventHandler? PropertyChanged;

        public PlaybackMetadataService(MusicPlaybackService playbackService, MediaDatabaseService mediaDatabaseService, ILogger<PlaybackMetadataService> logger)
        {
            _playbackService = playbackService;
            _mediaDatabaseService = mediaDatabaseService;
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
                if (CurrentTrack != null && !string.IsNullOrEmpty(CurrentTrack.Path))
                {
                    var updatedTrack = await _mediaDatabaseService.GetMediaItemByPathAsync(CurrentTrack.Path);
                    if (updatedTrack != null)
                    {
                        _playbackService.UpdateCurrentTrack(updatedTrack!);
                        OnPropertyChanged(nameof(CurrentTrack));
                        _logger.LogInformation("Playback metadata updated for track: {Title}", updatedTrack.Title);
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

        // Метаданные текущего трека, берутся из модели MediaItem
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