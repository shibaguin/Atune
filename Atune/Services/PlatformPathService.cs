using System;
using System.IO;
using System.Collections.Generic;

namespace Atune.Services
{
    public class PlatformPathService : IPlatformPathService
    {
        // Добавляем статический кэш для путей
        private static readonly Dictionary<string, string> _pathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Объявляем поля, которые не должны содержать null.
        private readonly string _basePath;
        private readonly string _configPath;

        public PlatformPathService()
        {
            _basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) ?? string.Empty;
            _configPath = GetConfigPath() ?? string.Empty;
        }

        // Изменён метод: теперь возвращает string вместо string?
        private string GetConfigPath()
        {
            // Ваша логика для получения пути конфигурации.
            // Если нет данных – возвращаем пустую строку.
            return string.Empty;
        }

        public string GetPlatformPath()
        {
            return _basePath;
        }

        public string GetConfigurationPath()
        {
            return _configPath ?? string.Empty;
        }

        public string GetSettingsPath(string fileName = "settings.ini")
        {
            string key = "settings_" + fileName;
            if (_pathCache.TryGetValue(key, out string? cachedPath))
                return cachedPath!;
        
            string path;
            if (OperatingSystem.IsAndroid())
            {
                path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) ?? string.Empty, 
                    fileName);
            }
            else
            {
                path = Path.Combine(_basePath, "Atune", fileName);
            }
            _pathCache[key] = path;
            return path;
        }

        public string GetDatabasePath(string databaseFileName = "media_library.db")
        {
            string key = "database_" + databaseFileName;
            if (_pathCache.TryGetValue(key, out string? cachedPath))
                return cachedPath!;
        
            string path;
            if (OperatingSystem.IsAndroid())
            {
                path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal) ?? string.Empty, 
                    databaseFileName);
            }
            else
            {
                path = Path.Combine(_basePath, "Atune", "Data", databaseFileName);
            }
            _pathCache[key] = path;
            return path;
        }

        public string GetPluginsDirectory()
        {
            if (OperatingSystem.IsAndroid())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) ?? string.Empty, 
                    "AtunePlugins");
            }
            
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) ?? string.Empty, 
                "Atune", 
                "Plugins");
        }

        // Returns the directory where cover art files are stored
        public string GetCoversDirectory()
        {
            string baseDir;
            if (OperatingSystem.IsAndroid())
            {
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) ?? string.Empty;
            }
            else if (OperatingSystem.IsLinux())
            {
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) ?? string.Empty;
            }
            else
            {
                // Windows and others
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) ?? string.Empty;
            }
            return Path.Combine(baseDir, "Atune", "Covers", "Default covers");
        }

        // Returns the full path to the default cover image file
        public string GetDefaultCoverPath()
        {
            return Path.Combine(GetCoversDirectory(), "default_cover.jpg");
        }
    }
} 
