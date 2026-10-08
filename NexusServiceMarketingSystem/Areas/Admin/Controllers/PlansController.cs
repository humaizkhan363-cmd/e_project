using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Areas.Admin.Models;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers
{
    /// <summary>Admin maintains the plans and their charges (insert, update, delete and search).</summary>
    [Area("Admin")]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public class PlansController : Controller
    {
        private readonly AppDbContext _db;

        public PlansController(AppDbContext db) => _db = db;

        // All plans, optionally searched by name or description.
        public async Task<IActionResult> Index(string? q)
        {
            IQueryable<Plan> query = _db.Plans;
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p => p.Name.Contains(q) || (p.Description != null && p.Description.Contains(q)));
            }
            var plans = await query.OrderBy(p => p.ConnectionType).ThenBy(p => p.Name).ToListAsync();
            ViewBag.Search = q;
            return View(plans);
        }

        // Plans already used by an order or connection are history and cannot be deleted; deactivate them instead.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var plan = await _db.Plans.FindAsync(id);
            if (plan is null)
            {
                return NotFound();
            }
            bool inUse = await _db.Orders.AnyAsync(o => o.PlanId == id) || await _db.Connections.AnyAsync(c => c.PlanId == id);
            if (inUse)
            {
                TempData["ErrorMessage"] = $"Plan '{plan.Name}' is used by existing orders or connections and cannot be deleted. Edit it and clear 'Active' instead.";
                return RedirectToAction(nameof(Index));
            }
            _db.Plans.Remove(plan);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = $"Plan '{plan.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // New plan form.
        [HttpGet]
        public IActionResult Create() => View(new PlanFormViewModel());

        // Saves a new plan.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlanFormViewModel model)
        {
            if (await _db.Plans.AnyAsync(p => p.Name == model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "A plan with this name already exists.");
            }
            foreach (string error in model.ValidateBusinessRules())
            {
                ModelState.AddModelError(string.Empty, error);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var plan = ToEntity(new Plan(), model);
            _db.Plans.Add(plan);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"Plan '{plan.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        // Edit form for a plan.
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var plan = await _db.Plans.FindAsync(id);
            if (plan is null)
            {
                return NotFound();
            }

            return View(ToViewModel(plan));
        }

        // Saves the plan changes.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PlanFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var plan = await _db.Plans.FindAsync(id);
            if (plan is null)
            {
                return NotFound();
            }

            if (await _db.Plans.AnyAsync(p => p.Name == model.Name && p.Id != id))
            {
                ModelState.AddModelError(nameof(model.Name), "A plan with this name already exists.");
            }
            foreach (string error in model.ValidateBusinessRules())
            {
                ModelState.AddModelError(string.Empty, error);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ToEntity(plan, model);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"Plan '{plan.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // ---------------------------------------------------------------- mapping helpers
        private static PlanFormViewModel ToViewModel(Plan plan) => new()
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            ConnectionType = plan.ConnectionType,
            Kind = plan.Kind,
            IncludedHours = plan.IncludedHours,
            SpeedKbps = plan.SpeedKbps,
            ValidityMonths = plan.ValidityMonths,
            Price = plan.Price,
            SecurityDeposit = plan.SecurityDeposit,
            LocalCallRatePerMinute = plan.LocalCallRatePerMinute,
            StdCallRatePerMinute = plan.StdCallRatePerMinute,
            MobileMessagingRatePerMinute = plan.MobileMessagingRatePerMinute,
            IsActive = plan.IsActive
        };

        private static Plan ToEntity(Plan plan, PlanFormViewModel model)
        {
            plan.Name = model.Name;
            plan.Description = model.Description;
            plan.ConnectionType = model.ConnectionType;
            plan.Kind = model.Kind;
            // Only the fields that apply to this Kind are kept; the others are cleared so a
            // plan edited from Hourly to Unlimited (for example) doesn't keep stale values.
            plan.IncludedHours = model.Kind == PlanKind.Hourly ? model.IncludedHours : null;
            plan.SpeedKbps = model.Kind == PlanKind.Unlimited ? model.SpeedKbps : null;
            bool isLandline = model.Kind is PlanKind.LocalRental or PlanKind.StdRental;
            plan.LocalCallRatePerMinute = isLandline ? model.LocalCallRatePerMinute : null;
            plan.StdCallRatePerMinute = model.Kind == PlanKind.StdRental ? model.StdCallRatePerMinute : null;
            plan.MobileMessagingRatePerMinute = model.Kind == PlanKind.StdRental ? model.MobileMessagingRatePerMinute : null;
            plan.ValidityMonths = model.ValidityMonths;
            plan.Price = model.Price;
            plan.SecurityDeposit = model.SecurityDeposit;
            plan.IsActive = model.IsActive;
            return plan;
        }
    }
}
