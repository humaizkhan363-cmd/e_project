using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.ViewComponents;

/// <summary>One figure on a dashboard: label, value, icon name, tone (primary / accent / warn / danger) and a hint.</summary>
public sealed record DashboardStat(string Label, string Value, string Icon, string Tone, string Hint);

/// <summary>
/// Live key figures shown at the top of each role's dashboard. Every number is read from the database for the
/// signed-in user's scope (a retail employee sees their shop, a customer only their own records).
/// </summary>
public class DashboardStatsViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        var user = HttpContext.User;
        List<DashboardStat> stats;

        if (user.IsInRole(nameof(EmployeeRole.Admin)))
        {
            stats = new()
            {
                new("Customers", (await db.Customers.CountAsync(c => c.IsActive)).ToString(), "users", "primary", "active accounts"),
                new("Active connections", (await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Active)).ToString(), "plug", "accent", "currently in service"),
                new("Open orders", (await db.Orders.CountAsync(o => o.Status == OrderStatus.Placed || o.Status == OrderStatus.UnderFeasibilityCheck || o.Status == OrderStatus.Feasible)).ToString(), "clipboard", "warn", "awaiting feasibility or activation"),
                new("Low-stock items", (await db.Products.CountAsync(p => p.IsActive && p.StockQuantity <= p.ReorderLevel)).ToString(), "box", "danger", "at or below reorder level")
            };
        }
        else if (user.IsInRole(nameof(EmployeeRole.RetailStaff)))
        {
            int employeeId = int.Parse(user.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
            int shop = await db.Employees.Where(e => e.Id == employeeId).Select(e => e.RetailShopId ?? 0).SingleOrDefaultAsync();
            var shopBills = db.Bills.Where(b => b.Status != BillStatus.Paid && b.Status != BillStatus.Cancelled
                && (b.Connection.Order.RetailShopId == null || b.Connection.Order.RetailShopId == shop));
            stats = new()
            {
                new("Shop orders", (await db.Orders.CountAsync(o => o.RetailShopId == shop)).ToString(), "clipboard", "primary", "placed at this shop"),
                new("In progress", (await db.Orders.CountAsync(o => o.RetailShopId == shop && (o.Status == OrderStatus.Placed || o.Status == OrderStatus.UnderFeasibilityCheck || o.Status == OrderStatus.Feasible))).ToString(), "clock", "warn", "not yet connected"),
                new("Connections", (await db.Connections.CountAsync(c => c.Order.RetailShopId == shop && c.Status != ConnectionStatus.PermanentlyInactive)).ToString(), "plug", "accent", "provided through this shop"),
                new("Bills to collect", (await shopBills.CountAsync()).ToString(), "receipt", "danger", "unpaid bills this shop may collect")
            };
        }
        else if (user.IsInRole(nameof(EmployeeRole.Technical)))
        {
            stats = new()
            {
                new("Feasibility queue", (await db.Orders.CountAsync(o => o.Status == OrderStatus.Placed || o.Status == OrderStatus.UnderFeasibilityCheck)).ToString(), "radar", "warn", "orders to check"),
                new("Ready to connect", (await db.Orders.CountAsync(o => o.Status == OrderStatus.Feasible)).ToString(), "bolt", "accent", "feasible orders"),
                new("Overdue bills", (await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Active
                    && c.Bills.Any(b => b.DueDate < today && b.Status != BillStatus.Paid && b.Status != BillStatus.Cancelled))).ToString(), "alert", "danger", "active connections to review"),
                new("Low-stock items", (await db.Products.CountAsync(p => p.IsActive && p.StockQuantity <= p.ReorderLevel)).ToString(), "box", "primary", "equipment to reorder")
            };
        }
        else if (user.IsInRole(nameof(EmployeeRole.Accounts)))
        {
            var open = await db.Bills.Where(b => b.Status != BillStatus.Paid && b.Status != BillStatus.Cancelled)
                .Select(b => new { b.DueDate, Due = b.TotalAmount - (b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m) }).ToListAsync();
            DateTime monthStart = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            decimal collected = await db.Payments.Where(p => p.PaidAtUtc >= monthStart).SumAsync(p => (decimal?)p.Amount) ?? 0m;
            stats = new()
            {
                new("Outstanding", open.Sum(b => b.Due).ToString("C0"), "dollar", "primary", "unpaid on open bills"),
                new("Overdue bills", open.Count(b => b.DueDate < today).ToString(), "alert", "danger", "past their due date"),
                new("Collected this month", collected.ToString("C0"), "card", "accent", "payments received"),
                new("Bills issued", (await db.Bills.CountAsync(b => b.Status != BillStatus.Cancelled)).ToString(), "receipt", "warn", "all time")
            };
        }
        else if (user.IsInRole(RoleNames.Customer))
        {
            int customerId = int.Parse(user.FindFirst(RoleNames.CustomerIdClaim)!.Value);
            var dues = await db.Bills.Where(b => b.Connection.CustomerId == customerId && b.Status != BillStatus.Paid && b.Status != BillStatus.Cancelled)
                .Select(b => b.TotalAmount - (b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m)).ToListAsync();
            stats = new()
            {
                new("Active connections", (await db.Connections.CountAsync(c => c.CustomerId == customerId && c.Status == ConnectionStatus.Active)).ToString(), "plug", "accent", "in service"),
                new("Orders in progress", (await db.Orders.CountAsync(o => o.CustomerId == customerId && (o.Status == OrderStatus.Placed || o.Status == OrderStatus.UnderFeasibilityCheck || o.Status == OrderStatus.Feasible))).ToString(), "clock", "warn", "being processed"),
                new("Amount due", dues.Where(d => d > 0).Sum().ToString("C"), "receipt", "danger", "on your open bills"),
                new("Total orders", (await db.Orders.CountAsync(o => o.CustomerId == customerId)).ToString(), "clipboard", "primary", "all time")
            };
        }
        else
        {
            stats = new();
        }

        return View(stats);
    }
}
