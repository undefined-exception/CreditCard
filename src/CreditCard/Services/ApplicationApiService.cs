using CreditCard.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace CreditCard.Services
{
    public class ApplicationApiService : IApplicationApiService
    {
        private readonly CreditBureauApiConfig creditBureauApiConfig;

        private readonly ILogger<ApplicationService> logger;

        private readonly HttpClient httpClient;

        private const string SubmitEndpoint = "/api/CreditCardApplication/submit";
        private const string StatusEndpoint = "/api/CreditCardApplication/status/{0}";

        public ApplicationApiService(
            HttpClient httpClient,
            IOptions<CreditBureauApiConfig> options,
            ILogger<ApplicationService> logger)
        {
            this.httpClient = httpClient;
            this.creditBureauApiConfig = options.Value;
            this.logger = logger;
        }

        public async Task<ApplicationSubmissionResponse> SubmitApplication(ApplicationInputModel applicationInputModel)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonBody = JsonSerializer.Serialize(applicationInputModel, jsonOptions);

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
                RequestUri = new Uri(SubmitEndpoint, UriKind.Relative)
            };

            var response = await httpClient.SendAsync(httpRequestMessage);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Credit Bureau call failed {Status} {Message}", response.StatusCode, content);

                response.EnsureSuccessStatusCode();
            }

            var applicationResult = JsonSerializer.Deserialize<ApplicationSubmissionResponse>(content, jsonOptions);

            return applicationResult;
        }

        public async Task<ApplicationSubmissionResponse> GetApplicationStatus(string applicationId)
        { 
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(string.Format(StatusEndpoint, applicationId), UriKind.Relative)
            };

            var response = await httpClient.SendAsync(httpRequestMessage);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Credit Bureau call failed {Status} {Message}", response.StatusCode, content);

                response.EnsureSuccessStatusCode();
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var applicationResult = JsonSerializer.Deserialize<ApplicationSubmissionResponse>(content, jsonOptions);

            return applicationResult;
        }
    }
}
