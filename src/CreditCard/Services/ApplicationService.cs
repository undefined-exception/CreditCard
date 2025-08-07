using CreditCard.Data;
using CreditCard.Models;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CreditCard.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly ApplicationDbContext db;


        private readonly ILogger<ApplicationService> logger;

        private readonly IApplicationApiService applicationApiService;

        private readonly TelemetryClient telemetryClient;

        private readonly IApplicationNotificationService applicationNotificationService;

        public ApplicationService(
            IApplicationApiService applicationApiService,
            ApplicationDbContext db,
            ILogger<ApplicationService> logger,
            TelemetryClient telemetryClient,
            IApplicationNotificationService applicationNotificationService)
        {
            this.applicationApiService = applicationApiService;
            this.db = db;
            this.logger = logger;
            this.telemetryClient = telemetryClient;
            this.applicationNotificationService = applicationNotificationService;
        }

        public async Task<List<ApplicationModel>> ListApplications()
        {
            var applications = await db.Applications
                .ToListAsync();

            return applications;
        }

        public async Task<ApplicationModel> SubmitApplication(ApplicationInputModel applicationInputModel)
        {
            telemetryClient.TrackEvent("ApplicationSubmitStarted", new Dictionary<string, string>
            {
                { "SSN", CalulateHash(applicationInputModel.SSN)}
            });

            var dbApplication = db.Applications.Add(new ApplicationModel
            {
                AnnualIncome = applicationInputModel.AnnualIncome,
                SSN = applicationInputModel.SSN,
                DateOfBirth = applicationInputModel.DateOfBirth,
                EmploymentStatus = applicationInputModel.EmploymentStatus,
                FirstName = applicationInputModel.FirstName,
                HousingStatus = applicationInputModel.HousingStatus,
                LastName = applicationInputModel.LastName,
                MonthlyRentOrMortgage = applicationInputModel.MonthlyRentOrMortgage,
                Status = ApplicationStatus.Submitted
            });

            await db.SaveChangesAsync();

            try
            {
                var response = await applicationApiService.SubmitApplication(applicationInputModel);

                var applicationStatus = GetStatus(response.Status);

                if (applicationStatus == ApplicationStatus.Approved)
                {
                    telemetryClient.TrackEvent("ApplicationSubmitApproved", new Dictionary<string, string>
                    {
                        { "SSN", CalulateHash(applicationInputModel.SSN)}
                    });
                }

                dbApplication.Entity.Status = applicationStatus;
                dbApplication.Entity.ExternalApplicationId = response.ApplicationId;

                await db.SaveChangesAsync();

                return dbApplication.Entity;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Application API submit failed.");

                telemetryClient.TrackEvent("ApplicationSubmitFailed", new Dictionary<string, string>
                {
                    { "SSN", CalulateHash(applicationInputModel.SSN)}
                });

                dbApplication.Entity.Status = ApplicationStatus.Failed;
                dbApplication.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                await db.SaveChangesAsync();

                return dbApplication.Entity;
            }
        }

        public async Task RefreshPendingApplicationStatuses()
        {
            var pendingApplications = await db.Applications
                .Where(x => x.Status == ApplicationStatus.Pending)
                .ToListAsync();

            foreach (var application in pendingApplications)
            {
                try
                {
                    var response = await applicationApiService.GetApplicationStatus(application.ExternalApplicationId);

                    var applicationStatus = GetStatus(response.Status);

                    if (applicationStatus == ApplicationStatus.Approved)
                    {
                        telemetryClient.TrackEvent("ApplicationSubmitApproved", new Dictionary<string, string>
                        {
                            { "SSN", CalulateHash(application.SSN)}
                        });
                    }

                    application.Status = applicationStatus;

                    await db.SaveChangesAsync();

                    await applicationNotificationService.NotifyApplicationStatusChange(application.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Application API submit failed.");

                    telemetryClient.TrackEvent("ApplicationSubmitFailed", new Dictionary<string, string>
                    {
                        { "SSN", CalulateHash(application.SSN)}
                    });

                    application.Status = ApplicationStatus.Failed;
                    await db.SaveChangesAsync();
                }
            }
        }


        private static ApplicationStatus GetStatus(string input) =>
            input switch
            {
                "Approved" => ApplicationStatus.Approved,
                "Denied" => ApplicationStatus.Rejected,
                "Pending" => ApplicationStatus.Pending,
                _ => throw new InvalidOperationException($"Invalid status: {input}")
            };

        private string CalulateHash(string input)
        {
            return Encoding.UTF8.GetString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(input)));
        }
    }
}
