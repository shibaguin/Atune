using Microsoft.EntityFrameworkCore;
using Atune.Models;

public class AppDbContext : DbContext
{
    public DbSet<MediaItem> MediaItems { get; set; }

    public AppDbContext() { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=media_library.db");
    }
} 