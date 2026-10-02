using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Controllers
{
    // Public plan catalogue: everyone (visitors, customers and staff) can see what each plan costs.
    public class CatalogController : Controller
    {
        private readonly AppDbContext _db;

        public CatalogController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Plans()
        {
            List<Plan> plans = await _db.Plans.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.ConnectionType).ThenBy(p => p.Kind).ThenBy(p => p.Price)
                .ToListAsync();
            return View(plans);
        }
    }
}
