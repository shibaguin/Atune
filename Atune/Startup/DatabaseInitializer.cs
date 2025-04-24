using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Atune.Data;

namespace Atune.Startup
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try
            {
                Log.Information("Initializing database...");

                var pending = await db.Database.GetPendingMigrationsAsync();
                if (pending.Any())
                {
                    Log.Information($"Applying {pending.Count()} pending migrations...");
                    await db.Database.MigrateAsync();
                }

                var exists = await db.MediaItems.AnyAsync();
                Log.Information($"Database status: {(exists ? "OK" : "EMPTY")}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database initialization failed");
                throw;
            }
        }
    }
} 