using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Dashboard;

namespace NexusServiceMarketingSystem.Areas.Accounts.Controllers
{
    [Area("Accounts")]
    [Authorize(Roles = nameof(EmployeeRole.Accounts))]
    public class HomeController(IDashboardService dashboard) : Controller
    {
        // Accounts dashboard: unpaid and overdue bills, collections this month.
        public async Task<IActionResult> Index()
        {
            string name = User.FindFirst(RoleNames.DisplayNameClaim)?.Value ?? "";
            return View("Dashboard", await dashboard.AccountsAsync(name));
        }
    }
}
