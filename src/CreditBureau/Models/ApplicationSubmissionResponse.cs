namespace CreditBureau.Models
{
    public class ApplicationSubmissionResponse
    {
        public string ApplicationId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
