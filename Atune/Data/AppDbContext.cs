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
            var dbPath = OperatingSystem.IsAndroid() 
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtuneDB", "media_library.db")
                : Path.Combine(AppContext.BaseDirectory, "Data", "media_library.db");

            var dir = Path.GetDirectoryName(dbPath)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            Console.WriteLine($"Инициализирована БД по пути: {Path.GetFullPath(dbPath)}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.ToTable("MediaItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Artist).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Path).IsRequired();
            entity.Property(e => e.Duration)
                .HasConversion(
                    v => v.Ticks,
                    v => TimeSpan.FromTicks(v))
                .HasColumnType("BIGINT");
        });
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
} 