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

namespace NexusServiceMarketingSystem.Areas.Retail.Controllers;

/// <summary>
/// Retail outlet employees place connection orders for customers and track the orders placed at their own shop.
/// </summary>
[Area("Retail"), Authorize(Roles = nameof(EmployeeRole.RetailStaff))]
public class OrdersController(AppDbContext db, IOrderWorkflowService workflow) : Controller
{
    // Lists the orders placed at this employee's shop, newest first.
    public async Task<IActionResult> Index()
    {
        int shop = await ShopId();
        if (shop == 0) return Forbid();
        return View(await db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan)
            .Where(o => o.RetailShopId == shop).OrderByDescending(o => o.PlacedAtUtc).Take(300).ToListAsync());
    }

    // Shows the order form; a customer can be pre-selected from the customer details page.
    [HttpGet]
    public async Task<IActionResult> Create(int? customerId)
    {
        var m = new OrderFormViewModel { CustomerId = customerId };
        await Populate(m);
        return View(m);
    }

    // Places the order through the workflow service, which generates the order number and applies bulk discounts.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderFormViewModel m)
    {
        if (!ModelState.IsValid) { await Populate(m); return View(m); }
        int employee = int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
        int shop = await ShopId();
        if (shop == 0) return Forbid();
        try
        {
            var o = await workflow.PlaceAsync(new PlaceOrderRequest(m.CustomerId!.Value, m.ConnectionType!.Value, m.PlanId, m.CityId,
                m.InstallationAddress, m.Quantity, shop, employee, m.LandlinePlanId));
            TempData["StatusMessage"] = $"Order {o.OrderNumber} placed.";
            return RedirectToAction(nameof(Details), new { id = o.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await Populate(m);
            return View(m);
        }
    }

    // Order status, feasibility results and connections created for an order of this shop.
    public async Task<IActionResult> Details(int id)
    {
        int shop = await ShopId();
        var o = await db.Orders.AsNoTracking().Include(x => x.Customer).Include(x => x.City).Include(x => x.Plan).Include(x => x.LandlinePlan)
            .Include(x => x.FeasibilityChecks).Include(x => x.Connections)
            .SingleOrDefaultAsync(x => x.Id == id && x.RetailShopId == shop);
        return o is null ? NotFound() : View(o);
    }

    // The retail shop of the signed-in employee (0 when the employee has no active shop assignment).
    private async Task<int> ShopId()
    {
        int id = int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
        return await db.Employees.Where(e => e.Id == id && e.IsActive && e.Role == EmployeeRole.RetailStaff)
            .Select(e => e.RetailShopId ?? 0).SingleOrDefaultAsync();
    }

    // Fills the drop-down lists of the order form (plans are grouped by service type, with prices).
    private async Task Populate(OrderFormViewModel m)
    {
        m.Customers = await db.Customers.Where(c => c.IsActive).OrderBy(c => c.FullName)
            .Select(c => new SelectListItem(c.FullName + " — " + c.Phone, c.Id.ToString())).ToListAsync();
        var activePlans = await db.Plans.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.ConnectionType).ThenBy(p => p.Price).ToListAsync();
        m.Plans = OrderFormLists.GroupedPlans(activePlans);
        m.LandlinePlans = OrderFormLists.LandlinePlans(activePlans);
        m.Cities = await db.Cities.Where(c => c.IsActive).OrderBy(c => c.Name).Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToListAsync();
        m.ConnectionTypes = OrderFormLists.ConnectionTypes();
    }
}
