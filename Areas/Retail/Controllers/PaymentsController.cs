using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Models.Workflows;
using NexusServiceMarketingSystem.Services.Workflows;

namespace NexusServiceMarketingSystem.Areas.Retail.Controllers;

[Area("Retail"), Authorize(Roles = nameof(EmployeeRole.RetailStaff))]
public class PaymentsController(AppDbContext db, IBillingService billing) : Controller
{
    public async Task<IActionResult> Index()
    {
        int employeeId=int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
        int? myShop=await db.Employees.Where(e=>e.Id==employeeId&&e.IsActive&&e.Role==EmployeeRole.RetailStaff).Select(e=>e.RetailShopId).SingleOrDefaultAsync();
        if(myShop is null)return Forbid();
        // Only bills this shop may collect: orders handled by this shop, or customer self-service orders.
        var bills=await db.Bills.AsNoTracking().Include(b=>b.Connection).ThenInclude(c=>c.Customer).Include(b=>b.Payments).Where(b=>b.Status!=BillStatus.Paid&&b.Status!=BillStatus.Cancelled&&(b.Connection.Order.RetailShopId==null||b.Connection.Order.RetailShopId==myShop)).OrderBy(b=>b.DueDate).Take(300).ToListAsync();
        return View(bills.Select(b=>new BillListItemViewModel{Bill=b,AmountPaid=b.Payments.Sum(p=>p.Amount),Outstanding=b.TotalAmount-b.Payments.Sum(p=>p.Amount),DisplayStatus=b.DueDate<DateOnly.FromDateTime(DateTime.Today)?"Overdue":b.Payments.Any()?"Partially paid":"Unpaid"}).Where(x=>x.Outstanding>0).ToList());
    }
    // Payments recorded at this employee's shop (till-date payment details).
    public async Task<IActionResult> History()
    {
        int emp=int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
        int? shop=await db.Employees.Where(e=>e.Id==emp&&e.IsActive&&e.Role==EmployeeRole.RetailStaff).Select(e=>e.RetailShopId).SingleOrDefaultAsync();
        if(shop is null)return Forbid();
        var payments=await db.Payments.AsNoTracking().Include(p=>p.Bill).ThenInclude(b=>b.Connection).ThenInclude(c=>c.Customer)
            .Where(p=>p.RetailShopId==shop).OrderByDescending(p=>p.PaidAtUtc).Take(300).ToListAsync();
        return View(payments);
    }
    [HttpGet] public async Task<IActionResult> Collect(int id)
    {
        var b=await db.Bills.AsNoTracking().Include(x=>x.Connection).ThenInclude(c=>c.Customer).Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==id);if(b is null)return NotFound();decimal paid=b.Payments.Sum(p=>p.Amount);
        return View(new PaymentFormViewModel{BillId=b.Id,CustomerName=b.Connection.Customer.FullName,AccountId=b.Connection.AccountId,TotalAmount=b.TotalAmount,AmountPaid=paid,OutstandingAmount=b.TotalAmount-paid});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Collect(PaymentFormViewModel m)
    {
        int emp=int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);int? shop=await db.Employees.Where(e=>e.Id==emp&&e.IsActive&&e.Role==EmployeeRole.RetailStaff).Select(e=>e.RetailShopId).SingleOrDefaultAsync();if(shop is null)return Forbid();
        if(!ModelState.IsValid)return await Collect(m.BillId);
        try{await billing.RecordPaymentAsync(new RecordPaymentRequest(m.BillId,emp,shop,m.Amount,m.Method!.Value,m.Reference));TempData["StatusMessage"]="Payment recorded.";return RedirectToAction(nameof(Index));}
        catch(InvalidOperationException ex){ModelState.AddModelError(string.Empty,ex.Message);return await Collect(m.BillId);}
    }
}
