using CreditCard.Data;
using Microsoft.EntityFrameworkCore;

namespace CreditCard.Extensions
{
    public static class DatabaseExtensions
    {
        public static IHost MigrateDatabase(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                logger.LogInformation("MigrateDatabase");
                dbContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Database migration failed. Application startup aborted.");
                throw new InvalidOperationException("Database migration failed. Application startup aborted.", ex);
            }

            return host;
        }
    }
}