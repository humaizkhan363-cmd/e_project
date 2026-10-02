using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Models.Workflows;
using NexusServiceMarketingSystem.Services.Workflows;

namespace NexusServiceMarketingSystem.Areas.Technical.Controllers;

/// <summary>
/// Technical staff create connections from feasible orders, change their status (active / temporarily inactive /
/// permanently inactive), change plans, replace spoiled equipment and suspend connections with overdue bills.
/// </summary>
[Area("Technical"), Authorize(Roles = nameof(EmployeeRole.Technical))]
public class ConnectionsController(AppDbContext db, IConnectionProvisioningService provisioning) : Controller
{
    // All connections (newest first) with the plans and in-stock equipment needed by the inline forms.
    public async Task<IActionResult> Index()
    {
        ViewBag.Plans = await db.Plans.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        ViewBag.Products = await db.Products.AsNoTracking().Where(p => p.IsActive && p.StockQuantity > 0).OrderBy(p => p.Name).ToListAsync();
        ViewBag.OverdueCount = (await provisioning.GetOverdueAsync()).Count;
        return View(await db.Connections.AsNoTracking().Include(c => c.Customer).Include(c => c.Plan).Include(c => c.Order)
            .OrderByDescending(c => c.ActivatedAtUtc).Take(500).ToListAsync());
    }

    // Customer asks to move to another plan of the same connection type (security deposit is per type, so it stays the same).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePlan(int id, int planId)
    {
        var c = await db.Connections.SingleOrDefaultAsync(x => x.Id == id);
        var p = await db.Plans.SingleOrDefaultAsync(x => x.Id == planId && x.IsActive);
        if (c is null || p is null || c.Status == ConnectionStatus.PermanentlyInactive || p.ConnectionType != c.ConnectionType)
        {
            TempData["ErrorMessage"] = "Plan could not be changed. Choose an active plan of the same connection type for a connection that is not permanently inactive.";
            return RedirectToAction(nameof(Index));
        }
        c.PlanId = p.Id;
        c.StatusChangedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = "Plan changed. The next bill charges the new plan's fee.";
        return RedirectToAction(nameof(Index));
    }

    // Equipment spoiled by the customer is replaced from stock: the old unit is marked returned and the new unit is
    // recorded as a replacement with the product's replacement charge, which Accounts' next bill picks up automatically.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplaceEquipment(int id, int productId, string? serialNumber)
    {
        var c = await db.Connections.Include(x => x.ConnectionProducts).SingleOrDefaultAsync(x => x.Id == id);
        var p = await db.Products.SingleOrDefaultAsync(x => x.Id == productId && x.IsActive);
        if (c is null || p is null || c.ConnectionType == ConnectionType.Telephone || c.Status == ConnectionStatus.PermanentlyInactive)
        {
            TempData["ErrorMessage"] = "Equipment could not be replaced. Choose active equipment for an internet connection that is not permanently inactive.";
            return RedirectToAction(nameof(Index));
        }
        if (p.StockQuantity < 1)
        {
            TempData["ErrorMessage"] = p.Name + " is out of stock.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var old in c.ConnectionProducts.Where(x => x.ReturnedAtUtc == null))
            old.ReturnedAtUtc = DateTime.UtcNow;
        db.ConnectionProducts.Add(new ConnectionProduct
        {
            ConnectionId = c.Id,
            ProductId = p.Id,
            Quantity = 1,
            SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim(),
            IsReplacement = true,
            ReplacementChargeAmount = p.ReplacementCharge,
            Notes = "Replacement issued by Technical staff."
        });
        p.StockQuantity -= 1;

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "The replacement could not be saved. The serial number may already be in use, or the stock changed. Please try again.";
            return RedirectToAction(nameof(Index));
        }
        TempData["StatusMessage"] = "Replacement issued for " + c.AccountId + ". Replacement charge " + p.ReplacementCharge.ToString("C") + " will be added to the customer's next bill.";
        return RedirectToAction(nameof(Index));
    }

