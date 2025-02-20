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
        void LoadSettings();
        
        // Метод для сохранения настроек интерфейса в settings.ini
        void SaveSettings();

        // Метод для обновления настроек интерфейса через UI
        void UpdateInterfaceSettings(double headerFontSize,
                                     double navigationDividerWidth,
                                     double navigationDividerHeight,
                                     double topDockHeight,
                                     double barHeight,
                                     double navigationFontSize,
                                     double barPadding);

        // Новый метод для восстановления настроек интерфейса по умолчанию
        void RestoreDefaults();
    }
} 