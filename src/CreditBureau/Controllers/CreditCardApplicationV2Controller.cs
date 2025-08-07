namespace CreditBureau.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    namespace CreditCardApplicationAPI.Controllers
    {
        [ApiController]
        [Route("api/v2/[controller]")]
        public class CreditCardApplicationV2Controller : ControllerBase
        {
            private static readonly Random _random = new Random();
            private static readonly ConcurrentDictionary<string, ApplicationResult> _mockApplications = new ConcurrentDictionary<string, ApplicationResult>();

            [HttpPost("submit")]
            public ActionResult<ApplicationSubmissionResponse> SubmitApplication([FromBody] ApplicationRequest request)
            {
                // Validate required fields
                if (string.IsNullOrEmpty(request.SSN) || string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName))
                {
                    return BadRequest("SSN, FirstName, and LastName are required");
                }

                // Create application ID
                var applicationId = Guid.NewGuid().ToString();

                // Store initial application with "Processing" status
                var initialResult = new ApplicationResult
                {
                    ApplicationId = applicationId,
                    Status = "Processing",
                    CreditLimit = 0,
                    InterestRate = 0,
                    CardType = null,
                    ProcessingTimeMs = 0,
                    Message = "Your application is being processed"
                };

                _mockApplications.TryAdd(applicationId, initialResult);

                // Simulate background processing
                _ = ProcessApplicationInBackground(applicationId, request);

                return Ok(new ApplicationSubmissionResponse
                {
                    ApplicationId = applicationId,
                    Status = "Processing",
                    Message = "Application received and is being processed",
                    Timestamp = DateTime.UtcNow
                });
            }

            [HttpGet("status/{applicationId}")]
            public ActionResult<ApplicationResult> CheckApplicationStatus(string applicationId)
            {
                if (_mockApplications.TryGetValue(applicationId, out var result))
                {
                    return Ok(result);
                }

                return NotFound("Application not found");
            }

            private async Task ProcessApplicationInBackground(string applicationId, ApplicationRequest request)
            {
                // Simulate processing delay (3-10 seconds)
                int delaySeconds = _random.Next(3000, 10000);
                await Task.Delay(delaySeconds);

                // Generate final application result
                var finalResult = new ApplicationResult
                {
                    ApplicationId = applicationId,
                    Status = GetRandomStatus(),
                    CreditLimit = GetRandomCreditLimit(),
                    InterestRate = GetRandomInterestRate(),
                    CardType = GetRandomCardType(),
                    ProcessingTimeMs = delaySeconds,
                    Message = GetRandomMessage()
                };

                // Update the stored result
                _mockApplications.TryUpdate(applicationId, finalResult, _mockApplications[applicationId]);
            }

            private string GetRandomStatus()
            {
                string[] statuses = { "Approved", "Denied", "Pending", "Approved with conditions" };
                return statuses[_random.Next(statuses.Length)];
            }

            private decimal GetRandomCreditLimit()
            {
                decimal[] limits = { 500, 1000, 1500, 2000, 2500, 3000, 5000, 7500, 10000, 0 };
                return limits[_random.Next(limits.Length)];
            }

            private decimal GetRandomInterestRate()
            {
                return _random.Next(8, 25) + (decimal)_random.NextDouble();
            }

            private string GetRandomCardType()
            {
                string[] types = { "Standard", "Gold", "Platinum", "Secured", "Student" };
                return types[_random.Next(types.Length)];
            }

            private string GetRandomMessage()
            {
                string[] approvedMessages =
                {
                "Congratulations! Your application has been approved.",
                "Welcome to our premium card member program!",
                "You've been approved for a special offer!"
            };

                string[] deniedMessages =
                {
                "We regret to inform you that your application has been denied.",
                "Based on our review, we cannot approve your application at this time.",
                "Thank you for your application. Unfortunately, we cannot approve it."
            };

                string[] pendingMessages =
                {
                "Your application requires additional review.",
                "We need more time to process your application.",
                "Additional verification is required for your application."
            };

                return _random.Next(3) switch
                {
                    0 => approvedMessages[_random.Next(approvedMessages.Length)],
                    1 => deniedMessages[_random.Next(deniedMessages.Length)],
                    _ => pendingMessages[_random.Next(pendingMessages.Length)]
                };
            }
        }

        public class ApplicationSubmissionResponse
        {
            public string ApplicationId { get; set; }
            public string Status { get; set; }
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
