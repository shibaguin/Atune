using System;
using Microsoft.Extensions.DependencyInjection;

namespace Atune.Services
{
    public interface IPlatformPathService
    {
        // Returns the path for the settings file (e.g., "settings.ini")
        // Возвращает путь к файлу настроек (например, "settings.ini")
        string GetSettingsPath(string fileName = "settings.ini");

        // Returns the path for the database file (e.g., "media_library.db")
        // Возвращает путь к файлу базы данных (например, "media_library.db")
        string GetDatabasePath(string databaseFileName = "media_library.db");

        // Returns the directory for plugin files
        string GetPluginsDirectory();

        // Returns the directory for storing cover art files
        string GetCoversDirectory();
        // Returns the full path to the default cover image file in the covers directory
        string GetDefaultCoverPath();
    }
}
