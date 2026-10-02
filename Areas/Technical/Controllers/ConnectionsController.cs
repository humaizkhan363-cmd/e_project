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

[Area("Technical"),Authorize(Roles=nameof(EmployeeRole.Technical))]
public class ConnectionsController(AppDbContext db,IConnectionProvisioningService provisioning):Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Plans=await db.Plans.AsNoTracking().Where(p=>p.IsActive).OrderBy(p=>p.Name).ToListAsync();
        ViewBag.Products=await db.Products.AsNoTracking().Where(p=>p.IsActive&&p.StockQuantity>0).OrderBy(p=>p.Name).ToListAsync();
        return View(await db.Connections.AsNoTracking().Include(c=>c.Customer).Include(c=>c.Plan).Include(c=>c.Order).OrderByDescending(c=>c.ActivatedAtUtc).Take(500).ToListAsync());
    }
    // Customer asks to move to another plan of the same connection type (security deposit is per type, so it stays the same).
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePlan(int id,int planId)
    {
        var c=await db.Connections.SingleOrDefaultAsync(x=>x.Id==id);
        var p=await db.Plans.SingleOrDefaultAsync(x=>x.Id==planId&&x.IsActive);
        if(c is null||p is null||c.Status==ConnectionStatus.PermanentlyInactive||p.ConnectionType!=c.ConnectionType)
        {TempData["ErrorMessage"]="Plan could not be changed. Choose an active plan of the same connection type for a connection that is not permanently inactive.";return RedirectToAction(nameof(Index));}
        c.PlanId=p.Id;c.StatusChangedAtUtc=DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["StatusMessage"]="Plan changed. Future bills will use the new plan.";
        return RedirectToAction(nameof(Index));
    }
    // Equipment spoiled by the customer is replaced from stock: the old unit is marked returned and the new unit is recorded as a replacement.
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplaceEquipment(int id,int productId,string? serialNumber)
    {
        var c=await db.Connections.Include(x=>x.ConnectionProducts).SingleOrDefaultAsync(x=>x.Id==id);
        var p=await db.Products.SingleOrDefaultAsync(x=>x.Id==productId&&x.IsActive);
        if(c is null||p is null||c.ConnectionType==ConnectionType.Telephone||c.Status==ConnectionStatus.PermanentlyInactive)
        {TempData["ErrorMessage"]="Equipment could not be replaced. Choose active equipment for an internet connection that is not permanently inactive.";return RedirectToAction(nameof(Index));}
        if(p.StockQuantity<1){TempData["ErrorMessage"]=p.Name+" is out of stock.";return RedirectToAction(nameof(Index));}
        foreach(var old in c.ConnectionProducts.Where(x=>x.ReturnedAtUtc==null)){old.ReturnedAtUtc=DateTime.UtcNow;}
        db.ConnectionProducts.Add(new ConnectionProduct{ConnectionId=c.Id,ProductId=p.Id,Quantity=1,SerialNumber=string.IsNullOrWhiteSpace(serialNumber)?null:serialNumber.Trim(),IsReplacement=true,Notes="Replacement issued by Technical staff."});
        p.StockQuantity-=1;
        try{await db.SaveChangesAsync();}
        catch(DbUpdateException){TempData["ErrorMessage"]="The replacement could not be saved. The serial number may already be in use, or the stock changed. Please try again.";return RedirectToAction(nameof(Index));}
        TempData["StatusMessage"]="Replacement issued for "+c.AccountId+". Replacement charge: "+p.ReplacementCharge.ToString("C")+" (Accounts adds it to the customer's next bill).";
        return RedirectToAction(nameof(Index));
    }
    [HttpGet] public async Task<IActionResult> Provision(int id)
    {
        var o=await db.Orders.Include(x=>x.Customer).SingleOrDefaultAsync(x=>x.Id==id&&x.Status==OrderStatus.Feasible);if(o is null)return NotFound();
        var m=new ConnectionProvisionViewModel{OrderId=o.Id,OrderNumber=o.OrderNumber,ConnectionType=o.ConnectionType,Quantity=o.Quantity};await Populate(m,o.CustomerId);return View(m);
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Provision(ConnectionProvisionViewModel m)
    {
        var o=await db.Orders.Include(x=>x.Customer).SingleOrDefaultAsync(x=>x.Id==m.OrderId);if(o is null)return NotFound();
        m.OrderNumber=o.OrderNumber;m.ConnectionType=o.ConnectionType;m.Quantity=o.Quantity;
        if(!ModelState.IsValid){await Populate(m,o.CustomerId);return View(m);}
        try{var result=await provisioning.ProvisionAsync(new ProvisionConnectionRequest(m.OrderId,int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value),m.ProductId,m.SerialNumber,m.LandlineConnectionId,m.PhoneNumber));TempData["StatusMessage"]=$"Provisioned {result.Count} connection(s).";return RedirectToAction(nameof(Index));}
        catch(InvalidOperationException ex){ModelState.AddModelError(string.Empty,ex.Message);await Populate(m,o.CustomerId);return View(m);}
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(ConnectionStatusFormViewModel m)
    {
        try{await provisioning.ChangeStatusAsync(m.Id,int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value),m.Status);TempData["StatusMessage"]="Connection status updated.";}
        catch(InvalidOperationException ex){TempData["ErrorMessage"]=ex.Message;}return RedirectToAction(nameof(Index));
    }
    private async Task Populate(ConnectionProvisionViewModel m,int customerId)
    {
        m.Products=await db.Products.Where(p=>p.IsActive&&p.StockQuantity>=m.Quantity).OrderBy(p=>p.Name).Select(p=>new SelectListItem(p.Name+" (stock "+p.StockQuantity+")",p.Id.ToString())).ToListAsync();
        m.Landlines=await db.Connections.Where(c=>c.CustomerId==customerId&&c.ConnectionType==ConnectionType.Telephone&&c.Status==ConnectionStatus.Active).OrderBy(c=>c.AccountId).Select(c=>new SelectListItem(c.AccountId+" — "+c.PhoneNumber,c.Id.ToString())).ToListAsync();
    }
}
