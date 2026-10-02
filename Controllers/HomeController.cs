using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        // Public landing page. Bulk/corporate discount bands come from the database so the page never disagrees with billing.
        public async Task<IActionResult> Index()
        {
            List<DiscountScheme> schemes = await _db.DiscountSchemes.AsNoTracking()
                .Where(d => d.IsActive).OrderBy(d => d.MinConnections).ToListAsync();
            return View(schemes);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
