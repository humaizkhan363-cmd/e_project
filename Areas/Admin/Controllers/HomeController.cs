using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
