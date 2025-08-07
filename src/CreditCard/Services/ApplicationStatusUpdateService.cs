using Microsoft.EntityFrameworkCore;

namespace CreditCard.Services
{
    public class ApplicationStatusUpdateService : BackgroundService
    {
        private readonly ILogger<ApplicationStatusUpdateService> _logger;
        private readonly IServiceProvider _services;
        private readonly TimeSpan _period = TimeSpan.FromSeconds(5);

        public ApplicationStatusUpdateService(
            ILogger<ApplicationStatusUpdateService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new PeriodicTimer(_period);

            while (!stoppingToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using AsyncServiceScope scope = _services.CreateAsyncScope();

                    var applicationService = scope.ServiceProvider.GetRequiredService<IApplicationService>();

                    await applicationService.RefreshPendingApplicationStatuses();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing periodic work");
                }
            }
        }
    }
}
