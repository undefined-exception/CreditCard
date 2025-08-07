namespace CreditCard.Models
{
    public class CreditBureauApiConfig : IHttpClientConfig, IHttpPolicyConfig
    {
        public Uri BaseEndpoint { get; init; } = new("https://localhost:7189/");
        public TimeSpan RequestTimeout { get; init; }
        public HttpPolicyConfig? HttpPolicyConfig { get; set; }
    }
}
