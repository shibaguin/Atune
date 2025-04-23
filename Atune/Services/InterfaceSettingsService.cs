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

        // Добавляем кэш для секций INI файла
        private Dictionary<string, List<string>>? _iniCache;

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
            if (!File.Exists(_iniFilePath))
            {
                _logger.LogInformation($"Settings file not found at {_iniFilePath}. Creating new file with default values.");
                SetDefaults();
                SaveSettings();
                return;
            }
            
            // Используем кэш, если он уже загружен
            Dictionary<string, List<string>> sections;
            if (_iniCache != null)
            {
                sections = _iniCache;
            }
            else
            {
                sections = ParseIniFile();
                _iniCache = sections;
            }
            
            if (!sections.ContainsKey("InterfaceDimensions"))
            {
                _logger.LogWarning("Section [InterfaceDimensions] not found in INI file. Using default interface settings.");
                SetDefaults();
                SaveSettings();
                return;
            }
            
            var section = sections["InterfaceDimensions"];
            // Разбираем секцию в словарь ключ-значение
            var kv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in section)
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                {
                    _logger.LogWarning($"Ignoring malformed line in InterfaceDimensions section: '{line}'");
                    continue;
                }
                kv[parts[0].Trim()] = parts[1].Trim();
            }
            
            // Безопасное чтение настроек с проверкой корректности значений
            if (!kv.TryGetValue("HeaderFontSize", out var headerStr) || !double.TryParse(headerStr, out double headerValue))
            {
                _logger.LogWarning("Invalid or missing HeaderFontSize. Using default.");
                headerValue = DefaultHeaderFontSize;
            }
            if (!kv.TryGetValue("NavigationDividerWidth", out var navWidthStr) || !double.TryParse(navWidthStr, out double navWidth))
            {
                _logger.LogWarning("Invalid or missing NavigationDividerWidth. Using default.");
                navWidth = DefaultNavigationDividerWidth;
            }
            if (!kv.TryGetValue("NavigationDividerHeight", out var navHeightStr) || !double.TryParse(navHeightStr, out double navHeight))
            {
                _logger.LogWarning("Invalid or missing NavigationDividerHeight. Using default.");
                navHeight = DefaultNavigationDividerHeight;
            }
            if (!kv.TryGetValue("TopDockHeight", out var topDockStr) || !double.TryParse(topDockStr, out double topDock))
            {
                _logger.LogWarning("Invalid or missing TopDockHeight. Using default.");
                topDock = DefaultTopDockHeight;
            }
            if (!kv.TryGetValue("BarHeight", out var barHeightStr) || !double.TryParse(barHeightStr, out double barHeight))
            {
                _logger.LogWarning("Invalid or missing BarHeight. Using default.");
                barHeight = DefaultBarHeight;
            }
            if (!kv.TryGetValue("NavigationFontSize", out var navFontStr) || !double.TryParse(navFontStr, out double navFont))
            {
                _logger.LogWarning("Invalid or missing NavigationFontSize. Using default.");
                navFont = DefaultNavigationFontSize;
            }
            if (!kv.TryGetValue("BarPadding", out var barPaddingStr) || !double.TryParse(barPaddingStr, out double barPadding))
            {
                _logger.LogWarning("Invalid or missing BarPadding. Using default.");
                barPadding = DefaultBarPadding;
            }
            
            // Дополнительные проверки логических зависимостей
            if (headerValue > topDock)
            {
                _logger.LogError($"HeaderFontSize ({headerValue}) cannot be greater than TopDockHeight ({topDock}). Using default HeaderFontSize.");
                headerValue = DefaultHeaderFontSize;
            }
            if (navHeight > barHeight)
            {
                _logger.LogError($"NavigationDividerHeight ({navHeight}) cannot be greater than BarHeight ({barHeight}). Using BarHeight value.");
                navHeight = barHeight;
            }
            
            HeaderFontSize = headerValue;
            NavigationDividerWidth = navWidth;
            NavigationDividerHeight = navHeight;
            TopDockHeight = topDock;
            BarHeight = barHeight;
            NavigationFontSize = navFont;
            BarPadding = barPadding;
            
            SaveSettings();
        }
        
        private void SetDefaults()
        {
            HeaderFontSize = DefaultHeaderFontSize;
            NavigationDividerWidth = DefaultNavigationDividerWidth;
            NavigationDividerHeight = DefaultNavigationDividerHeight;
            TopDockHeight = DefaultTopDockHeight;
            BarHeight = DefaultBarHeight;
            NavigationFontSize = DefaultNavigationFontSize;
            BarPadding = DefaultBarPadding;
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
        
        // Новый метод для разбора INI-файла на секции
        private Dictionary<string, List<string>> ParseIniFile()
        {
            var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            List<string>? currentSectionLines = null;
            string currentSectionName = string.Empty;

            if (File.Exists(_iniFilePath))
            {
                foreach (var line in File.ReadAllLines(_iniFilePath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        // Начало новой секции
                        currentSectionName = trimmed.TrimStart('[').TrimEnd(']');
                        if (!sections.ContainsKey(currentSectionName))
                        {
                            sections[currentSectionName] = new List<string>();
                        }
                        currentSectionLines = sections[currentSectionName];
                    }
                    else
                    {
                        if (currentSectionLines != null)
                        {
                            currentSectionLines.Add(line);
                        }
                        // Если строка находится вне секции, её можно игнорировать или сохранять в секцию по умолчанию
                    }
                }
            }
            return sections;
        }

        // Новый метод для записи словаря секций обратно в INI-файл
        private void WriteIniFile(Dictionary<string, List<string>> sections)
        {
            var lines = new List<string>();
            foreach (var kvp in sections)
            {
                lines.Add($"[{kvp.Key}]");
                lines.AddRange(kvp.Value);
                lines.Add(string.Empty); // пустая строка для отделения секций
            }
            File.WriteAllLines(_iniFilePath, lines);
        }

        // Изменяем метод SaveSettings(), чтобы обновлять только секцию [InterfaceDimensions]
        public void SaveSettings()
        {
            // Готовим содержимое секции для настроек интерфейса
            var interfaceSection = new List<string>
            {
                $"HeaderFontSize={HeaderFontSize}",
                $"NavigationDividerWidth={NavigationDividerWidth}",
                $"NavigationDividerHeight={NavigationDividerHeight}",
                $"TopDockHeight={TopDockHeight}",
                $"BarHeight={BarHeight}",
                $"NavigationFontSize={NavigationFontSize}",
                $"BarPadding={BarPadding}"
            };

            // Разбираем текущее содержимое файла на секции (если файл существует)
            var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(_iniFilePath))
            {
                sections = ParseIniFile();
            }

            // Обновляем (или добавляем) секцию InterfaceDimensions
            sections["InterfaceDimensions"] = interfaceSection;

            // Записываем обновлённый словарь секций обратно в файл
            WriteIniFile(sections);

            // После сохранения обновляем глобальные ресурсы приложения
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
