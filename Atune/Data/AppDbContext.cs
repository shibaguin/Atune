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
    public DbSet<Album> Albums { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<PlaylistMediaItem> PlaylistMediaItems { get; set; }
    public DbSet<PlaybackQueueItem> PlaybackQueueItems { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options) 
    {
        var dbPath = Database.GetDbConnection().DataSource;
        Console.WriteLine($"Using DB: {Path.GetFullPath(dbPath)}");
        Console.WriteLine($"Application directory: {AppContext.BaseDirectory}");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string dbPath = GetDatabasePath();
            Console.WriteLine($"Database path: {dbPath}");

            EnsureDatabaseDirectory(dbPath);
            CreateDatabaseFile(dbPath);

            optionsBuilder.UseSqlite($"Data Source={dbPath};")
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
        }
    }

    private string GetDatabasePath()
    {
        if (OperatingSystem.IsAndroid())
        {
            // Use a special path for Android
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
        // Явно указываем порядок применения миграций
        modelBuilder.HasAnnotation("Relational:MigrationHistoryTable", "__EFMigrationsHistory");
        modelBuilder.HasAnnotation("Relational:MigrationHistoryTableSchema", null);
        
        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.ToTable("MediaItems");
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.Path)
                  .IsUnique()
                  .HasDatabaseName("IX_MediaItems_Path");
                  
            entity.HasIndex(e => new { e.AlbumId, e.Title })
                  .HasDatabaseName("IX_MediaItems_Album_Title");
                  
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Path)
                .IsRequired();
                
            entity.Property(e => e.AlbumId)
                .IsRequired();
                
            entity.Property(e => e.Duration)
                .HasConversion(
                    v => v.Ticks,
                    v => TimeSpan.FromTicks(v))
                .HasColumnType("BIGINT");
        });

        modelBuilder.Entity<Album>(entity =>
        {
            entity.ToTable("Albums");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Title).IsRequired();
        });

        modelBuilder.Entity<Artist>(entity =>
        {
            entity.ToTable("Artists");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).IsRequired();
        });

        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.ToTable("Playlists");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired();
        });

        modelBuilder.Entity<AlbumArtist>(entity =>
        {
            entity.HasKey(aa => new { aa.AlbumId, aa.ArtistId });
            
            entity.HasOne(aa => aa.Album)
                  .WithMany(a => a.AlbumArtists)
                  .HasForeignKey(aa => aa.AlbumId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(aa => aa.Artist)
                  .WithMany(a => a.AlbumArtists)
                  .HasForeignKey(aa => aa.ArtistId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrackArtist>(entity =>
        {
            entity.HasKey(ta => new { ta.MediaItemId, ta.ArtistId });
            
            entity.HasOne(ta => ta.MediaItem)
                  .WithMany(m => m.TrackArtists)
                  .HasForeignKey(ta => ta.MediaItemId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(ta => ta.Artist)
                  .WithMany(a => a.TrackArtists)
                  .HasForeignKey(ta => ta.ArtistId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlaylistMediaItem>(entity =>
        {
            entity.HasKey(pmi => new { pmi.PlaylistId, pmi.MediaItemId });
            
            entity.HasOne(pmi => pmi.Playlist)
                  .WithMany(p => p.PlaylistMediaItems)
                  .HasForeignKey(pmi => pmi.PlaylistId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(pmi => pmi.MediaItem)
                  .WithMany(m => m.PlaylistMediaItems)
                  .HasForeignKey(pmi => pmi.MediaItemId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.Property(pmi => pmi.Position).IsRequired();
            entity.Property(pmi => pmi.AddedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<MediaItem>()
            .HasOne(m => m.Album)
            .WithMany(a => a.Tracks)
            .HasForeignKey(m => m.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MediaItem>().ToTable(nameof(MediaItems), t => 
            t.ExcludeFromMigrations(false));

        modelBuilder.Entity<MediaItem>()
            .HasIndex(m => m.Path)
            .IsUnique();

        modelBuilder.Entity<PlaybackQueueItem>(entity =>
        {
            entity.ToTable("PlaybackQueueItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MediaItemId).IsRequired();
            entity.Property(e => e.Position).IsRequired();
            entity.Property(e => e.AddedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.MediaItem)
                  .WithMany()
                  .HasForeignKey(e => e.MediaItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public IQueryable<MediaItem> SafeMedia => MediaItems?.AsQueryable() ?? throw new InvalidOperationException("MediaItems DbSet not initialized");

    public async Task<int> SaveChangesAsync()
    {
        try
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
        catch (Exception ex)
        {
            // Логируем ошибку
            throw new InvalidOperationException("Error saving changes to the database", ex);
        }
    }

    public async Task<List<MediaItem>> GetAllMediaAsync()
    {
        try
        {
            // Используем Include для загрузки связанных сущностей Album и TrackArtists, затем Include для Artist
            return await MediaItems
                .Include(m => m.Album)
                .Include(m => m.TrackArtists)
                    .ThenInclude(ta => ta.Artist)
                .AsNoTracking()
                .OrderBy(m => m.Title)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            throw new InvalidOperationException("Error retrieving all media items", ex);
        }
    }

    public async Task<MediaItem?> GetMediaByIdAsync(int id)
    {
        try
        {
            return await MediaItems
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            throw new InvalidOperationException("Error retrieving media item by ID", ex);
        }
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
        catch (Exception ex)
        {
            // Логируем ошибку
            throw new InvalidOperationException("Error initializing database", ex);
        }
        finally 
        {
            await Database.CloseConnectionAsync();
        }
    }

    public async Task<bool> ExistsByPathAsync(string path)
    {
        try
        {
            return await MediaItems.AnyAsync(m => m.Path == path);
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            throw new InvalidOperationException("Error checking existence by path", ex);
        }
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

    public async Task BulkUpdateAsync(IEnumerable<MediaItem> entities, Action<BulkUpdateOptions>? configureOptions = null)
    {
        var options = new BulkUpdateOptions();
        configureOptions?.Invoke(options);
        
        int count = 0;
        foreach (var entity in entities)
        {
            Entry(entity).State = EntityState.Modified;
            count++;
            if (options.BatchSize > 0 && count % options.BatchSize == 0)
            {
                await SaveChangesAsync();
            }
        }
        await SaveChangesAsync();
    }

    public async Task BulkDeleteAsync(IEnumerable<MediaItem> entities, Action<BulkDeleteOptions>? configureOptions = null)
    {
        var options = new BulkDeleteOptions();
        configureOptions?.Invoke(options);
        
        int count = 0;
        foreach (var entity in entities)
        {
            Remove(entity);
            count++;
            if (options.BatchSize > 0 && count % options.BatchSize == 0)
            {
                await SaveChangesAsync();
            }
        }
        await SaveChangesAsync();
    }

    public async Task<List<MediaItem>> GetMediaItemsAsync()
    {
        return await MediaItems.ToListAsync();
    }

}

public class BulkInsertOptions
{
    public int BatchSize { get; set; } = 100;
    public bool InsertKeepIdentity { get; set; }
}

public class BulkUpdateOptions
{
    public int BatchSize { get; set; } = 100;
}

public class BulkDeleteOptions
{
    public int BatchSize { get; set; } = 100;
} 