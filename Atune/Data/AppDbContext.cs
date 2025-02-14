using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Atune.Models;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Threading.Tasks;

[RequiresUnreferencedCode("EF Core may require unreferenced code")]
public class AppDbContext : DbContext
{
    public DbSet<MediaItem> MediaItems { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options) 
    {
        var dbPath = Database.GetDbConnection().DataSource;
        Console.WriteLine($"Используемая БД: {Path.GetFullPath(dbPath)}");
        Console.WriteLine($"Директория приложения: {AppContext.BaseDirectory}");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string dbPath = GetDatabasePath();
            Console.WriteLine($"Database path: {dbPath}");

            EnsureDatabaseDirectory(dbPath);
            CreateDatabaseFile(dbPath);

            optionsBuilder.UseSqlite($"Data Source={dbPath};");
        }
    }

    private string GetDatabasePath()
    {
        if (OperatingSystem.IsAndroid())
        {
            // Используем специальный путь для Android
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "atune_media.db");
        }
        
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Atune",
            "Data",
            "media_library.db");
    }

    private void EnsureDatabaseDirectory(string dbPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(dbPath);
            if (string.IsNullOrEmpty(dir))
                throw new InvalidOperationException("Invalid database directory");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                Console.WriteLine($"Created database directory: {dir}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL DIRECTORY ERROR: {ex}");
            throw;
        }
    }

    private void CreateDatabaseFile(string dbPath)
    {
        try
        {
            if (!File.Exists(dbPath))
            {
                Console.WriteLine("Creating new database file");
                using (File.Create(dbPath)) { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL FILE CREATION ERROR: {ex}");
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.ToTable("MediaItems");
            entity.HasKey(e => e.Id);
            
            // Добавляем индексы для часто используемых полей
            entity.HasIndex(e => e.Path)
                  .IsUnique()
                  .HasDatabaseName("IX_MediaItems_Path");
                  
            entity.HasIndex(e => e.Artist)
                  .HasDatabaseName("IX_MediaItems_Artist");
                  
            entity.HasIndex(e => e.Genre)
                  .HasDatabaseName("IX_MediaItems_Genre");
                  
            entity.HasIndex(e => new { e.Artist, e.Title })
                  .HasDatabaseName("IX_MediaItems_Artist_Title");

            // Существующие настройки
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Artist).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Path).IsRequired();
            entity.Property(e => e.Duration)
                .HasConversion(
                    v => v.Ticks,
                    v => TimeSpan.FromTicks(v))
                .HasColumnType("BIGINT");
        });

        // Явно создаем таблицу если не существует
        modelBuilder.Entity<MediaItem>().ToTable(nameof(MediaItems), t => 
            t.ExcludeFromMigrations(false));

        modelBuilder.Entity<MediaItem>()
            .HasIndex(m => m.Path)
            .IsUnique();
        
        modelBuilder.Entity<MediaItem>()
            .HasIndex(m => new { m.Artist, m.Album });
    }

    public IQueryable<MediaItem> SafeMedia => MediaItems?.AsQueryable() ?? throw new InvalidOperationException("MediaItems DbSet not initialized");

    public async Task<int> SaveChangesAsync()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is MediaItem && 
                (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var validationContext = new ValidationContext(entityEntry.Entity);
            Validator.ValidateObject(entityEntry.Entity, validationContext, true);
        }

        return await base.SaveChangesAsync();
    }

    public async Task<List<MediaItem>> GetAllMediaAsync()
    {
        return await MediaItems
            .AsNoTracking()
            .OrderBy(m => m.Title)
            .ToListAsync();
    }

    public async Task<MediaItem?> GetMediaByIdAsync(int id)
    {
        return await MediaItems
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task AddMediaAsync(MediaItem media)
    {
        await MediaItems.AddAsync(media);
        await SaveChangesAsync();
    }

    public async Task RemoveMediaAsync(int id)
    {
        var media = await MediaItems.FindAsync(id);
        if (media != null)
        {
            MediaItems.Remove(media);
            await SaveChangesAsync();
        }
    }

    public async Task InitializeDatabase()
    {
        try 
        {
            await Database.OpenConnectionAsync();
        }
        finally 
        {
            await Database.CloseConnectionAsync();
        }
    }

    public async Task<bool> ExistsByPathAsync(string path)
    {
        return await MediaItems.AnyAsync(m => m.Path == path);
    }

    public async Task BulkInsertAsync(IEnumerable<MediaItem> entities, Action<BulkInsertOptions>? configureOptions = null)
    {
        var options = new BulkInsertOptions();
        configureOptions?.Invoke(options);
        
        foreach (var entity in entities)
        {
            await MediaItems.AddAsync(entity);
            if (options.BatchSize > 0 && MediaItems.Local.Count % options.BatchSize == 0)
            {
                await SaveChangesAsync();
            }
        }
        await SaveChangesAsync();
    }
}

public class BulkInsertOptions
{
    public int BatchSize { get; set; } = 100;
    public bool InsertKeepIdentity { get; set; }
} 