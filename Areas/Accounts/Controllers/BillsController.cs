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

/// <summary>
/// The Accounts department generates the postpaid bills (only Accounts may do this) and records payments made at the office.
/// </summary>
[Area("Accounts"), Authorize(Roles = nameof(EmployeeRole.Accounts))]
public class BillsController(AppDbContext db, IBillingService billing) : Controller
{
    // All bills, newest first, with amount paid, amount due and status.
    public async Task<IActionResult> Index()
    {
        var bills = await db.Bills.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer).Include(b => b.Payments)
            .OrderByDescending(b => b.IssueDate).Take(500).ToListAsync();
        return View(bills.Select(Item).ToList());
    }

    // Bill form. Choosing a connection (and period start) shows what the bill will contain before it is generated.
    [HttpGet]
    public async Task<IActionResult> Create(int? connectionId, DateOnly? periodStart)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var m = new BillGenerationViewModel
        {
            ConnectionId = connectionId ?? 0,
            IssueDate = today,
            DueDate = today.AddDays(14),
            PeriodStart = periodStart ?? today.AddMonths(-1),
            PeriodEnd = (periodStart ?? today.AddMonths(-1)).AddMonths(1).AddDays(-1)
        };
        await Populate(m);
        return View(m);
    }

    // Generates the bill: plan fee (once per validity period), call charges from the minutes, other usage,
    // unbilled replacement charges, deposit and bulk discount on the first bill, and 12.24 % service tax.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BillGenerationViewModel m)
    {
        if (!ModelState.IsValid) { await Populate(m); return View(m); }
        try
        {
            var bill = await billing.GenerateBillAsync(new GenerateBillRequest(m.ConnectionId, EmployeeId(), m.PeriodStart, m.PeriodEnd,
                m.IssueDate, m.DueDate, m.LocalMinutes, m.StdMinutes, m.MobileMinutes, m.OtherUsageCharge));
            TempData["StatusMessage"] = $"Bill #{bill.Id} generated: {bill.TotalAmount:C}"
                + (bill.PreviousBalance > 0 ? $" (plus {bill.PreviousBalance:C} unpaid from earlier bills)." : ".");
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await Populate(m);
            return View(m);
        }
    }

    // Payment form for one bill, showing the bill total, the amount paid and the amount due.
    [HttpGet]
    public async Task<IActionResult> Payment(int id)
    {
        var m = await PaymentForm(id);
        return m is null ? NotFound() : View(m);
    }

    // Records a payment received at the Accounts office (no retail shop).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Payment(PaymentFormViewModel m)
    {
        var form = await PaymentForm(m.BillId);
        if (form is null) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                await billing.RecordPaymentAsync(new RecordPaymentRequest(m.BillId, EmployeeId(), null, m.Amount, m.Method!.Value, m.Reference));
                TempData["StatusMessage"] = "Payment recorded.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        }
        form.Amount = m.Amount;
        form.Method = m.Method;
        form.Reference = m.Reference;
        return View(form);
    }

    // Employee id stored in the login cookie.
    private int EmployeeId() => int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);

    // Builds the payment form for a bill, including what is still unpaid on the connection's other bills.
    private async Task<PaymentFormViewModel?> PaymentForm(int billId)
    {
        var bill = await db.Bills.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer).Include(b => b.Payments)
            .SingleOrDefaultAsync(b => b.Id == billId);
        if (bill is null) return null;
        decimal paid = bill.Payments.Sum(p => p.Amount);
        decimal otherUnpaid = (await db.Bills
            .Where(b => b.ConnectionId == bill.ConnectionId && b.Id != bill.Id && b.Status != BillStatus.Cancelled && b.Status != BillStatus.Paid)
            .Select(b => b.TotalAmount - (b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m)).ToListAsync())
            .Where(x => x > 0).Sum();
        return new PaymentFormViewModel
        {
            BillId = bill.Id,
            CustomerName = bill.Connection.Customer.FullName,
            AccountId = bill.Connection.AccountId,
            TotalAmount = bill.TotalAmount,
            AmountPaid = paid,
            OutstandingAmount = bill.TotalAmount - paid,
            PreviousBalance = otherUnpaid
        };
    }

    // Connections that can be billed, plus the charges preview of the chosen connection.
    private async Task Populate(BillGenerationViewModel m)
    {
        m.Connections = await db.Connections.Where(c => c.Status != ConnectionStatus.PermanentlyInactive).Include(c => c.Customer)
            .OrderBy(c => c.AccountId).Select(c => new SelectListItem(c.AccountId + " — " + c.Customer.FullName, c.Id.ToString())).ToListAsync();
        if (m.ConnectionId > 0)
        {
            try { m.Preview = await billing.PreviewAsync(m.ConnectionId, m.PeriodStart); }
            catch (InvalidOperationException) { m.Preview = null; }
        }
    }

    // Bill list row: amount paid, amount due and a readable status (Paid / Overdue / Partially paid / Unpaid).
    private static BillListItemViewModel Item(Bill b)
    {
        var paid = b.Payments.Sum(p => p.Amount);
        var due = b.TotalAmount - paid;
        var status = due <= 0 ? "Paid" : b.DueDate < DateOnly.FromDateTime(DateTime.Today) ? "Overdue" : paid > 0 ? "Partially paid" : "Unpaid";
        return new() { Bill = b, AmountPaid = paid, Outstanding = due, DisplayStatus = status };
    }
}
