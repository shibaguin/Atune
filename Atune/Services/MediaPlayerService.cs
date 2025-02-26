using LibVLCSharp.Shared;
using System;

namespace Atune.Services
{
    public class MediaPlayerService : IDisposable
    {
        private LibVLC _libVlc;
        private MediaPlayer _player;
        private Media? _currentMedia;
        
        public event EventHandler? PlaybackEnded;

        public MediaPlayerService()
        {
            try 
            {
                _libVlc = new LibVLC(enableDebugLogs: true);
                _player = new MediaPlayer(_libVlc);
                _player.EndReached += (sender, e) => PlaybackEnded?.Invoke(this, EventArgs.Empty);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"VLC INIT ERROR: {ex}");
                throw;
            }
        }

        public void Play(string path)
        {
            Stop();
            _currentMedia = new Media(_libVlc, new Uri(path));
            _player.Media = _currentMedia;
            _player.Play();
        }

        public void Pause() => _player.Pause();
        
        public void Resume() => _player.Play();

        public void Stop()
        {
            _player.Stop();
            _currentMedia?.Dispose();
            _currentMedia = null;
        }

        public bool IsPlaying => _player.IsPlaying;

        public int Volume
        {
            get => _player.Volume;
            set => _player.Volume = Math.Clamp(value, 0, 100);
        }

        public TimeSpan Position
        {
            get => TimeSpan.FromMilliseconds(_player.Position * _player.Media?.Duration ?? 0);
            set
            {
                if (_player.Media != null && _player.Media.Duration > 0)
                    _player.Position = (float)(value.TotalMilliseconds / _player.Media.Duration);
            }
        }

        public void Dispose()
        {
            _player.Dispose();
            _libVlc.Dispose();
            GC.SuppressFinalize(this);
        }

        private void OnPlaybackEnded(object? sender, EventArgs e)
        {
            PlaybackEnded?.Invoke(this, e);
        }
    }
} 