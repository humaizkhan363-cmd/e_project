using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Retail.Controllers;

// Connections created for orders handled by this employee's shop, with the billed/paid/due position (till-date billing details).
[Area("Retail"), Authorize(Roles = nameof(EmployeeRole.RetailStaff))]
public class ConnectionsController(AppDbContext db) : Controller
{
    // Connections of this shop with amount billed, paid and due.
    public async Task<IActionResult> Index()
    {
        int emp = int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
        int? shop = await db.Employees.Where(e => e.Id == emp && e.IsActive && e.Role == EmployeeRole.RetailStaff).Select(e => e.RetailShopId).SingleOrDefaultAsync();
        if (shop is null) return Forbid();

        var connections = await db.Connections.AsNoTracking().Include(c => c.Customer).Include(c => c.Plan)
            .Where(c => c.Order.RetailShopId == shop).OrderByDescending(c => c.ActivatedAtUtc).Take(300).ToListAsync();
        var bills = await db.Bills.AsNoTracking()
            .Where(b => b.Connection.Order.RetailShopId == shop && b.Status != BillStatus.Cancelled)
            .Select(b => new { b.ConnectionId, b.TotalAmount, Paid = b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m }).ToListAsync();

        return View(connections.Select(c => new ShopConnectionItem
        {
            Connection = c,
            Billed = bills.Where(b => b.ConnectionId == c.Id).Sum(b => b.TotalAmount),
            Paid = bills.Where(b => b.ConnectionId == c.Id).Sum(b => b.Paid)
        }).ToList());
    }
}

/// <summary>One connection row with its till-date billing position.</summary>
public class ShopConnectionItem
{
    public Connection Connection { get; set; } = null!;
    public decimal Billed { get; set; }
    public decimal Paid { get; set; }
    public decimal Due => Math.Max(0m, Billed - Paid);
}
