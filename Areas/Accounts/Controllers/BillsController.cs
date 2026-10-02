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

namespace NexusServiceMarketingSystem.Areas.Accounts.Controllers;

[Area("Accounts"), Authorize(Roles = nameof(EmployeeRole.Accounts))]
public class BillsController(AppDbContext db, IBillingService billing) : Controller
{
    public async Task<IActionResult> Index()
    {
        var bills = await db.Bills.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer).Include(b => b.Payments)
            .OrderByDescending(b => b.IssueDate).Take(500).ToListAsync();
        return View(bills.Select(b => Item(b)).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var m = new BillGenerationViewModel { IssueDate = DateOnly.FromDateTime(DateTime.Today), DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)), PeriodStart = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)), PeriodEnd = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) };
        await Populate(m); return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BillGenerationViewModel m)
    {
        if (!ModelState.IsValid) { await Populate(m); return View(m); }
        try
        {
            var bill = await billing.GenerateBillAsync(new GenerateBillRequest(m.ConnectionId, EmployeeId(), m.PeriodStart, m.PeriodEnd, m.IssueDate, m.DueDate, m.UsageCharge, m.ReplacementCharge));
            TempData["StatusMessage"] = $"Bill #{bill.Id} generated."; return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); await Populate(m); return View(m); }
    }

    [HttpGet]
    public async Task<IActionResult> Payment(int id)
    {
        var bill = await db.Bills.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer).Include(b => b.Payments).SingleOrDefaultAsync(b => b.Id == id);
        if (bill is null) return NotFound();
        decimal paid = bill.Payments.Sum(p => p.Amount);
        return View(new PaymentFormViewModel { BillId = bill.Id, CustomerName = bill.Connection.Customer.FullName, AccountId = bill.Connection.AccountId, TotalAmount = bill.TotalAmount, AmountPaid = paid, OutstandingAmount = bill.TotalAmount-paid });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Payment(PaymentFormViewModel m)
    {
        if (!ModelState.IsValid) return await Payment(m.BillId) is ViewResult v ? View(v.Model) : NotFound();
        try { await billing.RecordPaymentAsync(new RecordPaymentRequest(m.BillId, EmployeeId(), null, m.Amount, m.Method!.Value, m.Reference)); TempData["StatusMessage"] = "Payment recorded."; return RedirectToAction(nameof(Index)); }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); var view = await Payment(m.BillId); return view is ViewResult v ? View(v.Model) : NotFound(); }
    }

    private int EmployeeId() => int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
    private async Task Populate(BillGenerationViewModel m) => m.Connections = await db.Connections.Where(c => c.Status != ConnectionStatus.PermanentlyInactive).Include(c => c.Customer).OrderBy(c => c.AccountId).Select(c => new SelectListItem(c.AccountId + " — " + c.Customer.FullName, c.Id.ToString())).ToListAsync();
    private static BillListItemViewModel Item(Bill b) { var paid=b.Payments.Sum(p=>p.Amount); var due=b.TotalAmount-paid; var status=due<=0?"Paid":b.DueDate<DateOnly.FromDateTime(DateTime.Today)?"Overdue":paid>0?"Partially paid":"Unpaid"; return new(){Bill=b,AmountPaid=paid,Outstanding=due,DisplayStatus=status}; }
}
