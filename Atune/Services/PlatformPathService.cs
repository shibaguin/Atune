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
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            }
            // For desktop OS, use ApplicationData + application folder
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atune", fileName);
        }

        public string GetDatabasePath(string databaseFileName = "media_library.db")
        {
            if (OperatingSystem.IsAndroid())
            {
                // For Android, use Personal for storing database
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), databaseFileName);
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atune", "Data", databaseFileName);
        }
    }
} 