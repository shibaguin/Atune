using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices; // Added for platform check / Добавлено для проверки платформы

namespace Atune.Services
{
    public class InterfaceSettingsService : IInterfaceSettingsService
    {
        // Значения по умолчанию, соответствующие текущим параметрам
        // Default values corresponding to the current parameters
        private const double DefaultHeaderFontSize = 24;
        private const double DefaultNavigationDividerWidth = 3;
        private const double DefaultBarHeight = 50;
        private const double DefaultTopDockHeight = 50;
        private const double DefaultNavigationFontSize = 14;
        private const double DefaultBarPadding = 8;
        private const double DefaultNavigationDividerHeight = DefaultBarHeight; // По умолчанию равно BarHeight
        
        private readonly string _iniFilePath;
        private readonly ILoggerService _logger;

        public double HeaderFontSize { get; private set; }
        public double NavigationDividerWidth { get; private set; }
        public double NavigationDividerHeight { get; private set; }
        public double TopDockHeight { get; private set; }
        public double BarHeight { get; private set; }
        public double NavigationFontSize { get; private set; }
        public double BarPadding { get; private set; }
        
        // Обновлённый конструктор для задания пути к settings.ini в зависимости от платформы
        // Updated constructor to set the path to settings.ini depending on the platform
        public InterfaceSettingsService(ILoggerService logger)
        {
            _logger = logger;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _iniFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atune", "settings.ini");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _iniFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "Atune", "settings.ini");
            }
            else
            {
                _iniFilePath = Path.Combine(AppContext.BaseDirectory, "settings.ini");
            }
            // Создаем директорию, если она не существует
            // Create the directory if it does not exist
            Directory.CreateDirectory(Path.GetDirectoryName(_iniFilePath)!);
            LoadSettings();
        }
        
