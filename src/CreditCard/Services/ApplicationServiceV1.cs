using CreditCard.Data;
using CreditCard.Models;
using Microsoft.Extensions.Options;

namespace CreditCard.Services
{
    public class ApplicationServiceV1
    {
        private readonly ApplicationDbContext db;


        private readonly CreditServiceConfig creditServiceConfig;

        public ApplicationServiceV1(ApplicationDbContext db, IOptions<CreditServiceConfig> options)
        {
            this.db = db;
            this.creditServiceConfig = options.Value;
        }

        public async Task ProcessApplication()
        {
            var httpClient = new HttpClient();



            httpClient.


        }
    }
}
