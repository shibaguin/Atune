using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Определяем базовый путь для хранения базы данных в зависимости от ОС
        string basePath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Для Linux используем переменную окружения HOME
            basePath = Environment.GetEnvironmentVariable("HOME") ??
                throw new Exception("HOME environment variable is not set for Linux platform.");
        }
        else
        {
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        string dbPath = Path.Combine(
            basePath,
            "Atune",
            "Data",
            "media_library.db");
        
        // Создаем директорию, если не существует
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
        
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        
        return new AppDbContext(optionsBuilder.Options);
    }
} 