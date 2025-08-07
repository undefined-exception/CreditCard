namespace CreditCard.Models
{
    public class RetryPolicyConfig
    {
        public bool IsUsed { get; set; }
        public int? RetryCount { get; set; }
        public int? RetryInterval { get; set; }
        public int[] AdditionalStatusCodes { get; set; } = Array.Empty<int>();
    }
}
