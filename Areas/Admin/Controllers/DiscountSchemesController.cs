using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Areas.Admin.Models;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public class DiscountSchemesController : Controller
    {
        private readonly AppDbContext _db;

        public DiscountSchemesController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var schemes = await _db.DiscountSchemes.OrderBy(s => s.MinConnections).ToListAsync();
            return View(schemes);
        }

        [HttpGet]
        public IActionResult Create() => View(new DiscountSchemeFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DiscountSchemeFormViewModel model)
        {
            ValidateRange(model);
            if (await _db.DiscountSchemes.AnyAsync(s => s.Name == model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "A scheme with this name already exists.");
            }
            if (await _db.DiscountSchemes.AnyAsync(s => s.MinConnections == model.MinConnections))
            {
                ModelState.AddModelError(nameof(model.MinConnections), "Another scheme already starts at this many connections.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var scheme = new DiscountScheme
            {
                Name = model.Name,
                MinConnections = model.MinConnections,
                MaxConnections = model.MaxConnections,
                DiscountPercent = model.DiscountPercent,
                AppliesToAdvance = model.AppliesToAdvance,
                AppliesToSecurityDeposit = model.AppliesToSecurityDeposit,
                IsActive = model.IsActive
            };
            _db.DiscountSchemes.Add(scheme);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"Discount scheme '{scheme.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var scheme = await _db.DiscountSchemes.FindAsync(id);
            if (scheme is null)
            {
                return NotFound();
            }

            return View(new DiscountSchemeFormViewModel
            {
                Id = scheme.Id,
                Name = scheme.Name,
                MinConnections = scheme.MinConnections,
                MaxConnections = scheme.MaxConnections,
                DiscountPercent = scheme.DiscountPercent,
                AppliesToAdvance = scheme.AppliesToAdvance,
                AppliesToSecurityDeposit = scheme.AppliesToSecurityDeposit,
                IsActive = scheme.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DiscountSchemeFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var scheme = await _db.DiscountSchemes.FindAsync(id);
            if (scheme is null)
            {
                return NotFound();
            }

            ValidateRange(model);
            if (await _db.DiscountSchemes.AnyAsync(s => s.Name == model.Name && s.Id != id))
            {
                ModelState.AddModelError(nameof(model.Name), "A scheme with this name already exists.");
            }
            if (await _db.DiscountSchemes.AnyAsync(s => s.MinConnections == model.MinConnections && s.Id != id))
            {
                ModelState.AddModelError(nameof(model.MinConnections), "Another scheme already starts at this many connections.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            scheme.Name = model.Name;
            scheme.MinConnections = model.MinConnections;
            scheme.MaxConnections = model.MaxConnections;
            scheme.DiscountPercent = model.DiscountPercent;
            scheme.AppliesToAdvance = model.AppliesToAdvance;
            scheme.AppliesToSecurityDeposit = model.AppliesToSecurityDeposit;
            scheme.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"Discount scheme '{scheme.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }

        private void ValidateRange(DiscountSchemeFormViewModel model)
        {
            if (model.MaxConnections is not null && model.MaxConnections < model.MinConnections)
            {
                ModelState.AddModelError(nameof(model.MaxConnections), "Maximum connections cannot be less than the minimum.");
            }
        }
    }
}
