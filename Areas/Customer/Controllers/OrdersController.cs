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

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

[Area("Customer"), Authorize(Roles = RoleNames.Customer)]
public class OrdersController(AppDbContext db, IOrderWorkflowService workflow) : Controller
{
    public async Task<IActionResult> Index()
    {
        int customerId = CustomerId();
        return View(await db.Orders.AsNoTracking().Include(o => o.Plan).Include(o => o.City)
            .Where(o => o.CustomerId == customerId).OrderByDescending(o => o.PlacedAtUtc).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new OrderFormViewModel();
        await Populate(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderFormViewModel model)
    {
        if (!ModelState.IsValid) { await Populate(model); return View(model); }
        try
        {
            var order = await workflow.PlaceAsync(new PlaceOrderRequest(CustomerId(), model.ConnectionType!.Value,
                model.PlanId, model.CityId, model.InstallationAddress, model.Quantity, null, null));
            TempData["StatusMessage"] = $"Order {order.OrderNumber} submitted. Track its progress here.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); await Populate(model); return View(model); }
    }

    public async Task<IActionResult> Details(int id)
    {
        int customerId = CustomerId();
        var order = await db.Orders.AsNoTracking().Include(o => o.Plan).Include(o => o.City)
            .Include(o => o.FeasibilityChecks).Include(o => o.Connections).ThenInclude(c => c.Plan)
            .SingleOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);
        return order is null ? NotFound() : View(order);
    }

    // A customer may cancel an order until the connection has been created.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        int customerId = CustomerId();
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);
        if (order is null) return NotFound();
        if (order.Status is OrderStatus.Placed or OrderStatus.UnderFeasibilityCheck or OrderStatus.Feasible)
        {
            order.Status = OrderStatus.Cancelled;
            order.StatusChangedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            TempData["StatusMessage"] = "Order cancelled.";
        }
        else TempData["ErrorMessage"] = "This order can no longer be cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private int CustomerId() => int.Parse(User.FindFirst(RoleNames.CustomerIdClaim)!.Value);
    private async Task Populate(OrderFormViewModel m)
    {
        m.ConnectionTypes = Enum.GetValues<ConnectionType>().Select(x => new SelectListItem(x.ToString(), ((int)x).ToString())).ToList();
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
    }
}
