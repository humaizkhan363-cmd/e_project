using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Workflows;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

[Area("Customer"), Authorize(Roles = RoleNames.Customer)]
public class PortalController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var c = await OwnCustomer().Include(x => x.City).SingleOrDefaultAsync(); if (c is null) return NotFound();
        return View(new ProfileForm { FullName=c.FullName, Email=c.Email, Phone=c.Phone, AddressLine=c.AddressLine, PostalCode=c.PostalCode, CityName=c.City.Name });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileForm m)
    {
        if (!ModelState.IsValid) return View(m);
        var c=await OwnCustomer().SingleOrDefaultAsync(); if(c is null)return NotFound();
        if(!string.IsNullOrWhiteSpace(m.Email) && await db.Customers.AnyAsync(x=>x.Id!=c.Id && x.Email==m.Email.Trim())){ModelState.AddModelError(nameof(m.Email),"That email is already used.");return View(m);}
        c.FullName=m.FullName.Trim(); c.Email=m.Email?.Trim(); c.Phone=m.Phone.Trim(); c.AddressLine=m.AddressLine.Trim(); c.PostalCode=m.PostalCode?.Trim();
        await db.SaveChangesAsync(); TempData["StatusMessage"]="Profile updated."; return RedirectToAction(nameof(Profile));
    }
    public async Task<IActionResult> Bills()
    {
        int id=CustomerId(); var bills=await db.Bills.AsNoTracking().Include(b=>b.Connection).Include(b=>b.Payments).Where(b=>b.Connection.CustomerId==id).OrderByDescending(b=>b.IssueDate).ToListAsync();
        return View(bills.Select(b=>new BillListItemViewModel { Bill=b, AmountPaid=b.Payments.Sum(p=>p.Amount), Outstanding=b.TotalAmount-b.Payments.Sum(p=>p.Amount), DisplayStatus=b.TotalAmount<=b.Payments.Sum(p=>p.Amount)?"Paid":b.DueDate<DateOnly.FromDateTime(DateTime.Today)?"Overdue":b.Payments.Any()?"Partially paid":"Unpaid" }).ToList());
    }
    // The customer's connections with status and balance (account ID, status and dues in one place).
    public async Task<IActionResult> Connections()
    {
        int id=CustomerId();
        var connections=await db.Connections.AsNoTracking().Include(c=>c.Plan).Include(c=>c.City).Where(c=>c.CustomerId==id).OrderBy(c=>c.AccountId).ToListAsync();
        var bills=await db.Bills.AsNoTracking().Where(b=>b.Connection.CustomerId==id && b.Status!=Models.Enums.BillStatus.Cancelled)
            .Select(b=>new{b.ConnectionId,b.TotalAmount,Paid=b.Payments.Sum(p=>(decimal?)p.Amount)??0m}).ToListAsync();
        return View(connections.Select(c=>new MyConnectionItem{Connection=c,Due=bills.Where(b=>b.ConnectionId==c.Id).Sum(b=>b.TotalAmount-b.Paid)}).ToList());
    }
    [HttpGet]
    public async Task<IActionResult> Feedback()
    {
        int id=CustomerId(); var m=new FeedbackFormViewModel();
        m.Orders=await db.Orders.Where(o=>o.CustomerId==id).OrderByDescending(o=>o.PlacedAtUtc).Select(o=>new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(o.OrderNumber,o.Id.ToString())).ToListAsync();
        m.Connections=await db.Connections.Where(c=>c.CustomerId==id).OrderBy(c=>c.AccountId).Select(c=>new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(c.AccountId,c.Id.ToString())).ToListAsync(); return View(m);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(FeedbackFormViewModel m)
    {
        int id=CustomerId(); if(!ModelState.IsValid)return await FeedbackForm(m);
        if(m.OrderId.HasValue&&!await db.Orders.AnyAsync(o=>o.Id==m.OrderId&&o.CustomerId==id)){ModelState.AddModelError(nameof(m.OrderId),"Choose one of your own orders.");return await FeedbackForm(m);}
        if(m.ConnectionId.HasValue&&!await db.Connections.AnyAsync(c=>c.Id==m.ConnectionId&&c.CustomerId==id)){ModelState.AddModelError(nameof(m.ConnectionId),"Choose one of your own connections.");return await FeedbackForm(m);}
        db.Feedbacks.Add(new Feedback{CustomerId=id,OrderId=m.OrderId,ConnectionId=m.ConnectionId,Rating=m.Rating,Comments=m.Comments.Trim()}); await db.SaveChangesAsync(); TempData["StatusMessage"]="Thank you. Your feedback was submitted."; return RedirectToAction(nameof(Feedback));
    }
    private async Task<IActionResult> FeedbackForm(FeedbackFormViewModel m){int id=CustomerId();m.Orders=await db.Orders.Where(o=>o.CustomerId==id).Select(o=>new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(o.OrderNumber,o.Id.ToString())).ToListAsync();m.Connections=await db.Connections.Where(c=>c.CustomerId==id).Select(c=>new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(c.AccountId,c.Id.ToString())).ToListAsync();return View(m);}
    private IQueryable<NexusServiceMarketingSystem.Models.Entities.Customer> OwnCustomer()=>db.Customers.Where(c=>c.Id==CustomerId());
    private int CustomerId()=>int.Parse(User.FindFirst(RoleNames.CustomerIdClaim)!.Value);
}

public class ProfileForm
{
    [Required,StringLength(150)] public string FullName{get;set;}=string.Empty;
    [EmailAddress,StringLength(256)] public string? Email{get;set;}
    [Required,Phone,StringLength(30)] public string Phone{get;set;}=string.Empty;
    [Required,StringLength(250)] public string AddressLine{get;set;}=string.Empty;
    [StringLength(20)] public string? PostalCode{get;set;}
    public string CityName{get;set;}=string.Empty;
}

public class MyConnectionItem
{
    public Connection Connection { get; set; } = null!;
    public decimal Due { get; set; }
}
