using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models.Account;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = RoleNames.Customer)]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
