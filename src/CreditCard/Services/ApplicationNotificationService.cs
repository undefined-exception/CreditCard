using Microsoft.ApplicationInsights;

namespace CreditCard.Services
{
    public class ApplicationNotificationService : IApplicationNotificationService
    {
        private readonly TelemetryClient telemetryClient;

        public ApplicationNotificationService(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        public Task NotifyApplicationStatusChange(Guid applicationId)
        {
            telemetryClient.TrackEvent("ApplicationStatusChange", new Dictionary<string, string>
            {
                { "applicationId", applicationId.ToString() }
            });

            return Task.CompletedTask;
        }
    }
}
