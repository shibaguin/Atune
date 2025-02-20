namespace Atune.Services
{
    public interface IInterfaceSettingsService
    {
        double HeaderFontSize { get; }
        double NavigationDividerWidth { get; }
        double NavigationDividerHeight { get; }
        double TopDockHeight { get; }
        double BarHeight { get; }
        double NavigationFontSize { get; }
        double BarPadding { get; }

        // Метод для загрузки (обновления) настроек из файла
        // Method for loading (updating) settings from a file
        void LoadSettings();
        
        // Метод для сохранения настроек интерфейса в settings.ini
        // Method for saving interface settings to settings.ini
        void SaveSettings();

        // Метод для обновления настроек интерфейса через UI
        // Method for updating interface settings through the UI
        void UpdateInterfaceSettings(double headerFontSize,
                                     double navigationDividerWidth,
                                     double navigationDividerHeight,
                                     double topDockHeight,
                                     double barHeight,
                                     double navigationFontSize,
                                     double barPadding);

        // Новый метод для восстановления настроек интерфейса по умолчанию
        // New method for restoring interface settings to default values
        void RestoreDefaults();
    }
} 