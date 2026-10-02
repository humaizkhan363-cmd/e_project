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

/// <summary>
/// Retail outlet employees collect bill payments and see the payments recorded at their shop.
/// A shop may collect bills of orders it handled and of customer self-service orders.
/// </summary>
[Area("Retail"), Authorize(Roles = nameof(EmployeeRole.RetailStaff))]
public class PaymentsController(AppDbContext db, IBillingService billing) : Controller
{
    // Unpaid bills this shop may collect, oldest due date first.
    public async Task<IActionResult> Index()
    {
        int? myShop = await ShopId();
        if (myShop is null) return Forbid();
        var bills = await CollectableBills(myShop.Value).AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer).Include(b => b.Payments)
            .Where(b => b.Status != BillStatus.Paid && b.Status != BillStatus.Cancelled)
            .OrderBy(b => b.DueDate).Take(300).ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        return View(bills.Select(b => new BillListItemViewModel
        {
            Bill = b,
            AmountPaid = b.Payments.Sum(p => p.Amount),
            Outstanding = b.TotalAmount - b.Payments.Sum(p => p.Amount),
            DisplayStatus = b.DueDate < today ? "Overdue" : b.Payments.Any() ? "Partially paid" : "Unpaid"
        }).Where(x => x.Outstanding > 0).ToList());
    }

    // Payments recorded at this employee's shop (till-date payment details).
    public async Task<IActionResult> History()
    {
        int? shop = await ShopId();
        if (shop is null) return Forbid();
        var payments = await db.Payments.AsNoTracking().Include(p => p.Bill).ThenInclude(b => b.Connection).ThenInclude(c => c.Customer)
            .Where(p => p.RetailShopId == shop).OrderByDescending(p => p.PaidAtUtc).Take(300).ToListAsync();
        return View(payments);
    }

    // Payment form for a bill this shop may collect.
    [HttpGet]
    public async Task<IActionResult> Collect(int id)
    {
        int? shop = await ShopId();
        if (shop is null) return Forbid();
        var m = await PaymentForm(shop.Value, id);
        return m is null ? NotFound() : View(m);
    }

    // Records the payment at this employee's shop; the billing service re-checks every rule.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Collect(PaymentFormViewModel m)
    {
        int employee = EmployeeId();
        int? shop = await ShopId();
        if (shop is null) return Forbid();
        var form = await PaymentForm(shop.Value, m.BillId);
        if (form is null) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                await billing.RecordPaymentAsync(new RecordPaymentRequest(m.BillId, employee, shop, m.Amount, m.Method!.Value, m.Reference));
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

    // The signed-in employee's active retail shop, or null.
    private async Task<int?> ShopId()
    {
        int employee = EmployeeId();
        return await db.Employees.Where(e => e.Id == employee && e.IsActive && e.Role == EmployeeRole.RetailStaff)
            .Select(e => e.RetailShopId).SingleOrDefaultAsync();
    }

    // Bills of orders handled by this shop, or of customer self-service orders.
    private IQueryable<Bill> CollectableBills(int shop) =>
        db.Bills.Where(b => b.Connection.Order.RetailShopId == null || b.Connection.Order.RetailShopId == shop);

    // Builds the payment form, including what is still unpaid on the connection's other bills.
    private async Task<PaymentFormViewModel?> PaymentForm(int shop, int billId)
    {
        var b = await CollectableBills(shop).AsNoTracking().Include(x => x.Connection).ThenInclude(c => c.Customer).Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == billId);
        if (b is null) return null;
        decimal paid = b.Payments.Sum(p => p.Amount);
        decimal otherUnpaid = (await db.Bills
            .Where(x => x.ConnectionId == b.ConnectionId && x.Id != b.Id && x.Status != BillStatus.Cancelled && x.Status != BillStatus.Paid)
            .Select(x => x.TotalAmount - (x.Payments.Sum(p => (decimal?)p.Amount) ?? 0m)).ToListAsync())
            .Where(x => x > 0).Sum();
        return new PaymentFormViewModel
        {
            BillId = b.Id,
            CustomerName = b.Connection.Customer.FullName,
            AccountId = b.Connection.AccountId,
            TotalAmount = b.TotalAmount,
            AmountPaid = paid,
            OutstandingAmount = b.TotalAmount - paid,
            PreviousBalance = otherUnpaid
        };
    }
}
