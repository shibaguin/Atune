using System;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Avalonia.Threading;

namespace Atune.Services
{
    public class LibVLCFactory : ILibVLCFactory
    {
        public ILibVLC Create()
        {
            Core.Initialize();
            var lib = new LibVLC(
                enableDebugLogs: false,
                "--avcodec-hw=none",
                "--no-xlib",
                "--ignore-config",
                "--no-sub-autodetect-file",
                "--network-caching=300",
                "--file-caching=300",
                "--no-audio-time-stretch"
            );
            return new LibVLCWrapper(lib);
        }
    }

    public class LibVLCWrapper : ILibVLC
    {
        private readonly LibVLC _inner;
        public LibVLCWrapper(LibVLC lib) => _inner = lib;
        internal LibVLC Native => _inner;
        public void Dispose() => _inner.Dispose();
    }

    public class MediaFactory : IMediaFactory
    {
        public IMedia Create(ILibVLC lib, Uri uri)
        {
            if (lib is LibVLCWrapper wrapper)
                return new MediaWrapper(wrapper.Native, uri);
            throw new InvalidOperationException("Invalid ILibVLC implementation");
        }
    }

    public class MediaWrapper : IMedia
    {
        private readonly Media _inner;
        public MediaWrapper(LibVLC lib, Uri uri) => _inner = new Media(lib, uri);
        // Constructor to wrap existing Media instance
        public MediaWrapper(Media existing) => _inner = existing;
        public void Parse(MediaParseOptions options) => _inner.Parse(options);
        public long Duration => _inner.Duration;
        public string? Mrl => _inner.Mrl;
        public string? Meta(MetadataType type) => _inner.Meta(type);
        internal Media InnerMedia => _inner;
        public void Dispose() => _inner.Dispose();
    }

    public class MediaPlayerFactory : IMediaPlayerFactory
    {
        public IMediaPlayer Create(ILibVLC lib)
        {
            if (lib is LibVLCWrapper wrapper)
                return new MediaPlayerWrapper(wrapper.Native);
            throw new InvalidOperationException("Invalid ILibVLC implementation");
        }
    }

    public class MediaPlayerWrapper : IMediaPlayer
    {
        private readonly MediaPlayer _inner;
        public MediaPlayerWrapper(LibVLC lib)
        {
            _inner = new MediaPlayer(lib);
            _inner.EndReached += (s, e) => EndReached?.Invoke(this, e);
            _inner.Playing += (s, e) => Playing?.Invoke(this, e);
            _inner.Paused += (s, e) => Paused?.Invoke(this, e);
        }

        public event EventHandler? EndReached;
        public event EventHandler? Playing;
        public event EventHandler? Paused;

        public void Play(IMedia media)
        {
            if (media is MediaWrapper wrapper)
                _inner.Play(wrapper.InnerMedia);
            else
                throw new InvalidOperationException("Invalid IMedia implementation");
        }

        public void Play() => _inner.Play();
        public void Pause() => _inner.Pause();
        public void Stop() => _inner.Stop();

        public bool IsPlaying => _inner.IsPlaying;
        public int Volume { get => _inner.Volume; set => _inner.Volume = value; }
        public float Position { get => _inner.Position; set => _inner.Position = value; }
        public IMedia? Media
        {
            get
            {
                var nativeMedia = _inner.Media;
                return nativeMedia != null ? new MediaWrapper(nativeMedia) : null;
            }
            set
            {
                if (value is MediaWrapper wrapper)
                    _inner.Media = wrapper.InnerMedia;
            }
        }
        public bool Mute { get => _inner.Mute; set => _inner.Mute = value; }

        public void Dispose() => _inner.Dispose();
    }

    public class AvaloniaDispatcherService : IDispatcherService
    {
        public Task InvokeAsync(Action action)
        {
            Dispatcher.UIThread.Post(action);
            return Task.CompletedTask;
        }
    }
}
