using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Controllers
{
    /// <summary>Public pages: landing page, privacy and error page.</summary>
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
            // Coverage section: active cities with their code and number of open retail shops.
            ViewBag.Coverage = await _db.Cities.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name)
                .Select(c => new CityCoverage(c.Name, c.Code, _db.RetailShops.Count(s => s.CityId == c.Id && s.IsActive)))
                .ToListAsync();
            ViewBag.PlanCount = await _db.Plans.CountAsync(p => p.IsActive);
            return View(schemes);
        }

        // Privacy notice.
        public IActionResult Privacy()
        {
            return View();
        }

        // Error page (production only), with the request id for support.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    /// <summary>One city on the home page coverage list.</summary>
    public sealed record CityCoverage(string Name, string Code, int Shops);
}