    // Form to create the connection(s) of a feasible order.
    [HttpGet]
    public async Task<IActionResult> Provision(int id)
    {
        var o = await db.Orders.Include(x => x.Customer).Include(x => x.LandlinePlan)
            .SingleOrDefaultAsync(x => x.Id == id && x.Status == OrderStatus.Feasible);
        if (o is null) return NotFound();
        var m = new ConnectionProvisionViewModel { OrderId = o.Id };
        Describe(m, o);
        await Populate(m, o.CustomerId);
        return View(m);
    }

    // Creates the connection(s): account IDs are generated, equipment is issued from stock and, for a dial-up order
    // that includes a new telephone line, the line is created and linked to the dial-up connection.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Provision(ConnectionProvisionViewModel m)
    {
        var o = await db.Orders.Include(x => x.Customer).Include(x => x.LandlinePlan).SingleOrDefaultAsync(x => x.Id == m.OrderId);
        if (o is null) return NotFound();
        Describe(m, o);
        if (!ModelState.IsValid) { await Populate(m, o.CustomerId); return View(m); }
        try
        {
            var result = await provisioning.ProvisionAsync(new ProvisionConnectionRequest(m.OrderId, EmployeeId(), m.ProductId,
                m.SerialNumber, m.LandlineConnectionId, m.PhoneNumber));
            TempData["StatusMessage"] = $"Provisioned {result.Count} connection(s): " + string.Join(", ", result.Select(c => c.AccountId)) + ".";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await Populate(m, o.CustomerId);
            return View(m);
        }
    }

    // Sets a connection active, temporarily inactive or permanently inactive.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(ConnectionStatusFormViewModel m)
    {
        try
        {
            await provisioning.ChangeStatusAsync(m.Id, EmployeeId(), m.Status);
            TempData["StatusMessage"] = "Connection status updated.";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    // Postpaid rule: active connections with overdue bills, and suspended connections whose dues are now paid.
    public async Task<IActionResult> Overdue()
    {
        ViewBag.Cleared = await provisioning.GetClearedSuspendedAsync();
        return View(await provisioning.GetOverdueAsync());
    }

    // Makes one overdue connection (id) or all overdue connections (no id) temporarily inactive.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(int? id)
    {
        try
        {
            int count = await provisioning.SuspendOverdueAsync(EmployeeId(), id);
            TempData["StatusMessage"] = count == 0 ? "No connection needed to be suspended." : $"{count} connection(s) set temporarily inactive for overdue bills.";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Overdue));
    }

    // Reactivates a temporarily inactive connection after its bills are paid.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(int id)
    {
        try
        {
            await provisioning.ChangeStatusAsync(id, EmployeeId(), ConnectionStatus.Active);
            TempData["StatusMessage"] = "Connection reactivated.";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Overdue));
    }

    // Employee id stored in the login cookie.
    private int EmployeeId() => int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);

    // Copies the order facts shown on the provisioning form (never taken from the posted form).
    private static void Describe(ConnectionProvisionViewModel m, Order o)
    {
        m.OrderNumber = o.OrderNumber;
        m.ConnectionType = o.ConnectionType;
        m.Quantity = o.Quantity;
        m.IncludesNewLandline = o.ConnectionType == ConnectionType.DialUp && o.LandlinePlanId is not null;
        m.LandlinePlanName = o.LandlinePlan?.Name;
    }

    // Equipment with enough stock for the whole order, and the customer's active Nexus landlines (for dial-up).
    private async Task Populate(ConnectionProvisionViewModel m, int customerId)
    {
        m.Products = await db.Products.Where(p => p.IsActive && p.StockQuantity >= m.Quantity).OrderBy(p => p.Name)
            .Select(p => new SelectListItem(p.Name + " (stock " + p.StockQuantity + ")", p.Id.ToString())).ToListAsync();
        m.Landlines = await db.Connections
            .Where(c => c.CustomerId == customerId && c.ConnectionType == ConnectionType.Telephone && c.Status == ConnectionStatus.Active)
            .OrderBy(c => c.AccountId).Select(c => new SelectListItem(c.AccountId + " — " + c.PhoneNumber, c.Id.ToString())).ToListAsync();
    }
}
