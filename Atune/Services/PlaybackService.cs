using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atune.Models;
using Avalonia.Threading;

namespace Atune.Services
{
    public class PlaybackService : IPlaybackService
    {
        private readonly IPlaybackEngineService _engine;
        private readonly List<MediaItem> _queue = new List<MediaItem>();
        private int _currentIndex;
        private readonly DispatcherTimer _positionTimer;

        public event EventHandler<MediaItem?> TrackChanged;
        public event EventHandler<bool> PlaybackStateChanged;
        public event EventHandler<TimeSpan> PositionChanged;
        public event EventHandler<IReadOnlyList<MediaItem>> QueueChanged;

        public PlaybackService(IPlaybackEngineService engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _engine.PlaybackStarted += OnEngineStarted;
            _engine.PlaybackPaused  += OnEnginePaused;
            _engine.PlaybackEnded   += async (_, __) => await Next();

            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _positionTimer.Tick += (_, __) => PositionChanged?.Invoke(this, Position);
        }

        public void ClearQueue()
        {
            _queue.Clear(); _currentIndex = 0;
            QueueChanged?.Invoke(this, _queue.AsReadOnly());
        }

        public void Enqueue(MediaItem item)
        {
            if (item != null)
            {
                _queue.Add(item);
                QueueChanged?.Invoke(this, _queue.AsReadOnly());
            }
        }

        public void Enqueue(IEnumerable<MediaItem> items)
        {
            if (items != null)
            {
                _queue.AddRange(items);
                QueueChanged?.Invoke(this, _queue.AsReadOnly());
            }
        }

        public async Task Play()
        {
            if (_queue.Count == 0)
                return;

            if (_currentIndex < 0 || _currentIndex >= _queue.Count)
                _currentIndex = 0;

            var item = _queue[_currentIndex];
            TrackChanged?.Invoke(this, item);
            await _engine.Play(item.Path);
            _positionTimer.Start();
        }

        public Task Play(MediaItem item)
        {
            ClearQueue();
            Enqueue(item);
            return Play();
        }

        public async Task Next()
        {
            if (_queue.Count == 0)
                return;
            _currentIndex = (_currentIndex + 1) % _queue.Count;
            await Play();
        }

        public async Task Previous()
        {
            if (_queue.Count == 0)
                return;
            _currentIndex = (_currentIndex - 1 + _queue.Count) % _queue.Count;
            await Play();
        }

        public void Pause() => _engine.Pause();
        public void Resume() => _engine.Resume();
        public void Stop()
        {
            _positionTimer.Stop();
            _engine.Stop();
        }

        public TimeSpan Position
        {
            get => _engine.Position;
            set => _engine.Position = value;
        }

        public TimeSpan Duration => _engine.Duration;
        public int Volume
        {
            get => _engine.Volume;
            set => _engine.Volume = value;
        }

        private void OnEngineStarted(object? sender, EventArgs e)
        {
            PlaybackStateChanged?.Invoke(this, true);
        }

        private void OnEnginePaused(object? sender, EventArgs e)
        {
            PlaybackStateChanged?.Invoke(this, false);
        }

        public void Dispose()
        {
            _positionTimer.Stop();
            _engine.PlaybackStarted -= OnEngineStarted;
            _engine.PlaybackPaused  -= OnEnginePaused;
            _engine.PlaybackEnded   -= async (_, __) => await Next();
            _engine.Dispose();
        }
    }
} 