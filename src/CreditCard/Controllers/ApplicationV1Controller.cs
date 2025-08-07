using Microsoft.AspNetCore.Mvc;

namespace CreditCard.Controllers
{
    public class ApplicationV1Controller : Controller
    {
        private readonly ILogger<ApplicationV1Controller> _logger;

        public ApplicationV1Controller(ILogger<ApplicationV1Controller> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

    }
}
