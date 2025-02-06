using System;
using System.IO;

namespace Atune.Services
{
    public class PlatformPathService : IPlatformPathService
    {
        public string GetSettingsPath(string fileName = "settings.ini")
        {
            if (OperatingSystem.IsAndroid())
            {
                // Для Android используем MyDocuments для хранения настроек
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            }
            // Для десктопных ОС используем ApplicationData + папку приложения
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atune", fileName);
        }

        public string GetDatabasePath(string databaseFileName = "media_library.db")
        {
            if (OperatingSystem.IsAndroid())
            {
                // Для Android используем Personal для хранения базы данных
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), databaseFileName);
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atune", "Data", databaseFileName);
        }
    }
} 