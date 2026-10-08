using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Dashboard;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = RoleNames.Customer)]
    public class HomeController(IDashboardService dashboard) : Controller
    {
        // Customer dashboard: amount due, connections and own orders.
        public async Task<IActionResult> Index()
        {
            string name = User.FindFirst(RoleNames.DisplayNameClaim)?.Value ?? "";
            int customerId = int.Parse(User.FindFirst(RoleNames.CustomerIdClaim)!.Value);
            return View("Dashboard", await dashboard.CustomerAsync(customerId, name));
        }
    }
}
