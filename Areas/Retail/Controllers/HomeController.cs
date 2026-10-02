using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Retail.Controllers
{
    [Area("Retail")]
    [Authorize(Roles = nameof(EmployeeRole.RetailStaff))]
    public class HomeController : Controller
    {
        // Retail employee dashboard.
        public IActionResult Index() => View();
    }
}
