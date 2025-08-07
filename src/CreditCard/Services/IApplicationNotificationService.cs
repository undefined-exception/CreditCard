namespace CreditCard.Services
{
    public interface IApplicationNotificationService
    {
        Task NotifyApplicationStatusChange(Guid applicationId);
    }
}
