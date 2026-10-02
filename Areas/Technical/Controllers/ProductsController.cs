using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Technical.Controllers;

// Technical staff keep the equipment details (description, reorder level, stock corrections) up to date.
// Prices, vendors and new products stay with the Admin.
[Area("Technical"), Authorize(Roles = nameof(EmployeeRole.Technical))]
public class ProductsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        IQueryable<Product> query = db.Products.AsNoTracking().Include(p => p.Vendor).Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p => p.Name.Contains(q) || p.Sku.Contains(q));
        }
        ViewBag.Search = q;
        return View(await query.OrderBy(p => p.Name).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        Product? p = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (p is null) return NotFound();
        return View(new EquipmentEditViewModel { Id = p.Id, Sku = p.Sku, Name = p.Name, StockQuantity = p.StockQuantity, ReorderLevel = p.ReorderLevel, Description = p.Description });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EquipmentEditViewModel m)
    {
        Product? p = await db.Products.SingleOrDefaultAsync(x => x.Id == m.Id && x.IsActive);
        if (p is null) return NotFound();
        m.Sku = p.Sku; m.Name = p.Name; m.StockQuantity = p.StockQuantity;
        if (p.StockQuantity + m.StockAdjustment < 0)
            ModelState.AddModelError(nameof(m.StockAdjustment), $"The adjustment would make stock negative (current stock is {p.StockQuantity}).");
        if (!ModelState.IsValid) return View(m);

        p.Description = string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim();
        p.ReorderLevel = m.ReorderLevel;
        p.StockQuantity += m.StockAdjustment;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "This equipment was changed by someone else. Reload it and try again.");
            return View(m);
        }
        TempData["StatusMessage"] = $"{p.Name} updated. Stock is now {p.StockQuantity}.";
        return RedirectToAction(nameof(Index));
    }
}

public class EquipmentEditViewModel
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    [Range(0, 100000), Display(Name = "Reorder level")] public int ReorderLevel { get; set; }
    [Range(-100000, 100000), Display(Name = "Stock adjustment (+ received / - damaged or lost)")] public int StockAdjustment { get; set; }
    [StringLength(500)] public string? Description { get; set; }
}
