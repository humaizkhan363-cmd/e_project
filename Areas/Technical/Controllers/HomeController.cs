using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Technical.Controllers
{
    [Area("Technical")]
    [Authorize(Roles = nameof(EmployeeRole.Technical))]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
