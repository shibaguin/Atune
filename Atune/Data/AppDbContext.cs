using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Atune.Models;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;

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
} 