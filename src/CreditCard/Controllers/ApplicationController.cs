using CreditCard.Models;
using CreditCard.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreditCard.Controllers
{
    public class ApplicationController : Controller
    {
        private readonly IApplicationService applicationService;

        public ApplicationController(IApplicationService applicationService)
        {
            this.applicationService = applicationService;
        }

        public async Task<IActionResult> List()
        {
            var applications = await applicationService.ListApplications();

            return View(applications);
        }

        public async Task<IActionResult> Index()
        {
            return View("Create");
        }

        public async Task<IActionResult> Submit([FromForm] ApplicationInputModel model)
        {
            if(ModelState.IsValid)
            {
                await applicationService.SubmitApplication(model);

                TempData["Message"] = "We've received your application";
                TempData["MessageType"] = "success";

                return RedirectToAction("List");
            }

            return View();
        }
    }
}
