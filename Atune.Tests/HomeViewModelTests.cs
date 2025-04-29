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

namespace Atune.Tests
{
    public class HomeViewModelTests
    {
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
            var repoMock = new Mock<IHomeRepository>();
            repoMock.Setup(r => r.GetTopTracksAsync(It.IsAny<int>())).ReturnsAsync(new List<TopTrackDto>());
            repoMock.Setup(r => r.GetTopAlbumsAsync(It.IsAny<int>())).ReturnsAsync(new List<TopAlbumDto>());
            repoMock.Setup(r => r.GetTopPlaylistsAsync(It.IsAny<int>())).ReturnsAsync(new List<TopPlaylistDto>());
            repoMock.Setup(r => r.GetRecentTracksAsync(It.IsAny<int>())).ReturnsAsync(new List<RecentTrackDto>());
            var viewModel = new HomeViewModel(cache, logger, repoMock.Object);
            
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
            var repoMock = new Mock<IHomeRepository>();
            // prepare DTOs
            var track = new TopTrackDto { Id = 1, Title = "T1", CoverArtPath = "p1", ArtistName = "A1", Duration = TimeSpan.FromSeconds(10), PlayCount = 5 };
            var album = new TopAlbumDto { Id = 2, Title = "Al1", CoverArtPath = "p2", ArtistName = "A2", Year = 2000, TrackCount = 3, PlayCount = 7 };
            var playlist = new TopPlaylistDto { Id = 3, Name = "Pl1", CoverArtPath = "p3", TrackCount = 4, PlayCount = 9 };
            var recent = new RecentTrackDto { Id = 4, Title = "R1", CoverArtPath = "p4", ArtistName = "A4", LastPlayedAt = DateTime.UtcNow };
            repoMock.Setup(r => r.GetTopTracksAsync(It.IsAny<int>())).ReturnsAsync(new List<TopTrackDto> { track });
            repoMock.Setup(r => r.GetTopAlbumsAsync(It.IsAny<int>())).ReturnsAsync(new List<TopAlbumDto> { album });
            repoMock.Setup(r => r.GetTopPlaylistsAsync(It.IsAny<int>())).ReturnsAsync(new List<TopPlaylistDto> { playlist });
            repoMock.Setup(r => r.GetRecentTracksAsync(It.IsAny<int>())).ReturnsAsync(new List<RecentTrackDto> { recent });
            var viewModel = new HomeViewModel(cache, logger, repoMock.Object);
            // Act
            await viewModel.LoadDataAsync();
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
