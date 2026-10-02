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

[Area("Retail"), Authorize(Roles = nameof(EmployeeRole.RetailStaff))]
public class OrdersController(AppDbContext db, IOrderWorkflowService workflow) : Controller
{
    public async Task<IActionResult> Index()
    {
        int shop = await ShopId(); if (shop == 0) return Forbid();
        return View(await db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan)
            .Where(o => o.RetailShopId == shop).OrderByDescending(o => o.PlacedAtUtc).Take(300).ToListAsync());
    }

    [HttpGet] public async Task<IActionResult> Create(int? customerId) { var m = new OrderFormViewModel { CustomerId = customerId }; await Populate(m); return View(m); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderFormViewModel m)
    {
        if (!ModelState.IsValid) { await Populate(m); return View(m); }
        int employee = int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value); int shop = await ShopId(); if (shop == 0) return Forbid();
        try { var o = await workflow.PlaceAsync(new PlaceOrderRequest(m.CustomerId!.Value, m.ConnectionType!.Value, m.PlanId, m.CityId, m.InstallationAddress, m.Quantity, shop, employee)); TempData["StatusMessage"] = $"Order {o.OrderNumber} placed."; return RedirectToAction(nameof(Details), new { id = o.Id }); }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); await Populate(m); return View(m); }
    }

    public async Task<IActionResult> Details(int id)
    {
        int shop = await ShopId(); var o = await db.Orders.AsNoTracking().Include(x => x.Customer).Include(x => x.City).Include(x => x.Plan).Include(x => x.FeasibilityChecks).Include(x => x.Connections).SingleOrDefaultAsync(x => x.Id == id && x.RetailShopId == shop);
        return o is null ? NotFound() : View(o);
    }

    private async Task<int> ShopId() { int id = int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value); return await db.Employees.Where(e => e.Id == id && e.IsActive && e.Role == EmployeeRole.RetailStaff).Select(e => e.RetailShopId ?? 0).SingleOrDefaultAsync(); }
    private async Task Populate(OrderFormViewModel m)
    {
        m.Customers = await db.Customers.Where(c => c.IsActive).OrderBy(c => c.FullName).Select(c => new SelectListItem(c.FullName + " — " + c.Phone, c.Id.ToString())).ToListAsync();
        var activePlans = await db.Plans.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.ConnectionType).ThenBy(p => p.Price).ToListAsync();
        var planGroups = new Dictionary<ConnectionType, SelectListGroup>();
        m.Plans = activePlans.Select(p =>
        {
            if (!planGroups.TryGetValue(p.ConnectionType, out SelectListGroup? planGroup))
            {
                planGroup = new SelectListGroup { Name = p.ConnectionType.ToString() };
                planGroups[p.ConnectionType] = planGroup;
            }
            return new SelectListItem { Text = p.Name + " - " + p.Price.ToString("C"), Value = p.Id.ToString(), Group = planGroup };
        }).ToList();
        m.Cities = await db.Cities.Where(c => c.IsActive).OrderBy(c => c.Name).Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToListAsync();
        m.ConnectionTypes = Enum.GetValues<ConnectionType>().Select(x => new SelectListItem(x.ToString(), ((int)x).ToString())).ToList();
    }
}