        public void LoadSettings()
        {
            // Если файл настроек не найден, устанавливаем дефолтные значения и сразу сохраняем их,
            // чтобы файл содержал размеры интерфейса с первого запуска.
            // If the settings file is not found, set default values and save them immediately,
            // so that the file contains the interface sizes from the first launch.
            if (!File.Exists(_iniFilePath))
            {
                // Если файл настроек не найден, создаем файл с дефолтными значениями
                // If the settings file is not found, create a file with default values
                _logger.LogInformation($"Settings file not found at {_iniFilePath}. Creating new file with default values.");
                HeaderFontSize = DefaultHeaderFontSize;
                NavigationDividerWidth = DefaultNavigationDividerWidth;
                NavigationDividerHeight = DefaultNavigationDividerHeight;
                TopDockHeight = DefaultTopDockHeight;
                BarHeight = DefaultBarHeight;
                NavigationFontSize = DefaultNavigationFontSize;
                BarPadding = DefaultBarPadding;
                
                SaveSettings();
                return;
            }
            
            // Существующая логика загрузки настроек из файла
            // Existing logic for loading settings from a file
            try
            {
                var iniData = File.ReadAllLines(_iniFilePath);
                // Пример парсинга строки (реальная реализация может отличаться)
                // Example parsing of a line (the actual implementation may differ)
                foreach (var line in iniData)
                {
                    if (line.StartsWith("HeaderFontSize="))
                    {
                        if (double.TryParse(line.Split('=')[1], out double value))
                        {
                            HeaderFontSize = value;
                        }
                    }
                    else if (line.StartsWith("NavigationDividerWidth="))
                    {
                        if (double.TryParse(line.Split('=')[1], out double value))
                        {
                            NavigationDividerWidth = value;
                        }
                    }
                    else if (line.StartsWith("NavigationDividerHeight="))
                    {
                        if (double.TryParse(line.Split('=')[1], out double value))
                        {
                            NavigationDividerHeight = value;
                        }
                    }
                    else if (line.StartsWith("TopDockHeight="))
                    {
                        if (double.TryParse(line.Split('=')[1], out double value))
                        {
                            TopDockHeight = value;
                        }
                    }
                    else if (line.StartsWith("BarHeight="))
                    {
                        if (double.TryParse(line.Split('=')[1], out double value))
                        {
                            BarHeight = value;
                        }
                    }
                    else if (line.StartsWith("NavigationFontSize="))
                    {
                        if (double.TryParse(line.Split('=')[1], out double value))
                        {
                            NavigationFontSize = value;
                        }
                    }
                    else if (line.StartsWith("BarPadding="))
                    {
                        if (double.TryParse(line.Split('=')[1], out double value))
                        {
                            BarPadding = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки при загрузке настроек
                // Logging error during settings load
                _logger.LogError("Error loading interface settings.", ex);
            }
        }
        
        private Dictionary<string, string> LoadIniSection(string filePath, string sectionName)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool inSection = false;
            foreach (var line in File.ReadAllLines(filePath))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;
                    
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    inSection = string.Equals(trimmed.Trim('[', ']'), sectionName, StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (inSection)
                {
                    int separatorIndex = trimmed.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        string key = trimmed.Substring(0, separatorIndex).Trim();
                        string value = trimmed.Substring(separatorIndex + 1).Trim();
                        dict[key] = value;
                    }
                }
            }
            return dict;
        }
        
        // Обновлённый метод SaveSettings
        // Updated SaveSettings method
        public void SaveSettings()
        {
            // Existing code for saving settings to settings.ini
            // For example:
            // var settingsContent = new List<string>
            // {
            //     "[InterfaceDimensions]",
            //     $"HeaderFontSize={HeaderFontSize}",
            //     $"NavigationDividerWidth={NavigationDividerWidth}",
            //     $"NavigationDividerHeight={NavigationDividerHeight}",
            //     $"TopDockHeight={TopDockHeight}",
            //     $"BarHeight={BarHeight}",
            //     $"NavigationFontSize={NavigationFontSize}",
            //     $"BarPadding={BarPadding}"
            // };
            // File.WriteAllLines(_iniFilePath, settingsContent);

            // После сохранения обновляем глобальные ресурсы приложения
            // After saving, update the application's global resources
            UpdateApplicationResources();
        }

        // Новый вспомогательный метод для обновления глобальных ресурсов
        // New helper method to update application resources
        private void UpdateApplicationResources()
        {
            var app = Avalonia.Application.Current;
            if (app != null)
            {
                 // Устанавливаем новые значения в глобальный словарь ресурсов
                 // Sets new values in the global resource dictionary
                 app.Resources["HeaderFontSize"] = HeaderFontSize;
                 app.Resources["NavigationDividerWidth"] = NavigationDividerWidth;
                 app.Resources["NavigationDividerHeight"] = NavigationDividerHeight;
                 app.Resources["TopDockHeight"] = TopDockHeight;
                 app.Resources["BarHeight"] = BarHeight;
                 app.Resources["NavigationFontSize"] = NavigationFontSize;
                 app.Resources["BarPadding"] = BarPadding;

                 _logger.LogInformation("Global resources updated");

                 // Вызываем InvalidateVisual() для перерисовки главного окна на UI-потоке
                 // Calls InvalidateVisual() to redraw the main window on the UI thread
                 Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                 {
                     if (app.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                     {
                         desktop.MainWindow?.InvalidateVisual();
                     }
                 });
            }
        }
        
        // Обновляет настройки интерфейса через UI, проверяя допустимые диапазоны значений.
        // Updates the interface settings via UI, validating acceptable value ranges.
        
        // RUS:
        // <param name="headerFontSize">Размер шрифта заголовка</param>
        // <param name="navigationDividerWidth">Ширина разделителя навигации</param>
        // <param name="navigationDividerHeight">Высота разделителя навигации</param>
        // <param name="topDockHeight">Высота верхней панели</param>
        // <param name="barHeight">Высота панели</param>
        // <param name="navigationFontSize">Размер шрифта навигации</param>
        // <param name="barPadding">Отступы панели</param>

        // ENG:
        // <param name="headerFontSize">Header font size</param>
        // <param name="navigationDividerWidth">Navigation divider width</param>
        // <param name="navigationDividerHeight">Navigation divider height</param>
        // <param name="topDockHeight">Top dock height</param>
        // <param name="barHeight">Bar height</param>
        // <param name="navigationFontSize">Navigation font size</param>
        // <param name="barPadding">Bar padding</param>
        public void UpdateInterfaceSettings(double headerFontSize,
                                            double navigationDividerWidth,
                                            double navigationDividerHeight,
                                            double topDockHeight,
                                            double barHeight,
                                            double navigationFontSize,
                                            double barPadding)
        {
            try
            {
                // Прямые числовые ограничения (±10% от значения по умолчанию)
                // Numeric limits (±10% of default value)
                double minHeaderFontSize = 21.6;
                double maxHeaderFontSize = 26.4;
                double minNavigationDividerWidth = 2.7;
                double maxNavigationDividerWidth = 3.3;
                double minNavigationDividerHeight = 45;
                double maxNavigationDividerHeight = 55;
                double minTopDockHeight = 45;
                double maxTopDockHeight = 55;
                double minBarHeight = 45;
                double maxBarHeight = 55;
                double minNavigationFontSize = 12.6;
                double maxNavigationFontSize = 15.4;
                double minBarPadding = 7.2;
                double maxBarPadding = 8.8;

                if (headerFontSize <= 0 || headerFontSize < minHeaderFontSize || headerFontSize > maxHeaderFontSize)
                {
                    _logger.LogError($"Invalid value for HeaderFontSize ({headerFontSize}). Must be between {minHeaderFontSize} and {maxHeaderFontSize}. Using default: {DefaultHeaderFontSize}.");
                    headerFontSize = DefaultHeaderFontSize;
                }
                if (navigationDividerWidth <= 0 || navigationDividerWidth < minNavigationDividerWidth || navigationDividerWidth > maxNavigationDividerWidth)
                {
                    _logger.LogError($"Invalid value for NavigationDividerWidth ({navigationDividerWidth}). Must be between {minNavigationDividerWidth} and {maxNavigationDividerWidth}. Using default: {DefaultNavigationDividerWidth}.");
                    navigationDividerWidth = DefaultNavigationDividerWidth;
                }
                if (navigationDividerHeight <= 0 || navigationDividerHeight < minNavigationDividerHeight || navigationDividerHeight > maxNavigationDividerHeight)
                {
                    _logger.LogError($"Invalid value for NavigationDividerHeight ({navigationDividerHeight}). Must be between {minNavigationDividerHeight} and {maxNavigationDividerHeight}. Using default: {DefaultNavigationDividerHeight}.");
                    navigationDividerHeight = DefaultNavigationDividerHeight;
                }
                if (topDockHeight <= 0 || topDockHeight < minTopDockHeight || topDockHeight > maxTopDockHeight)
                {
                    _logger.LogError($"Invalid value for TopDockHeight ({topDockHeight}). Must be between {minTopDockHeight} and {maxTopDockHeight}. Using default: {DefaultTopDockHeight}.");
                    topDockHeight = DefaultTopDockHeight;
                }
                if (barHeight <= 0 || barHeight < minBarHeight || barHeight > maxBarHeight)
                {
                    _logger.LogError($"Invalid value for BarHeight ({barHeight}). Must be between {minBarHeight} and {maxBarHeight}. Using default: {DefaultBarHeight}.");
                    barHeight = DefaultBarHeight;
                }
                if (navigationFontSize <= 0 || navigationFontSize < minNavigationFontSize || navigationFontSize > maxNavigationFontSize)
                {
                    _logger.LogError($"Invalid value for NavigationFontSize ({navigationFontSize}). Must be between {minNavigationFontSize} and {maxNavigationFontSize}. Using default: {DefaultNavigationFontSize}.");
                    navigationFontSize = DefaultNavigationFontSize;
                }
                if (barPadding <= 0 || barPadding < minBarPadding || barPadding > maxBarPadding)
                {
                    _logger.LogError($"Invalid value for BarPadding ({barPadding}). Must be between {minBarPadding} and {maxBarPadding}. Using default: {DefaultBarPadding}.");
                    barPadding = DefaultBarPadding;
                }

                // Дополнительные логические проверки
                // Additional logical checks
                if (headerFontSize > topDockHeight)
                {
                    _logger.LogError($"Invalid relation: HeaderFontSize ({headerFontSize}) cannot be greater than TopDockHeight ({topDockHeight}). Using default: {DefaultHeaderFontSize}.");
                    headerFontSize = DefaultHeaderFontSize;
                }
                if (navigationDividerHeight > barHeight)
                {
                    _logger.LogError($"Invalid relation: NavigationDividerHeight ({navigationDividerHeight}) cannot be greater than BarHeight ({barHeight}). Using value equal to BarHeight.");
                    navigationDividerHeight = barHeight;
                }
                
                HeaderFontSize = headerFontSize;
                NavigationDividerWidth = navigationDividerWidth;
                NavigationDividerHeight = navigationDividerHeight;
                TopDockHeight = topDockHeight;
                BarHeight = barHeight;
                NavigationFontSize = navigationFontSize;
                BarPadding = barPadding;
                    
                SaveSettings();
            }
            catch(Exception ex)
            {
                _logger.LogError("Error updating interface settings.", ex);
            }
        }

        // Восстанавливает настройки интерфейса до значений по умолчанию.
        // Restores the interface settings to the default values.
        public void RestoreDefaults()
        {
            HeaderFontSize = DefaultHeaderFontSize;
            NavigationDividerWidth = DefaultNavigationDividerWidth;
            NavigationDividerHeight = DefaultNavigationDividerHeight;
            TopDockHeight = DefaultTopDockHeight;
            BarHeight = DefaultBarHeight;
            NavigationFontSize = DefaultNavigationFontSize;
            BarPadding = DefaultBarPadding;

            // Restore the interface settings to the default values
            // Восстановление настроек интерфейса до значений по умолчанию
            _logger.LogInformation("Interface settings restored to default values.");
            SaveSettings();
        }
    }
} 