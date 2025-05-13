using System;
using System.IO;
using System.Collections.Generic;
#if ANDROID
using Android.OS;
#endif

namespace Atune.Services
{
    public class PlatformPathService : IPlatformPathService
    {
        // Добавляем статический кэш для путей
        private static readonly Dictionary<string, string> _pathCache = new(StringComparer.OrdinalIgnoreCase);

        // Объявляем поля, которые не должны содержать null.
        private readonly string _basePath;
        private readonly string _configPath;

        public PlatformPathService()
        {
            _basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) ?? string.Empty;
            _configPath = GetConfigPath() ?? string.Empty;
        }

        // Изменён метод: теперь возвращает string вместо string?
        private static string GetConfigPath()
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

        public string GetSettingsPath(string fileName)
        {
            string key = "settings_" + fileName;
            if (_pathCache.TryGetValue(key, out string? cachedPath))
                return cachedPath!;

            string path;
            if (OperatingSystem.IsAndroid())
            {
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (string.IsNullOrEmpty(baseDir))
                {
#if ANDROID
                    baseDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDocuments)?.AbsolutePath;
#endif
                }
                path = Path.Combine(baseDir ?? string.Empty, "Atune", fileName);
            }
            else if (OperatingSystem.IsWindows())
            {
                path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Atune",
                    fileName);
            }
            else // Linux и другие Unix-системы
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                path = Path.Combine(homeDir, ".config", "Atune", fileName);
            }

            // Создаем директорию, если она не существует
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
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
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                if (string.IsNullOrEmpty(baseDir))
                {
#if ANDROID
                    baseDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDocuments)?.AbsolutePath;
#endif
                }
                path = Path.Combine(baseDir ?? string.Empty, databaseFileName);
            }
            else if (OperatingSystem.IsWindows())
            {
                path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Atune",
                    "Data",
                    databaseFileName);
            }
            else // Linux и другие Unix-системы
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                path = Path.Combine(homeDir, ".local", "share", "Atune", databaseFileName);
            }

            // Создаем директорию, если она не существует
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _pathCache[key] = path;
            return path;
        }

        public string GetPluginsDirectory()
        {
            if (OperatingSystem.IsAndroid())
            {
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (string.IsNullOrEmpty(baseDir))
                {
#if ANDROID
                    baseDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDocuments)?.AbsolutePath;
#endif
                }
                return Path.Combine(baseDir ?? string.Empty, "AtunePlugins");
            }
            else if (OperatingSystem.IsWindows())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Atune",
                    "Plugins");
            }
            else // Linux и другие Unix-системы
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(homeDir, ".local", "share", "Atune", "plugins");
            }
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
