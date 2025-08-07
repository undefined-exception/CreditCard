namespace CreditBureau.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    namespace CreditCardApplicationAPI.Controllers
    {
        [ApiController]
        [Route("[controller]")]
        public class CreditCardApplicationController : ControllerBase
        {
            private static readonly Random _random = new Random();

            [HttpPost]
            public async Task<ActionResult<ApplicationResult>> ProcessApplication([FromBody] ApplicationRequest request)
            {
                // Simulate processing delay (1-5 seconds)
                int delaySeconds = _random.Next(1000, 5000);
                await Task.Delay(delaySeconds);

                // Generate a random application result
                var result = new ApplicationResult
                {
                    ApplicationId = Guid.NewGuid().ToString(),
                    Status = GetRandomStatus(),
                    CreditLimit = GetRandomCreditLimit(),
                    InterestRate = GetRandomInterestRate(),
                    CardType = GetRandomCardType(),
                    ProcessingTimeMs = delaySeconds,
                    Message = GetRandomMessage()
                };

                return Ok(result);
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

        public class ApplicationRequest
        {
            public string SSN { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DateTime DateOfBirth { get; set; }
            public decimal AnnualIncome { get; set; }
            public string EmploymentStatus { get; set; }
            public string HousingStatus { get; set; }
            public decimal MonthlyRentOrMortgage { get; set; }
        }

        public class ApplicationResult
        {
            public string ApplicationId { get; set; }
            public string Status { get; set; }
            public decimal CreditLimit { get; set; }
            public decimal InterestRate { get; set; }
            public string CardType { get; set; }
            public int ProcessingTimeMs { get; set; }
            public string Message { get; set; }
        }
    }
}
