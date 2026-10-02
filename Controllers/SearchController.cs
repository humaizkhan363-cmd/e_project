using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Models.Workflows;

namespace NexusServiceMarketingSystem.Controllers
{
    /// <summary>
    /// Advanced search for Retail, Technical and Accounts employees (the Admin has the same page under
    /// Admin > Advanced search). Orders are found by order number, connections by account ID, or both by
    /// customer name / contact number / e-mail, service type, status and application date range.
    /// A retail employee only sees the orders and connections handled by his or her own shop.
    /// </summary>
    [Authorize(Roles = "RetailStaff,Technical,Accounts")]
    public class SearchController : Controller
    {
        private readonly AppDbContext _db;

        public SearchController(AppDbContext db)
        {
            _db = db;
        }

        // Runs the advanced search over orders and connections.
        [HttpGet]
        public async Task<IActionResult> Index(AdvancedSearchViewModel m)
        {
            // Retail employees are limited to their own shop.
            int? shop = null;
            if (User.IsInRole(nameof(EmployeeRole.RetailStaff)))
            {
                int employeeId = int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
                shop = await _db.Employees.Where(e => e.Id == employeeId && e.IsActive && e.Role == EmployeeRole.RetailStaff)
                    .Select(e => e.RetailShopId).SingleOrDefaultAsync();
                if (shop is null)
                {
                    return Forbid();
                }
            }

            // ---- Orders ----
            var orders = _db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan).AsQueryable();
            if (shop.HasValue) orders = orders.Where(o => o.RetailShopId == shop);
            if (!string.IsNullOrWhiteSpace(m.OrderNumber)) orders = orders.Where(o => o.OrderNumber.Contains(m.OrderNumber));
            if (!string.IsNullOrWhiteSpace(m.Customer))
                orders = orders.Where(o => o.Customer.FullName.Contains(m.Customer) || o.Customer.Phone.Contains(m.Customer) || (o.Customer.Email != null && o.Customer.Email.Contains(m.Customer)));
            if (m.ConnectionType.HasValue) orders = orders.Where(o => o.ConnectionType == m.ConnectionType.Value);
            if (m.OrderStatus.HasValue) orders = orders.Where(o => o.Status == m.OrderStatus.Value);
            if (m.From.HasValue) orders = orders.Where(o => o.PlacedAtUtc >= m.From.Value.Date);
            if (m.Through.HasValue) orders = orders.Where(o => o.PlacedAtUtc < m.Through.Value.Date.AddDays(1));
            m.Orders = await orders.OrderByDescending(o => o.PlacedAtUtc).Take(200).ToListAsync();

            // ---- Connections ----
            var connections = _db.Connections.AsNoTracking().Include(c => c.Customer).Include(c => c.Plan).AsQueryable();
            if (shop.HasValue) connections = connections.Where(c => c.Order.RetailShopId == shop);
            if (!string.IsNullOrWhiteSpace(m.AccountId)) connections = connections.Where(c => c.AccountId.Contains(m.AccountId));
            if (!string.IsNullOrWhiteSpace(m.Customer))
                connections = connections.Where(c => c.Customer.FullName.Contains(m.Customer) || c.Customer.Phone.Contains(m.Customer) || (c.Customer.Email != null && c.Customer.Email.Contains(m.Customer)));
            if (m.ConnectionType.HasValue) connections = connections.Where(c => c.ConnectionType == m.ConnectionType.Value);
            if (m.ConnectionStatus.HasValue) connections = connections.Where(c => c.Status == m.ConnectionStatus.Value);
            if (m.From.HasValue) connections = connections.Where(c => c.ActivatedAtUtc >= m.From.Value.Date);
            if (m.Through.HasValue) connections = connections.Where(c => c.ActivatedAtUtc < m.Through.Value.Date.AddDays(1));
            m.Connections = await connections.OrderBy(c => c.AccountId).Take(200).ToListAsync();

            return View(m);
        }
    }
}
