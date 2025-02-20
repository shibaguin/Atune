using Atune.Models;
using Atune.Services;
using Atune.ViewModels;
using Moq;
using Xunit;

namespace Atune.Tests
{
    public class SettingsViewModelTests
    {
        [Fact]
        public void Constructor_LoadsSettingsAndMapsLanguageCorrectly()
        {
            // Arrange
            var settings = new AppSettings
            {
                Language = "ru",
                ThemeVariant = ThemeVariant.System
            };
            var mockSettingsService = new Mock<ISettingsService>();
            mockSettingsService.Setup(s => s.LoadSettings()).Returns(settings);
            
            // Act
            var viewModel = new SettingsViewModel(mockSettingsService.Object);
            
            // Assert – "ru" должен отображаться как "Русская"
            Assert.Equal("Русская", viewModel.SelectedLanguage);
            Assert.Equal((int)ThemeVariant.System, viewModel.SelectedThemeIndex);
        }
        
        [Fact]
        public void SaveSettings_CommandConvertsLanguageDisplayToCode()
        {
            // Arrange
            var settings = new AppSettings
            {
                ThemeVariant = ThemeVariant.System,
                Language = "ru"
            };
            var mockSettingsService = new Mock<ISettingsService>();
            mockSettingsService.Setup(s => s.LoadSettings()).Returns(settings);
            
            var viewModel = new SettingsViewModel(mockSettingsService.Object);
            viewModel.SelectedLanguage = "English";
            // Допустим, что ThemeVariant.Light имеет числовое значение 1
            viewModel.SelectedThemeIndex = (int)ThemeVariant.Light;
            
            // Act – вызываем команду сохранения
            viewModel.SaveSettingsCommand.Execute(null);
            
            // Assert – проверяем, что вызывается SaveSettings с корректным кодом языка ("en")
            mockSettingsService.Verify(s => s.SaveSettings(
                It.Is<AppSettings>(a => a.Language == "en" && a.ThemeVariant == ThemeVariant.Light)
            ), Times.Once);
        }
    }
} 