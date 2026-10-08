using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Common;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Controllers
{
    // Search the status of an order (by order number) or a connection (by account ID).
    // Anyone may check status; money amounts are shown only to a signed-in user who is allowed to see them.
    public class TrackController : Controller
    {
        private readonly AppDbContext _db;

        public TrackController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? orderNumber, string? accountId)
        {
            var model = new TrackViewModel
            {
                OrderNumber = orderNumber?.Trim().ToUpperInvariant(),
                AccountId = accountId?.Trim().ToUpperInvariant()
            };

            if (!string.IsNullOrEmpty(model.OrderNumber))
            {
                model.OrderSearched = true;
                if (IdentifierFormat.IsValidOrderNumber(model.OrderNumber))
                {
                    string number = model.OrderNumber;
                    model.Order = await _db.Orders.AsNoTracking()
                        .Include(o => o.Plan).Include(o => o.City)
                        .Include(o => o.FeasibilityChecks).Include(o => o.Connections)
                        .SingleOrDefaultAsync(o => o.OrderNumber == number);
                }
            }

            if (!string.IsNullOrEmpty(model.AccountId))
            {
                model.AccountSearched = true;
                if (IdentifierFormat.IsValidAccountId(model.AccountId))
                {
                    string id = model.AccountId;
                    Connection? connection = await _db.Connections.AsNoTracking()
                        .Include(c => c.Plan).Include(c => c.City)
                        .SingleOrDefaultAsync(c => c.AccountId == id);
                    if (connection is not null)
                    {
                        model.Connection = connection;
                        model.CanSeeAmounts = CanSeeAmounts(connection.CustomerId);
                        if (model.CanSeeAmounts)
                        {
                            model.TotalBilled = await _db.Bills
                                .Where(b => b.ConnectionId == connection.Id && b.Status != BillStatus.Cancelled)
                                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0m;
                            model.TotalPaid = await _db.Payments
                                .Where(p => p.Bill.ConnectionId == connection.Id && p.Bill.Status != BillStatus.Cancelled)
                                .SumAsync(p => (decimal?)p.Amount) ?? 0m;
                        }
                    }
                }
            }

            return View(model);
        }

        // Staff may see any connection's amounts; a customer only their own; visitors never.
        private bool CanSeeAmounts(int connectionCustomerId)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return false;
            }
            string? role = User.FindFirstValue(ClaimTypes.Role);
            if (role == RoleNames.Customer)
            {
                return int.TryParse(User.FindFirstValue(RoleNames.CustomerIdClaim), out int own) && own == connectionCustomerId;
            }
            return true;
        }
    }

    public class TrackViewModel
    {
        public string? OrderNumber { get; set; }
        public string? AccountId { get; set; }
        public bool OrderSearched { get; set; }
        public bool AccountSearched { get; set; }
        public Order? Order { get; set; }
        public Connection? Connection { get; set; }
        public bool CanSeeAmounts { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal AmountDue => Math.Max(0m, TotalBilled - TotalPaid);
    }
}
