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
                // For Android, use MyDocuments for storing settings
                // Для Android используйте MyDocuments для хранения настроек
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            }
            // For desktop OS, use ApplicationData + application folder
            // Для операционной системы настольного компьютера используйте ApplicationData + папку приложения
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atune", fileName);
        }

        public string GetDatabasePath(string databaseFileName = "media_library.db")
        {
            if (OperatingSystem.IsAndroid())
            {
                // For Android, use Personal for storing database
                // Для Android используйте Personal для хранения базы данных
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), databaseFileName);
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atune", "Data", databaseFileName);
        }

        public string GetPluginsDirectory()
        {
            if (OperatingSystem.IsAndroid())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                    "AtunePlugins");
            }
            
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "Atune", 
                "Plugins");
        }
    }
} 