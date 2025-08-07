namespace CreditCard.Models
{
    public class BulkheadPolicyConfig
    {
        public bool IsUsed { get; set; }
        public int? MaxParallelization { get; set; }
    }
}
