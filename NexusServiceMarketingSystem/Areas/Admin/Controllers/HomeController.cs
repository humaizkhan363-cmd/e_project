using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public class HomeController : Controller
    {
        // Admin dashboard: links to master data, stock and operations.
        public IActionResult Index() => View();
    }
}
