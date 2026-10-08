using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Dashboard;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public class HomeController(IDashboardService dashboard) : Controller
    {
        // Admin dashboard: company-wide counters, open orders and quick actions.
        public async Task<IActionResult> Index()
        {
            string name = User.FindFirst(RoleNames.DisplayNameClaim)?.Value ?? "";
            return View("Dashboard", await dashboard.AdminAsync(name));
        }
    }
}
