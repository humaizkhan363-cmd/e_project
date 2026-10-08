using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Dashboard;

namespace NexusServiceMarketingSystem.Areas.Technical.Controllers
{
    [Area("Technical")]
    [Authorize(Roles = nameof(EmployeeRole.Technical))]
    public class HomeController(IDashboardService dashboard) : Controller
    {
        // Technical dashboard: feasibility queue, connections ready to create, overdue and stock.
        public async Task<IActionResult> Index()
        {
            string name = User.FindFirst(RoleNames.DisplayNameClaim)?.Value ?? "";
            return View("Dashboard", await dashboard.TechnicalAsync(name));
        }
    }
}
