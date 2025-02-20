using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices; // Добавлено для проверки платформы

namespace Atune.Services
{
    public class InterfaceSettingsService : IInterfaceSettingsService
    {
        // Default значения, соответствующие текущим параметрам
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
            Directory.CreateDirectory(Path.GetDirectoryName(_iniFilePath)!);
            LoadSettings();
        }
        
        public void LoadSettings()
        {
            // Если файл настроек не найден, устанавливаем дефолтные значения и сразу сохраняем их,
            // чтобы файл содержал размеры интерфейса с первого запуска.
            if (!File.Exists(_iniFilePath))
            {
                _logger.LogInformation($"Файл настроек не найден по пути: {_iniFilePath}. Создаем файл с дефолтными значениями.");
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
            try
            {
                var iniData = File.ReadAllLines(_iniFilePath);
                // Пример парсинга строки (реальная реализация может отличаться)
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
                _logger.LogError("Ошибка при загрузке настроек интерфейса", ex);
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
        
        // Новый метод для сохранения настроек интерфейса в settings.ini
        public void SaveSettings()
        {
            var lines = new List<string>
            {
                "[InterfaceDimensions]",
                $"HeaderFontSize={HeaderFontSize}",
                $"NavigationDividerWidth={NavigationDividerWidth}",
                $"NavigationDividerHeight={NavigationDividerHeight}",
                $"TopDockHeight={TopDockHeight}",
                $"BarHeight={BarHeight}",
                $"NavigationFontSize={NavigationFontSize}",
                $"BarPadding={BarPadding}"
            };

            try
            {
                File.WriteAllLines(_iniFilePath, lines);
                _logger.LogInformation("Настройки интерфейса успешно сохранены в settings.ini");
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при сохранении настроек интерфейса в settings.ini", ex);
            }
        }
        
        // Новый метод для обновления настроек интерфейса из UI и сохранения в settings.ini
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
                // Задаем числовые ограничения напрямую (±10% от значения по умолчанию)
                double minHeaderFontSize = 21.6;    // 24 - 2.4
                double maxHeaderFontSize = 26.4;    // 24 + 2.4

                double minNavigationDividerWidth = 2.7;  // 3 - 0.3
                double maxNavigationDividerWidth = 3.3;  // 3 + 0.3

                double minNavigationDividerHeight = 45;  // 50 - 5
                double maxNavigationDividerHeight = 55;  // 50 + 5

                double minTopDockHeight = 45;    // 50 - 5
                double maxTopDockHeight = 55;    // 50 + 5

                double minBarHeight = 45;        // 50 - 5
                double maxBarHeight = 55;        // 50 + 5

                double minNavigationFontSize = 12.6;    // 14 - 1.4
                double maxNavigationFontSize = 15.4;    // 14 + 1.4

                double minBarPadding = 7.2;      // 8 - 0.8
                double maxBarPadding = 8.8;      // 8 + 0.8

                if (headerFontSize <= 0 || headerFontSize < minHeaderFontSize || headerFontSize > maxHeaderFontSize)
                {
                    _logger.LogError($"Неверное значение HeaderFontSize ({headerFontSize}). Должно быть от {minHeaderFontSize} до {maxHeaderFontSize}. Используется значение по умолчанию: {DefaultHeaderFontSize}.");
                    headerFontSize = DefaultHeaderFontSize;
                }
                if (navigationDividerWidth <= 0 || navigationDividerWidth < minNavigationDividerWidth || navigationDividerWidth > maxNavigationDividerWidth)
                {
                    _logger.LogError($"Неверное значение NavigationDividerWidth ({navigationDividerWidth}). Должно быть от {minNavigationDividerWidth} до {maxNavigationDividerWidth}. Используется значение по умолчанию: {DefaultNavigationDividerWidth}.");
                    navigationDividerWidth = DefaultNavigationDividerWidth;
                }
                if (navigationDividerHeight <= 0 || navigationDividerHeight < minNavigationDividerHeight || navigationDividerHeight > maxNavigationDividerHeight)
                {
                    _logger.LogError($"Неверное значение NavigationDividerHeight ({navigationDividerHeight}). Должно быть от {minNavigationDividerHeight} до {maxNavigationDividerHeight}. Используется значение по умолчанию: {DefaultNavigationDividerHeight}.");
                    navigationDividerHeight = DefaultNavigationDividerHeight;
                }
                if (topDockHeight <= 0 || topDockHeight < minTopDockHeight || topDockHeight > maxTopDockHeight)
                {
                    _logger.LogError($"Неверное значение TopDockHeight ({topDockHeight}). Должно быть от {minTopDockHeight} до {maxTopDockHeight}. Используется значение по умолчанию: {DefaultTopDockHeight}.");
                    topDockHeight = DefaultTopDockHeight;
                }
                if (barHeight <= 0 || barHeight < minBarHeight || barHeight > maxBarHeight)
                {
                    _logger.LogError($"Неверное значение BarHeight ({barHeight}). Должно быть от {minBarHeight} до {maxBarHeight}. Используется значение по умолчанию: {DefaultBarHeight}.");
                    barHeight = DefaultBarHeight;
                }
                if (navigationFontSize <= 0 || navigationFontSize < minNavigationFontSize || navigationFontSize > maxNavigationFontSize)
                {
                    _logger.LogError($"Неверное значение NavigationFontSize ({navigationFontSize}). Должно быть от {minNavigationFontSize} до {maxNavigationFontSize}. Используется значение по умолчанию: {DefaultNavigationFontSize}.");
                    navigationFontSize = DefaultNavigationFontSize;
                }
                if (barPadding <= 0 || barPadding < minBarPadding || barPadding > maxBarPadding)
                {
                    _logger.LogError($"Неверное значение BarPadding ({barPadding}). Должно быть от {minBarPadding} до {maxBarPadding}. Используется значение по умолчанию: {DefaultBarPadding}.");
                    barPadding = DefaultBarPadding;
                }

                // Дополнительные логические проверки
                if (headerFontSize > topDockHeight)
                {
                    _logger.LogError($"Неверное соотношение: HeaderFontSize ({headerFontSize}) не может быть больше TopDockHeight ({topDockHeight}). Используется значение по умолчанию: {DefaultHeaderFontSize}.");
                    headerFontSize = DefaultHeaderFontSize;
                }
                if (navigationDividerHeight > barHeight)
                {
                    _logger.LogError($"Неверное соотношение: NavigationDividerHeight ({navigationDividerHeight}) не может быть больше BarHeight ({barHeight}). Используем значение равное BarHeight.");
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
                _logger.LogError("Ошибка при обновлении настроек интерфейса", ex);
            }
        }

        // Новый метод для восстановления настроек интерфейса по умолчанию
        public void RestoreDefaults()
        {
            HeaderFontSize = DefaultHeaderFontSize;
            NavigationDividerWidth = DefaultNavigationDividerWidth;
            NavigationDividerHeight = DefaultNavigationDividerHeight;
            TopDockHeight = DefaultTopDockHeight;
            BarHeight = DefaultBarHeight;
            NavigationFontSize = DefaultNavigationFontSize;
            BarPadding = DefaultBarPadding;

            _logger.LogInformation("Настройки интерфейса восстановлены до значений по умолчанию.");
            SaveSettings();
        }
    }
} 