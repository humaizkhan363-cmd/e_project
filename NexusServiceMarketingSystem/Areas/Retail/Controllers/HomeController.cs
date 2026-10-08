using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Dashboard;

namespace NexusServiceMarketingSystem.Areas.Retail.Controllers
{
    [Area("Retail")]
    [Authorize(Roles = nameof(EmployeeRole.RetailStaff))]
    public class HomeController(IDashboardService dashboard) : Controller
    {
        // Retail employee dashboard: figures for this employee's shop only.
        public async Task<IActionResult> Index()
        {
            string name = User.FindFirst(RoleNames.DisplayNameClaim)?.Value ?? "";
            int employeeId = int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
            return View("Dashboard", await dashboard.RetailAsync(employeeId, name));
        }
    }
}
