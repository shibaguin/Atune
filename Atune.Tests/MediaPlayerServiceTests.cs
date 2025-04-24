using System;
using System.Threading.Tasks;
using Atune.Services;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Atune.Tests
{
    public class MediaPlayerServiceTests
    {
        private MediaPlayerService CreateService(out Mock<IMediaPlayer> mediaPlayerMock, out Mock<IMedia> mediaMock)
        {
            var settingsServiceMock = new Mock<ISettingsService>();
            var loggerMock = new Mock<ILogger<MediaPlayerService>>();
            var libVlcFactoryMock = new Mock<ILibVLCFactory>();
            var mediaFactoryMock = new Mock<IMediaFactory>();
            var mediaPlayerFactoryMock = new Mock<IMediaPlayerFactory>();
            var dispatcherMock = new Mock<IDispatcherService>();

            var libVlcMock = new Mock<ILibVLC>();
            libVlcFactoryMock.Setup(f => f.Create()).Returns(libVlcMock.Object);

            mediaMock = new Mock<IMedia>();
            mediaFactoryMock.Setup(f => f.Create(libVlcMock.Object, It.IsAny<Uri>()))
                .Returns(mediaMock.Object);

            mediaPlayerMock = new Mock<IMediaPlayer>();
            mediaPlayerFactoryMock.Setup(f => f.Create(libVlcMock.Object))
                .Returns(mediaPlayerMock.Object);

            dispatcherMock.Setup(d => d.InvokeAsync(It.IsAny<Action>()))
                .Returns<Action>(a => { a(); return Task.CompletedTask; });

            var service = new MediaPlayerService(
                settingsServiceMock.Object,
                loggerMock.Object,
                libVlcFactoryMock.Object,
                mediaFactoryMock.Object,
                mediaPlayerFactoryMock.Object,
                dispatcherMock.Object);

            return service;
        }

        [Fact]
        public async Task Play_ShouldParseAndPlayMedia_AndRaisePlaybackStarted()
        {
            // Arrange
            var service = CreateService(out var mediaPlayerMock, out var mediaMock);
            bool started = false;
            service.PlaybackStarted += (_, _) => started = true;

            // Act
            await service.Play("file.mp3");

            // Assert
            mediaMock.Verify(m => m.Parse(MediaParseOptions.ParseLocal), Times.Once);
            mediaPlayerMock.Verify(p => p.Play(mediaMock.Object), Times.Once);
            Assert.True(started);
        }

        [Fact]
        public void Pause_ShouldPauseMedia_AndRaisePlaybackPaused()
        {
            // Arrange
            var service = CreateService(out var mediaPlayerMock, out var mediaMock);
            bool paused = false;
            service.PlaybackPaused += (_, _) => paused = true;

            // Act
            service.Pause();

            // Assert
            mediaPlayerMock.Verify(p => p.Pause(), Times.Once);
            Assert.True(paused);
        }

        [Fact]
        public void Resume_ShouldPlayMedia_AndRaisePlaybackStarted()
        {
            // Arrange
            var service = CreateService(out var mediaPlayerMock, out var mediaMock);
            bool started = false;
            service.PlaybackStarted += (_, _) => started = true;

            // Act
            service.Resume();

            // Assert
            mediaPlayerMock.Verify(p => p.Play(), Times.Once);
            Assert.True(started);
        }

        [Fact]
        public async Task Stop_ShouldStopMedia_AndDisposeCurrentMedia()
        {
            // Arrange
            var service = CreateService(out var mediaPlayerMock, out var mediaMock);
            // Act initial Play to set current media
            await service.Play("file.mp3");
            Assert.NotNull(service.GetCurrentMedia());

            // Act
            service.Stop();

            // Assert
            mediaPlayerMock.Verify(p => p.Stop(), Times.Once);
            mediaMock.Verify(m => m.Dispose(), Times.Once);
            Assert.Null(service.GetCurrentMedia());
        }

        [Fact]
        public async Task StopAsync_ShouldStopMediaOnDispatcher()
        {
            // Arrange
            var service = CreateService(out var mediaPlayerMock, out var mediaMock);
            await service.Play("file.mp3");

            // Act
            await service.StopAsync();

            // Assert
            mediaPlayerMock.Verify(p => p.Stop(), Times.Once);
            mediaMock.Verify(m => m.Dispose(), Times.Once);
            Assert.Null(service.GetCurrentMedia());
        }

        [Fact]
        public async Task Play_WhenExceptionInFactory_ShouldThrowMediaPlaybackException()
        {
            // Arrange
            var settingsServiceMock = new Mock<ISettingsService>();
            var loggerMock = new Mock<ILogger<MediaPlayerService>>();
            var libVlcFactoryMock = new Mock<ILibVLCFactory>();
            var mediaFactoryMock = new Mock<IMediaFactory>();
            var mediaPlayerFactoryMock = new Mock<IMediaPlayerFactory>();
            var dispatcherMock = new Mock<IDispatcherService>();

            var libVlcMock = new Mock<ILibVLC>();
            libVlcFactoryMock.Setup(f => f.Create()).Returns(libVlcMock.Object);

            mediaFactoryMock.Setup(f => f.Create(libVlcMock.Object, It.IsAny<Uri>()))
                .Throws(new Exception("create failed"));

            mediaPlayerFactoryMock.Setup(f => f.Create(libVlcMock.Object))
                .Returns(new Mock<IMediaPlayer>().Object);
            dispatcherMock.Setup(d => d.InvokeAsync(It.IsAny<Action>()))
                .Returns<Action>(a => { a(); return Task.CompletedTask; });

            var service = new MediaPlayerService(
                settingsServiceMock.Object,
                loggerMock.Object,
                libVlcFactoryMock.Object,
                mediaFactoryMock.Object,
                mediaPlayerFactoryMock.Object,
                dispatcherMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<MediaPlaybackException>(async () => await service.Play("file.mp3"));
        }

        [Fact]
        public async Task GetCurrentMetadataAsync_NoCurrentMedia_ReturnsDefaultValues()
        {
            // Arrange
            var service = CreateService(out var mediaPlayerMock, out var mediaMock);

            // Act
            var metadata = await service.GetCurrentMetadataAsync();

            // Assert
            Assert.Equal("Нет данных", metadata.Title);
            Assert.Equal("Нет данных", metadata.Artist);
        }

        [Fact]
        public void PlaybackEnded_ShouldRaisePlaybackEndedEvent_WhenMediaPlayerEndReached()
        {
            // Arrange
            var service = CreateService(out var mediaPlayerMock, out var mediaMock);
            bool ended = false;
            service.PlaybackEnded += (_, _) => ended = true;

            // Act
            mediaPlayerMock.Raise(m => m.EndReached += null, EventArgs.Empty);

            // Assert
            Assert.True(ended);
        }
    }
} 