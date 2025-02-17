using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Linq;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=media_library.db");
        
        var context = new AppDbContext(optionsBuilder.Options);
        
        // Synchronous execution of migrations
        if (context.Database.GetPendingMigrationsAsync().GetAwaiter().GetResult().Any())
        {
            context.Database.MigrateAsync().GetAwaiter().GetResult();
        }
        
        return context;
    }
} 