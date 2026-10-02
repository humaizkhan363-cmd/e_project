using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Areas.Admin.Models;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers
{
    /// <summary>Admin maintains the cities of the territory and their 3-digit codes (used in account IDs).</summary>
    [Area("Admin")]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public class CitiesController : Controller
    {
        private readonly AppDbContext _db;

        public CitiesController(AppDbContext db) => _db = db;

        // All cities.
        public async Task<IActionResult> Index()
        {
            var cities = await _db.Cities.OrderBy(c => c.Name).ToListAsync();
            return View(cities);
        }

        // New city form.
        [HttpGet]
        public IActionResult Create() => View(new CityFormViewModel());

        // Saves a new city (name and code are unique).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CityFormViewModel model)
        {
            if (await _db.Cities.AnyAsync(c => c.Name == model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "A city with this name already exists.");
            }
            if (await _db.Cities.AnyAsync(c => c.Code == model.Code))
            {
                ModelState.AddModelError(nameof(model.Code), "A city with this code already exists.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var city = new City { Name = model.Name, Code = model.Code, IsActive = model.IsActive };
            _db.Cities.Add(city);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"City '{city.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        // Edit form for a city.
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var city = await _db.Cities.FindAsync(id);
            if (city is null)
            {
                return NotFound();
            }

            return View(new CityFormViewModel { Id = city.Id, Name = city.Name, Code = city.Code, IsActive = city.IsActive });
        }

        // Saves the city changes.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CityFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var city = await _db.Cities.FindAsync(id);
            if (city is null)
            {
                return NotFound();
            }

            if (await _db.Cities.AnyAsync(c => c.Name == model.Name && c.Id != id))
            {
                ModelState.AddModelError(nameof(model.Name), "A city with this name already exists.");
            }
            if (await _db.Cities.AnyAsync(c => c.Code == model.Code && c.Id != id))
            {
                ModelState.AddModelError(nameof(model.Code), "A city with this code already exists.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            city.Name = model.Name;
            city.Code = model.Code;
            city.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"City '{city.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
