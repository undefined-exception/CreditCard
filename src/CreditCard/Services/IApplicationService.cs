using CreditCard.Models;

namespace CreditCard.Services
{
    public interface IApplicationService
    {
        Task<List<ApplicationModel>> ListApplications();

        Task<ApplicationModel> SubmitApplication(ApplicationInputModel applicationInputModel);

        Task RefreshPendingApplicationStatuses();
    }
}
