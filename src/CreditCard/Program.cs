using CreditCard.Extensions;

namespace CreditCard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            try
            {
                host.MigrateDatabase(); // Will throw an exception if migration fails
            }
            catch (Exception)
            {
                using var scope = host.Services.CreateScope();

                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Application shutting down.");
                throw;
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
