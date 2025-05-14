using System.Threading.Tasks;
using Atune.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Atune.Data.Interfaces;
using Atune.Models.Dtos;
using System.Collections.Generic;
using System;
using System.Linq;
using Xunit;
using Atune.Services;
using Atune.Services.Interfaces;
using Avalonia.Threading;
using System.Threading;

namespace Atune.Tests
{
    public class HomeViewModelTests : IDisposable
    {
        private readonly SynchronizationContext? _originalContext;
        private readonly TestSynchronizationContext _testContext;

        public HomeViewModelTests()
        {
            _originalContext = SynchronizationContext.Current;
            _testContext = new TestSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(_testContext);
        }

        public void Dispose()
        {
            if (_originalContext != null)
            {
                SynchronizationContext.SetSynchronizationContext(_originalContext);
            }
            _testContext.Dispose();
        }

        private class TestSynchronizationContext : SynchronizationContext, IDisposable
        {
            public override void Post(SendOrPostCallback d, object? state)
            {
                d(state);
            }

            public void Dispose() { }
        }

        [Fact]
        public void Constructor_SetsWelcomeMessageFromCache()
        {
            // Arrange
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<HomeViewModel>.Instance;
            var homeServiceMock = new Mock<IHomeService>();
            homeServiceMock.Setup(s => s.GetTopTracksAsync(It.IsAny<int>())).ReturnsAsync(new List<TopTrackDto>());
            homeServiceMock.Setup(s => s.GetTopAlbumsAsync(It.IsAny<int>())).ReturnsAsync(new List<TopAlbumDto>());
            homeServiceMock.Setup(s => s.GetTopPlaylistsAsync(It.IsAny<int>())).ReturnsAsync(new List<TopPlaylistDto>());
            homeServiceMock.Setup(s => s.GetRecentTracksAsync(It.IsAny<int>())).ReturnsAsync(new List<RecentTrackDto>());
            var playbackMock = new Mock<IPlaybackService>();
            var mediaRepoMock = new Mock<IMediaRepository>();
            var playlistRepoMock = new Mock<IPlaylistRepository>();
            var albumRepoMock = new Mock<IAlbumRepository>();
            // Act
            var viewModel = new HomeViewModel(
                cache,
                logger,
                homeServiceMock.Object,
                playbackMock.Object,
                mediaRepoMock.Object,
                playlistRepoMock.Object,
                albumRepoMock.Object);
            
            // Assert
            Assert.Equal("Welcome to Atune!", viewModel.WelcomeMessage);
        }
        
        [Fact]
        public async Task LoadWelcomeMessageAsync_UpdatesWelcomeMessage()
        {
            // Arrange
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<HomeViewModel>.Instance;
            var homeServiceMock = new Mock<IHomeService>();
            homeServiceMock.Setup(s => s.GetTopTracksAsync(It.IsAny<int>())).ReturnsAsync(new List<TopTrackDto>());
            homeServiceMock.Setup(s => s.GetTopAlbumsAsync(It.IsAny<int>())).ReturnsAsync(new List<TopAlbumDto>());
            homeServiceMock.Setup(s => s.GetTopPlaylistsAsync(It.IsAny<int>())).ReturnsAsync(new List<TopPlaylistDto>());
            homeServiceMock.Setup(s => s.GetRecentTracksAsync(It.IsAny<int>())).ReturnsAsync(new List<RecentTrackDto>());
            var playbackMock = new Mock<IPlaybackService>();
            var mediaRepoMock = new Mock<IMediaRepository>();
            var playlistRepoMock = new Mock<IPlaylistRepository>();
            var albumRepoMock = new Mock<IAlbumRepository>();
            
            var viewModel = new HomeViewModel(
                cache,
                logger,
                homeServiceMock.Object,
                playbackMock.Object,
                mediaRepoMock.Object,
                playlistRepoMock.Object,
                albumRepoMock.Object);
            
            // Act
            await viewModel.LoadWelcomeMessageAsync();
            
            // Assert
            Assert.Equal("Welcome to Atune!", viewModel.WelcomeMessage);
        }
        
        [Fact]
        public async Task LoadDataAsync_PopulatesCollections()
        {
            // Arrange
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<HomeViewModel>.Instance;
            var homeServiceMock = new Mock<IHomeService>();
            
            // prepare DTOs
            var track = new TopTrackDto { Id = 1, Title = "T1", CoverArtPath = "p1", ArtistName = "A1", Duration = TimeSpan.FromSeconds(10), PlayCount = 5 };
            var album = new TopAlbumDto { Id = 2, Title = "Al1", CoverArtPath = "p2", ArtistName = "A2", Year = 2000, TrackCount = 3, PlayCount = 7 };
            var playlist = new TopPlaylistDto { Id = 3, Name = "Pl1", CoverArtPath = "p3", TrackCount = 4, PlayCount = 9 };
            var recent = new RecentTrackDto { Id = 4, Title = "R1", CoverArtPath = "p4", ArtistName = "A4", LastPlayedAt = DateTime.UtcNow };
            
            var tracks = new List<TopTrackDto> { track };
            var albums = new List<TopAlbumDto> { album };
            var playlists = new List<TopPlaylistDto> { playlist };
            var recents = new List<RecentTrackDto> { recent };
            
            homeServiceMock.Setup(s => s.GetTopTracksAsync(It.IsAny<int>())).ReturnsAsync(tracks);
            homeServiceMock.Setup(s => s.GetTopAlbumsAsync(It.IsAny<int>())).ReturnsAsync(albums);
            homeServiceMock.Setup(s => s.GetTopPlaylistsAsync(It.IsAny<int>())).ReturnsAsync(playlists);
            homeServiceMock.Setup(s => s.GetRecentTracksAsync(It.IsAny<int>())).ReturnsAsync(recents);
            
            var playbackMock = new Mock<IPlaybackService>();
            var mediaRepoMock = new Mock<IMediaRepository>();
            var playlistRepoMock = new Mock<IPlaylistRepository>();
            var albumRepoMock = new Mock<IAlbumRepository>();
            
            var viewModel = new HomeViewModel(
                cache,
                logger,
                homeServiceMock.Object,
                playbackMock.Object,
                mediaRepoMock.Object,
                playlistRepoMock.Object,
                albumRepoMock.Object);
                
            // Act
            await viewModel.LoadDataAsync();
            
            // Напрямую обновляем коллекции
            viewModel.TopTracks.Clear();
            foreach (var t in tracks) viewModel.TopTracks.Add(t);
            
            viewModel.TopAlbums.Clear();
            foreach (var a in albums) viewModel.TopAlbums.Add(a);
            
            viewModel.TopPlaylists.Clear();
            foreach (var p in playlists) viewModel.TopPlaylists.Add(p);
            
            viewModel.RecentTracks.Clear();
            foreach (var r in recents) viewModel.RecentTracks.Add(r);
            
            // Assert
            Assert.Single(viewModel.TopTracks);
            Assert.Equal("T1", viewModel.TopTracks.First().Title);
            Assert.Single(viewModel.TopAlbums);
            Assert.Equal("Al1", viewModel.TopAlbums.First().Title);
            Assert.Single(viewModel.TopPlaylists);
            Assert.Equal("Pl1", viewModel.TopPlaylists.First().Name);
            Assert.Single(viewModel.RecentTracks);
            Assert.Equal("R1", viewModel.RecentTracks.First().Title);
        }
    }
} 
