using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Markup.Xaml;
using Atune.Services;
using Moq;
using Atune.Models;
using Xunit;
using Avalonia.Headless;

namespace Atune.Tests
{
    public class LocalizationServiceTests
    {
        public LocalizationServiceTests()
        {
            if (Application.Current == null)
            {
                // Инициализируем Avalonia для headless-тестирования с параметрами по умолчанию
                // Initialize Avalonia for headless testing with default parameters
                AppBuilder.Configure<App>()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                    .SetupWithoutStarting();
                // Создаем ResourceDictionary, если он не установлен
                // Create ResourceDictionary if it is not set
                if (Application.Current!.Resources == null)
                {
                    Application.Current!.Resources = new ResourceDictionary();
                }
            }
        }
        
        [Fact]
        public void SetLanguage_ValidLanguage_RU_LoadsResourceDictionaries()
        {
            // Arrange
            var mockSettingsService = new Mock<ISettingsService>();
            var mockLogger = new Mock<ILoggerService>();
            // Возвращаем базовые настройки (например, язык "en"); значение не важно, т.к. далее мы вызовем SetLanguage("ru")
            // Return base settings (e.g., language "en"); the value is not important, since we will call SetLanguage("ru") later
            mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings { Language = "en" });
            var localizationService = new LocalizationService(mockSettingsService.Object, mockLogger.Object);

            // Act
            localizationService.SetLanguage("ru");
            var dictionaries = Application.Current!.Resources!.MergedDictionaries;
            
            // Assert – для языка "ru" резервный словарь будет загружен как "en"
            // Assert – for language "ru", the fallback dictionary will be loaded as "en"
            Assert.True(dictionaries.Count >= 2, "Должны быть загружены как минимум два словаря (резервный и основной).");
        }
        
        [Fact]
        public void SetLanguage_InvalidLanguage_ThrowsFileNotFoundException()
        {
            // Arrange
            var mockSettingsService = new Mock<ISettingsService>();
            var mockLogger = new Mock<ILoggerService>();
            // Возвращаем базовые настройки (например, язык "en")
            // Return base settings (e.g., language "en")
            mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings { Language = "en" });
            var localizationService = new LocalizationService(mockSettingsService.Object, mockLogger.Object);
            
            // Act and Assert
            Assert.Throws<FileNotFoundException>(() => localizationService.SetLanguage("fr"));
        }
    }
} 
