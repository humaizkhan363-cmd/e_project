using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Workflows;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

/// <summary>
/// Customer self-service: contact details, bills, connections (status and amount due) and feedback.
/// Every query is limited to the signed-in customer.
/// </summary>
[Area("Customer"), Authorize(Roles = RoleNames.Customer)]
public class PortalController(AppDbContext db) : Controller
{
    // Shows the customer's own contact details.
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var c = await OwnCustomer().Include(x => x.City).SingleOrDefaultAsync();
        if (c is null) return NotFound();
        return View(new ProfileForm
        {
            FullName = c.FullName, Email = c.Email, Phone = c.Phone, AddressLine = c.AddressLine, PostalCode = c.PostalCode, CityName = c.City.Name
        });
    }

    // Updates the contact details (an email, when given, must not belong to another customer).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileForm m)
    {
        if (!ModelState.IsValid) return View(m);
        var c = await OwnCustomer().SingleOrDefaultAsync();
        if (c is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(m.Email) && await db.Customers.AnyAsync(x => x.Id != c.Id && x.Email == m.Email.Trim()))
        {
            ModelState.AddModelError(nameof(m.Email), "That email is already used.");
            return View(m);
        }
        c.FullName = m.FullName.Trim();
        c.Email = m.Email?.Trim();
        c.Phone = m.Phone.Trim();
        c.AddressLine = m.AddressLine.Trim();
        c.PostalCode = m.PostalCode?.Trim();
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    // The customer's bills with charges, amount paid, amount due and status.
    public async Task<IActionResult> Bills()
    {
        int id = CustomerId();
        var bills = await db.Bills.AsNoTracking().Include(b => b.Connection).Include(b => b.Payments)
            .Where(b => b.Connection.CustomerId == id).OrderByDescending(b => b.IssueDate).ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        return View(bills.Select(b =>
        {
            decimal paid = b.Payments.Sum(p => p.Amount);
            string status = b.TotalAmount <= paid ? "Paid" : b.DueDate < today ? "Overdue" : paid > 0 ? "Partially paid" : "Unpaid";
            return new BillListItemViewModel { Bill = b, AmountPaid = paid, Outstanding = b.TotalAmount - paid, DisplayStatus = status };
        }).ToList());
    }

    // The customer's connections with status and balance (account ID, status and dues in one place).
    public async Task<IActionResult> Connections()
    {
        int id = CustomerId();
        var connections = await db.Connections.AsNoTracking().Include(c => c.Plan).Include(c => c.City)
            .Where(c => c.CustomerId == id).OrderBy(c => c.AccountId).ToListAsync();
        var bills = await db.Bills.AsNoTracking().Where(b => b.Connection.CustomerId == id && b.Status != Models.Enums.BillStatus.Cancelled)
            .Select(b => new { b.ConnectionId, b.TotalAmount, Paid = b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m }).ToListAsync();
        return View(connections.Select(c => new MyConnectionItem
        {
            Connection = c,
            Due = bills.Where(b => b.ConnectionId == c.Id).Sum(b => b.TotalAmount - b.Paid)
        }).ToList());
    }

    // Feedback form (optionally about one of the customer's orders or connections).
    [HttpGet]
    public async Task<IActionResult> Feedback()
    {
        return await FeedbackForm(new FeedbackFormViewModel());
    }

    // Saves feedback; a linked order or connection must belong to the customer.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(FeedbackFormViewModel m)
    {
        int id = CustomerId();
        if (!ModelState.IsValid) return await FeedbackForm(m);
        if (m.OrderId.HasValue && !await db.Orders.AnyAsync(o => o.Id == m.OrderId && o.CustomerId == id))
        {
            ModelState.AddModelError(nameof(m.OrderId), "Choose one of your own orders.");
            return await FeedbackForm(m);
        }
        if (m.ConnectionId.HasValue && !await db.Connections.AnyAsync(c => c.Id == m.ConnectionId && c.CustomerId == id))
        {
            ModelState.AddModelError(nameof(m.ConnectionId), "Choose one of your own connections.");
            return await FeedbackForm(m);
        }
        db.Feedbacks.Add(new Feedback { CustomerId = id, OrderId = m.OrderId, ConnectionId = m.ConnectionId, Rating = m.Rating, Comments = m.Comments.Trim() });
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = "Thank you. Your feedback was submitted.";
        return RedirectToAction(nameof(Feedback));
    }

    // Fills the order and connection lists of the feedback form.
    private async Task<IActionResult> FeedbackForm(FeedbackFormViewModel m)
    {
        int id = CustomerId();
        m.Orders = await db.Orders.Where(o => o.CustomerId == id).OrderByDescending(o => o.PlacedAtUtc)
            .Select(o => new SelectListItem(o.OrderNumber, o.Id.ToString())).ToListAsync();
        m.Connections = await db.Connections.Where(c => c.CustomerId == id).OrderBy(c => c.AccountId)
            .Select(c => new SelectListItem(c.AccountId, c.Id.ToString())).ToListAsync();
        return View(m);
    }

    // The signed-in customer's record.
    private IQueryable<Models.Entities.Customer> OwnCustomer() => db.Customers.Where(c => c.Id == CustomerId());

    // Customer id stored in the login cookie.
    private int CustomerId() => int.Parse(User.FindFirst(RoleNames.CustomerIdClaim)!.Value);
}

/// <summary>Editable contact details of the signed-in customer (the city is shown but not changed here).</summary>
public class ProfileForm
{
    [Required, StringLength(150)] public string FullName { get; set; } = string.Empty;
    [EmailAddress, StringLength(256)] public string? Email { get; set; }
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = string.Empty;
    [Required, StringLength(250)] public string AddressLine { get; set; } = string.Empty;
    [StringLength(20)] public string? PostalCode { get; set; }
    public string CityName { get; set; } = string.Empty;
}

/// <summary>One row of "My connections": the connection and the amount still due on its bills.</summary>
public class MyConnectionItem
{
    public Connection Connection { get; set; } = null!;
    public decimal Due { get; set; }
}
