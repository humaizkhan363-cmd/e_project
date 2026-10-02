using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Areas.Admin.Models;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// Equipment purchases from product manufacturing vendors (the purchase list and payments made to vendors).
/// Recording a purchase increases the product's stock.
/// </summary>
[Area("Admin"), Authorize(Roles = nameof(EmployeeRole.Admin))]
public class PurchasesController(AppDbContext db) : Controller
{
    // Purchase list, newest first.
    public async Task<IActionResult> Index()
    {
        return View(await db.ProductPurchases.AsNoTracking().Include(p => p.Vendor).Include(p => p.Product)
            .OrderByDescending(p => p.PurchaseDate).ThenByDescending(p => p.Id).Take(500).ToListAsync());
    }

    // New purchase form.
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var m = new ProductPurchaseFormViewModel();
        await Populate(m);
        return View(m);
    }

    // Records the purchase and adds the quantity to stock in one transaction.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductPurchaseFormViewModel m)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == m.ProductId && p.VendorId == m.VendorId);
        if (product is null)
            ModelState.AddModelError(nameof(m.ProductId), "Choose a product supplied by the selected vendor.");
        if (m.AmountPaid > m.Quantity * m.UnitPrice)
            ModelState.AddModelError(nameof(m.AmountPaid), "Amount paid cannot exceed the purchase total.");
        if (!ModelState.IsValid) { await Populate(m); return View(m); }

        await using var tx = await db.Database.BeginTransactionAsync();
        product!.StockQuantity = checked(product.StockQuantity + m.Quantity);
        db.ProductPurchases.Add(new ProductPurchase
        {
            VendorId = m.VendorId,
            ProductId = m.ProductId,
            Quantity = m.Quantity,
            UnitPrice = m.UnitPrice,
            AmountPaid = m.AmountPaid,
            PurchaseDate = m.PurchaseDate,
            SupplierReference = m.SupplierReference?.Trim(),
            Notes = m.Notes?.Trim(),
            RecordedByEmployeeId = int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value)
        });
        try
        {
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["StatusMessage"] = "Purchase recorded and stock increased.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another user changed the product's stock at the same time (row-version check).
            await tx.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Stock changed in another session. Reload the product and try again.");
            await Populate(m);
            return View(m);
        }
    }

    // Active vendors and their products for the form's drop-down lists.
    private async Task Populate(ProductPurchaseFormViewModel m)
    {
        m.Vendors = await db.Vendors.Where(v => v.IsActive).OrderBy(v => v.Name)
            .Select(v => new SelectListItem(v.Name, v.Id.ToString())).ToListAsync();
        m.Products = await db.Products.Where(p => p.IsActive).OrderBy(p => p.Vendor.Name).ThenBy(p => p.Name)
            .Select(p => new SelectListItem(p.Vendor.Name + " — " + p.Name + " (stock " + p.StockQuantity + ")", p.Id.ToString())).ToListAsync();
    }
}
