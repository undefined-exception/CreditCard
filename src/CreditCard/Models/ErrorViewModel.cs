namespace CreditCard.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

    public class DefaultUserConfig
    {
        public string Email { get; set; }

        public string Password { get; set; }
    }

    public class ApplicationInputModel
    {
        public string Name { get; set; }

        public string Ssn { get; set; }
    }

    public class CreditServiceConfig
    {
        public string BaseUrl { get; set; }
    }

    public class ApplicationV1Response
    {
        public ApplicationStatusV1 ApplicationStatus { get; set; }
    }

    public enum ApplicationStatusV1
    {
        Apprived,
        Rejected
    }
}
