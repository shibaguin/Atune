using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using LibVLCSharp.Shared;
using Atune.Services;

namespace Atune.Tests.Integration
{
    public class MediaPlayerServiceIntegrationTests : IDisposable
    {
        private readonly string _testFilePath;

        public MediaPlayerServiceIntegrationTests()
        {
            // Generate a silent WAV file of 3 seconds for testing
            _testFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
            CreateSilentWav(_testFilePath, durationSeconds: 3);
        }

        public void Dispose()
        {
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);
        }

        private void CreateSilentWav(string path, int durationSeconds)
        {
            const int sampleRate = 8000;
            const short bitsPerSample = 16;
            const short channels = 1;
            int bytesPerSample = bitsPerSample / 8;
            int totalSamples = sampleRate * durationSeconds;
            int dataSize = totalSamples * channels * bytesPerSample;

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fs);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM format
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bytesPerSample);
            writer.Write((short)(channels * bytesPerSample));
            writer.Write(bitsPerSample);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);
            writer.Write(new byte[dataSize]);
        }

        [Fact]
        public async Task Play_ShouldStartAndPlayForDuration()
        {
            // Arrange
            Core.Initialize();
            var settingsService = new Mock<ISettingsService>().Object;
            var logger = new Mock<ILogger<MediaPlayerService>>().Object;
            var service = new MediaPlayerService(
                settingsService,
                logger,
                new LibVLCFactory(),
                new MediaFactory(),
                new MediaPlayerFactory(),
                new SynchronousDispatcher());

            bool started = false;
            service.PlaybackStarted += (_, _) => started = true;

            // Act
            await service.Play(_testFilePath);
            Assert.True(started, "Playback did not start");

            // Wait for 2 seconds (should still be playing)
            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.True(service.IsPlaying, "MediaPlayerService should still be playing");

            // Stop playback
            service.Stop();
            Assert.False(service.IsPlaying, "MediaPlayerService should have stopped after Stop()");
        }

        // Simple dispatcher that invokes actions synchronously for testing
        private class SynchronousDispatcher : IDispatcherService
        {
            public Task InvokeAsync(Action action)
            {
                action();
                return Task.CompletedTask;
            }
        }
    }
} 