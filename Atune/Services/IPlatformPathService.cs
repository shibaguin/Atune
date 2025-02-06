using System;
using Microsoft.Extensions.DependencyInjection;

namespace Atune.Services
{
    public interface IPlatformPathService
    {
        // Возвращает путь для файла настроек (например, "settings.ini")
        string GetSettingsPath(string fileName = "settings.ini");
        
        // Возвращает путь для базы данных (например, "media_library.db")
        string GetDatabasePath(string databaseFileName = "media_library.db");
    }
} 