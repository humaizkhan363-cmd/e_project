using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Models.Workflows;
using NexusServiceMarketingSystem.Services.Workflows;

namespace NexusServiceMarketingSystem.Areas.Technical.Controllers;

[Area("Technical"), Authorize(Roles = nameof(EmployeeRole.Technical))]
public class FeasibilityController(AppDbContext db, IOrderWorkflowService workflow) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Open orders first (they need Technical action), then the latest finished ones, so staff can track every order.
        List<Order> orders = await db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.FeasibilityChecks)
            .Where(o => o.Status == OrderStatus.Placed || o.Status == OrderStatus.UnderFeasibilityCheck || o.Status == OrderStatus.Feasible)
            .OrderBy(o => o.PlacedAtUtc).ToListAsync();
        List<Order> finished = await db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.FeasibilityChecks)
            .Where(o => o.Status == OrderStatus.NotFeasible || o.Status == OrderStatus.Connected || o.Status == OrderStatus.Cancelled)
            .OrderByDescending(o => o.PlacedAtUtc).Take(100).ToListAsync();
        orders.AddRange(finished);
        return View(orders);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        try { await workflow.StartFeasibilityAsync(id, EmployeeId()); TempData["StatusMessage"] = "Feasibility checks are ready."; }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Check(int id)
    {
        var check = await db.FeasibilityChecks.AsNoTracking().Include(c => c.Order).ThenInclude(o => o.Customer).SingleOrDefaultAsync(c => c.Id == id);
        if (check is null) return NotFound();
        return View(new FeasibilityCheckFormViewModel { Id = check.Id, OrderNumber = check.Order.OrderNumber,
            CustomerName = check.Order.Customer.FullName, CheckType = check.CheckType, Status = check.Status,
            DistanceKm = check.DistanceKm, ServerAvailable = check.ServerAvailable, Remarks = check.Remarks });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Check(FeasibilityCheckFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try { await workflow.CompleteFeasibilityAsync(model.Id, EmployeeId(), model.Status!.Value, model.DistanceKm, model.ServerAvailable, model.Remarks); TempData["StatusMessage"] = "Feasibility result saved."; return RedirectToAction(nameof(Index)); }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
    }

    private int EmployeeId() => int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
}
