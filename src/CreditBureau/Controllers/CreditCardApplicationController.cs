namespace CreditBureau.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using CreditBureau.Models;

    [ApiController]
    [Route("api/[controller]")]
    public class CreditCardApplicationController : ControllerBase
    {
        private static readonly Random _random = new Random();
        private static readonly ConcurrentDictionary<string, ApplicationResult> _mockApplications = new ConcurrentDictionary<string, ApplicationResult>();

        [HttpPost("submit")]
        public ActionResult<ApplicationSubmissionResponse> SubmitApplication([FromBody] ApplicationRequest request)
        {
            if (string.IsNullOrEmpty(request.SSN) || string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName))
            {
                return BadRequest("SSN, FirstName, and LastName are required");
            }

            // Create application ID
            var applicationId = Guid.NewGuid().ToString();

            // Store initial application with "Pending" status
            var initialResult = new ApplicationResult
            {
                ApplicationId = applicationId,
                Status = "Pending",
                Message = "Your application is being processed"
            };

            _mockApplications.TryAdd(applicationId, initialResult);

            // Simulate background Pending
            _ = ProcessApplicationInBackground(applicationId, request);

            return Ok(new ApplicationSubmissionResponse
            {
                ApplicationId = applicationId,
                Status = "Pending",
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

            return Ok(new ApplicationResult
            {
                ApplicationId = Guid.NewGuid().ToString(),
                Message = "Mock status",
                Status = "Approved"
            });
        }

        private async Task ProcessApplicationInBackground(string applicationId, ApplicationRequest request)
        {
            // Simulate Pending delay (3-10 seconds)
            int delaySeconds = _random.Next(3000, 10000);
            await Task.Delay(delaySeconds);

            // Generate final application result
            var finalResult = new ApplicationResult
            {
                ApplicationId = applicationId,
                Status = GetRandomStatus(),
                Message = GetRandomMessage()
            };

            // Update the stored result
            _mockApplications.TryUpdate(applicationId, finalResult, _mockApplications[applicationId]);
        }

        private string GetRandomStatus()
        {
            string[] statuses = { "Approved", "Denied", "Pending" };
            return statuses[_random.Next(statuses.Length)];
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
}

