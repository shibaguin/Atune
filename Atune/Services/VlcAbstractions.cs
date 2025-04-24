using System;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace Atune.Services
{
    public interface ILibVLC : IDisposable
    {
    }

    public interface IMedia : IDisposable
    {
        void Parse(MediaParseOptions options);
        long Duration { get; }
        string? Mrl { get; }
        string? Meta(MetadataType type);
    }

    public interface IMediaPlayer : IDisposable
    {
        event EventHandler? EndReached;
        event EventHandler? Playing;
        event EventHandler? Paused;

        void Play(IMedia media);
        void Play();
        void Pause();
        void Stop();

        bool IsPlaying { get; }
        int Volume { get; set; }
        float Position { get; set; }
        IMedia? Media { get; set; }
        bool Mute { get; set; }
    }

    public interface ILibVLCFactory
    {
        ILibVLC Create();
    }

    public interface IMediaFactory
    {
        IMedia Create(ILibVLC lib, Uri uri);
    }

    public interface IMediaPlayerFactory
    {
        IMediaPlayer Create(ILibVLC lib);
    }

    public interface IDispatcherService
    {
        Task InvokeAsync(Action action);
    }
} 