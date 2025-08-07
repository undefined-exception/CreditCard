namespace CreditCard.Models
{
    public class ThrottlingPolicyConfig
    {
        public bool IsUsed { get; set; }
        public int RequestsLimit { get; set; }
        public TimeSpan Window { get; set; }
        public int QueueLimit { get; set; }
    }
}
