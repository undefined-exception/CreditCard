using CreditCard.Models;

namespace CreditCard.Services
{
    public interface IApplicationApiService
    {
        Task<ApplicationSubmissionResponse> SubmitApplication(ApplicationInputModel applicationInputModel);

        Task<ApplicationSubmissionResponse> GetApplicationStatus(string applicationId);
    }
}
