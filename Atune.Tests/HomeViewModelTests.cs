using System.Threading.Tasks;
using Atune.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
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
            
            // Act
            var viewModel = new HomeViewModel(cache, logger);
            
            // Assert – сообщение по умолчанию из конструктора ожидается как "Welcome to Atune!"
            Assert.Equal("Welcome to Atune!", viewModel.WelcomeMessage);
        }
        
        [Fact]
        public async Task LoadWelcomeMessageAsync_UpdatesWelcomeMessage()
        {
            // Arrange
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<HomeViewModel>.Instance;
            var viewModel = new HomeViewModel(cache, logger);
            
            // Act
            await viewModel.LoadWelcomeMessageAsync();
            
            // Assert
            Assert.Equal("Welcome to Atune!", viewModel.WelcomeMessage);
        }
    }
} 