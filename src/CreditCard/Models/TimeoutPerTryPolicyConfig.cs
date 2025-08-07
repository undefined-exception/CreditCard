namespace CreditCard.Models
{
    public class TimeoutPerTryPolicyConfig
    {
        public bool IsUsed { get; set; }
        public TimeSpan TimeoutPerTry { get; set; }
    }
}
