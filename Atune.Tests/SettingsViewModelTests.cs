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
            
            // Создаем пустой mock для IInterfaceSettingsService, так как он сейчас требуется
            // Create a mock for IInterfaceSettingsService, as it is currently required
            var fakeInterfaceSettingsService = new Mock<IInterfaceSettingsService>();
            var viewModel = new SettingsViewModel(mockSettingsService.Object, fakeInterfaceSettingsService.Object);
            
            // Assert – "ru" должен отображаться как "Русский"
            // Assert – "ru" should display as "Russian"
            Assert.Equal("Русский", viewModel.SelectedLanguage);
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
            
            var fakeInterfaceSettingsService = new Mock<IInterfaceSettingsService>();
            var viewModel = new SettingsViewModel(mockSettingsService.Object, fakeInterfaceSettingsService.Object);
            viewModel.SelectedLanguage = "English";
            // Допустим, что ThemeVariant.Light имеет числовое значение 1
            // Assume that ThemeVariant.Light has a numerical value of 1
            viewModel.SelectedThemeIndex = (int)ThemeVariant.Light;
            
            // Act – вызываем команду сохранения
            // Act – call the save settings command
            viewModel.SaveSettingsCommand.Execute(null);
            
            // Assert – проверяем, что вызывается SaveSettings с корректным кодом языка ("en")
            // Assert – check that SaveSettings is called with the correct language code ("en")
            mockSettingsService.Verify(s => s.SaveSettings(
                It.Is<AppSettings>(a => a.Language == "en" && a.ThemeVariant == ThemeVariant.Light)
            ), Times.Once);
        }

        [Fact]
        public void RestoreDefaults_Command_UpdatesInterfaceProperties()
        {
            // Arrange: задаем значения по умолчанию, которые возвращаются от интерфейсного сервиса.
            // Arrange: set the default values returned by the interface service.
            double defaultHeaderFontSize = 24;
            double defaultNavigationDividerWidth = 3;
            double defaultNavigationDividerHeight = 3;
            double defaultTopDockHeight = 50;
            double defaultBarHeight = 50;
            double defaultNavigationFontSize = 14;
            double defaultBarPadding = 8;

            // Настройки приложения (например, язык "ru")
            // Application settings (e.g., language "ru")
            var settings = new AppSettings
            {
                Language = "ru",
                ThemeVariant = ThemeVariant.System
            };

            var mockSettingsService = new Mock<ISettingsService>();
            mockSettingsService.Setup(s => s.LoadSettings()).Returns(settings);

            var mockInterfaceSettingsService = new Mock<IInterfaceSettingsService>();
            mockInterfaceSettingsService.SetupGet(s => s.HeaderFontSize).Returns(defaultHeaderFontSize);
            mockInterfaceSettingsService.SetupGet(s => s.NavigationDividerWidth).Returns(defaultNavigationDividerWidth);
            mockInterfaceSettingsService.SetupGet(s => s.NavigationDividerHeight).Returns(defaultNavigationDividerHeight);
            mockInterfaceSettingsService.SetupGet(s => s.TopDockHeight).Returns(defaultTopDockHeight);
            mockInterfaceSettingsService.SetupGet(s => s.BarHeight).Returns(defaultBarHeight);
            mockInterfaceSettingsService.SetupGet(s => s.NavigationFontSize).Returns(defaultNavigationFontSize);
            mockInterfaceSettingsService.SetupGet(s => s.BarPadding).Returns(defaultBarPadding);

            // Act: Создаем экземпляр модели представления и изменяем значения интерфейсных свойств.
            // Act: Create an instance of the view model and change the values of the interface properties.
            var viewModel = new SettingsViewModel(mockSettingsService.Object, mockInterfaceSettingsService.Object);
            viewModel.HeaderFontSize = 40;
            viewModel.NavigationDividerWidth = 10;
            viewModel.NavigationDividerHeight = 10;
            viewModel.TopDockHeight = 100;
            viewModel.BarHeight = 100;
            viewModel.NavigationFontSize = 20;
            viewModel.BarPadding = 15;

            // Выполняем команду восстановления настроек по умолчанию.
            // Execute the restore defaults command.
            viewModel.RestoreDefaultsCommand.Execute(null);

            // Assert: После восстановления, значения свойств должны совпадать с возвращаемыми по умолчанию.
            // Assert: After restoration, the property values should match the default values returned.
            Assert.Equal(defaultHeaderFontSize, viewModel.HeaderFontSize);
            Assert.Equal(defaultNavigationDividerWidth, viewModel.NavigationDividerWidth);
            Assert.Equal(defaultNavigationDividerHeight, viewModel.NavigationDividerHeight);
            Assert.Equal(defaultTopDockHeight, viewModel.TopDockHeight);
            Assert.Equal(defaultBarHeight, viewModel.BarHeight);
            Assert.Equal(defaultNavigationFontSize, viewModel.NavigationFontSize);
            Assert.Equal(defaultBarPadding, viewModel.BarPadding);
        }
    }
} 