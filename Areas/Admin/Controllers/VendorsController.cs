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
    public class VendorsController : Controller
    {
        private readonly AppDbContext _db;

        public VendorsController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var vendors = await _db.Vendors.OrderBy(v => v.Name).ToListAsync();
            return View(vendors);
        }

        [HttpGet]
        public IActionResult Create() => View(new VendorFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorFormViewModel model)
        {
            if (await _db.Vendors.AnyAsync(v => v.Name == model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "A vendor with this name already exists.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var vendor = new Vendor
            {
                Name = model.Name,
                ContactPerson = model.ContactPerson,
                Email = model.Email,
                Phone = model.Phone,
                AddressLine = model.AddressLine,
                IsActive = model.IsActive
            };
            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"Vendor '{vendor.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vendor = await _db.Vendors.FindAsync(id);
            if (vendor is null)
            {
                return NotFound();
            }

            return View(new VendorFormViewModel
            {
                Id = vendor.Id,
                Name = vendor.Name,
                ContactPerson = vendor.ContactPerson,
                Email = vendor.Email,
                Phone = vendor.Phone,
                AddressLine = vendor.AddressLine,
                IsActive = vendor.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VendorFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var vendor = await _db.Vendors.FindAsync(id);
            if (vendor is null)
            {
                return NotFound();
            }

            if (await _db.Vendors.AnyAsync(v => v.Name == model.Name && v.Id != id))
            {
                ModelState.AddModelError(nameof(model.Name), "A vendor with this name already exists.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            vendor.Name = model.Name;
            vendor.ContactPerson = model.ContactPerson;
            vendor.Email = model.Email;
            vendor.Phone = model.Phone;
            vendor.AddressLine = model.AddressLine;
            vendor.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"Vendor '{vendor.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
