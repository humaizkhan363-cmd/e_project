using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Areas.Admin.Models;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers
{
    /// <summary>Admin maintains the equipment (modems, routers...): vendor, prices, replacement charge and stock.</summary>
    [Area("Admin")]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _db;

        public ProductsController(AppDbContext db) => _db = db;

        // All products with vendor and stock.
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products.Include(p => p.Vendor).OrderBy(p => p.Name).ToListAsync();
            return View(products);
        }

        // New product form.
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ProductFormViewModel { VendorOptions = await GetVendorOptionsAsync() };
            return View(model);
        }

        // Saves a new product (SKU must be unique).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            if (await _db.Products.AnyAsync(p => p.Sku == model.Sku))
            {
                ModelState.AddModelError(nameof(model.Sku), "A product with this SKU already exists.");
            }
            if (!await _db.Vendors.AnyAsync(v => v.Id == model.VendorId))
            {
                ModelState.AddModelError(nameof(model.VendorId), "Please select a valid vendor.");
            }
            if (!ModelState.IsValid)
            {
                model.VendorOptions = await GetVendorOptionsAsync();
                return View(model);
            }

            var product = new Product
            {
                Sku = model.Sku,
                Name = model.Name,
                Category = model.Category,
                Description = model.Description,
                VendorId = model.VendorId,
                PurchasePrice = model.PurchasePrice,
                ReplacementCharge = model.ReplacementCharge,
                StockQuantity = model.StockQuantity,
                ReorderLevel = model.ReorderLevel,
                IsActive = model.IsActive
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"Product '{product.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        // Edit form for a product.
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            var model = new ProductFormViewModel
            {
                Id = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Category = product.Category,
                Description = product.Description,
                VendorId = product.VendorId,
                PurchasePrice = product.PurchasePrice,
                ReplacementCharge = product.ReplacementCharge,
                StockQuantity = product.StockQuantity,
                ReorderLevel = product.ReorderLevel,
                IsActive = product.IsActive,
                VendorOptions = await GetVendorOptionsAsync()
            };
            return View(model);
        }

        // Saves the product changes.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var product = await _db.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            if (await _db.Products.AnyAsync(p => p.Sku == model.Sku && p.Id != id))
            {
                ModelState.AddModelError(nameof(model.Sku), "A product with this SKU already exists.");
            }
            if (!await _db.Vendors.AnyAsync(v => v.Id == model.VendorId))
            {
                ModelState.AddModelError(nameof(model.VendorId), "Please select a valid vendor.");
            }
            if (!ModelState.IsValid)
            {
                model.VendorOptions = await GetVendorOptionsAsync();
                return View(model);
            }

            product.Sku = model.Sku;
            product.Name = model.Name;
            product.Category = model.Category;
            product.Description = model.Description;
            product.VendorId = model.VendorId;
            product.PurchasePrice = model.PurchasePrice;
            product.ReplacementCharge = model.ReplacementCharge;
            product.StockQuantity = model.StockQuantity;
            product.ReorderLevel = model.ReorderLevel;
            product.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"Product '{product.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // Vendors for the drop-down list.
        private async Task<List<ProductFormViewModel.VendorOption>> GetVendorOptionsAsync()
        {
            return await _db.Vendors
                .Where(v => v.IsActive)
                .OrderBy(v => v.Name)
                .Select(v => new ProductFormViewModel.VendorOption { Id = v.Id, Name = v.Name })
                .ToListAsync();
        }
    }
}
