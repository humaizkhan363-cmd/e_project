using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Accounts.Controllers
{
    [Area("Accounts")]
    [Authorize(Roles = nameof(EmployeeRole.Accounts))]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
