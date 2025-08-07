namespace CreditCard.Models
{
    public interface IHttpClientConfig
    {
        Uri BaseEndpoint { get; }
        TimeSpan RequestTimeout { get; }
        bool DisableCookies => false;
    }
}
