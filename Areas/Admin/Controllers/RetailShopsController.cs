using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Areas.Admin.Models;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers
{
    /// <summary>Admin maintains the retail shops (one or more per city) where customers enquire, order and pay.</summary>
    [Area("Admin")]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public class RetailShopsController : Controller
    {
        private readonly AppDbContext _db;
        public RetailShopsController(AppDbContext db) => _db = db;

        // All shops by city.
        public async Task<IActionResult> Index()
        {
            var shops = await _db.RetailShops.Include(s => s.City).OrderBy(s => s.City.Name).ThenBy(s => s.Name).ToListAsync();
            return View(shops);
        }

        // New shop form.
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new RetailShopFormViewModel();
            await PopulateCities(model);
            return View(model);
        }

        // Saves a new shop.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RetailShopFormViewModel model)
        {
            await ValidateShop(model);
            if (!ModelState.IsValid) { await PopulateCities(model); return View(model); }
            var shop = new RetailShop { Name = model.Name.Trim(), AddressLine = model.AddressLine.Trim(), Phone = model.Phone?.Trim(), CityId = model.CityId, IsActive = model.IsActive };
            _db.RetailShops.Add(shop);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = $"Retail shop '{shop.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        // A shop with its employees.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var shop = await _db.RetailShops.Include(s => s.City).Include(s => s.Employees).SingleOrDefaultAsync(s => s.Id == id);
            return shop is null ? NotFound() : View(shop);
        }

        // Edit form for a shop.
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var shop = await _db.RetailShops.FindAsync(id);
            if (shop is null) return NotFound();
            var model = new RetailShopFormViewModel { Id = shop.Id, Name = shop.Name, AddressLine = shop.AddressLine, Phone = shop.Phone, CityId = shop.CityId, IsActive = shop.IsActive };
            await PopulateCities(model, shop.CityId);
            return View(model);
        }

        // Saves the shop changes.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RetailShopFormViewModel model)
        {
            if (id != model.Id) return BadRequest();
            var shop = await _db.RetailShops.FindAsync(id);
            if (shop is null) return NotFound();
            await ValidateShop(model, shop.CityId);
            if (!ModelState.IsValid) { await PopulateCities(model, shop.CityId); return View(model); }
            shop.Name = model.Name.Trim(); shop.AddressLine = model.AddressLine.Trim(); shop.Phone = model.Phone?.Trim(); shop.CityId = model.CityId; shop.IsActive = model.IsActive;
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = $"Retail shop '{shop.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // Opens or closes a shop.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var shop = await _db.RetailShops.FindAsync(id);
            if (shop is null) return NotFound();
            shop.IsActive = !shop.IsActive;
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = $"Retail shop '{shop.Name}' {(shop.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        // The city must be active and the shop name unique within the city.
        private async Task ValidateShop(RetailShopFormViewModel model, int? currentCityId = null)
        {
            if (!await _db.Cities.AnyAsync(c => c.Id == model.CityId && (c.IsActive || c.Id == currentCityId)))
                ModelState.AddModelError(nameof(model.CityId), "Select an active city.");
            string name = model.Name.Trim();
            if (await _db.RetailShops.AnyAsync(s => s.CityId == model.CityId && s.Name == name && s.Id != model.Id))
                ModelState.AddModelError(nameof(model.Name), "A shop with this name already exists in the selected city.");
        }

        // Active cities (plus the current one) for the drop-down list.
        private async Task PopulateCities(RetailShopFormViewModel model, int? includeId = null)
        {
            var cities = await _db.Cities.Where(c => c.IsActive || c.Id == includeId).OrderBy(c => c.Name).ToListAsync();
            model.Cities = cities.Select(c => new SelectListItem($"{c.Name} ({c.Code})", c.Id.ToString())).ToList();
        }
    }
}
