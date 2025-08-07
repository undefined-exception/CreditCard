namespace CreditCard.Models
{
    public class HttpPolicyConfig
    {
        public RetryPolicyConfig RetryPolicyConfig { get; set; }
        public TimeoutPerTryPolicyConfig TimeoutPerTryPolicyConfig { get; set; }
        public BulkheadPolicyConfig BulkheadPolicyConfig { get; set; }
        public ThrottlingPolicyConfig ThrottlingPolicyConfig { get; set; }
    }
}
