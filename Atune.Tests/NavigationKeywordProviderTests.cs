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
            // Assert – check that the dictionary contains 4 sections
            Assert.Equal(4, keywords.Count);
            // Проверяем наличие ключа для раздела настроек
            // Check for the presence of a key for the settings section
            Assert.True(keywords.ContainsKey(MainViewModel.SectionType.Settings));
            
            // Дополнительно: проверка наличия известных ключевых слов
            // Additional: check for known keywords
            var settingsKeywords = keywords[MainViewModel.SectionType.Settings];
            Assert.Contains("настройки", settingsKeywords);
        }
    }
} 