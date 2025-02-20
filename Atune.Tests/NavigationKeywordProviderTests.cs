using Atune.ViewModels;
using Xunit;

namespace Atune.Tests
{
    public class NavigationKeywordProviderTests
    {
        [Fact]
        public void GetNavigationKeywords_ReturnsExpectedDictionary()
        {
            // Arrange
            var provider = new NavigationKeywordProvider();
            
            // Act
            var keywords = provider.GetNavigationKeywords();
            
            // Assert – проверяем, что словарь содержит 4 раздела
            Assert.Equal(4, keywords.Count);
            // Проверяем наличие ключа для раздела настроек
            Assert.True(keywords.ContainsKey(MainViewModel.SectionType.Settings));
            
            // Дополнительно: проверка наличия известных ключевых слов
            var settingsKeywords = keywords[MainViewModel.SectionType.Settings];
            Assert.Contains("настройки", settingsKeywords);
        }
    }
} 