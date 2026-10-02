using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Models.Workflows;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

[Area("Admin"),Authorize(Roles=nameof(EmployeeRole.Admin))]
public class OperationsController(AppDbContext db):Controller
{
    public async Task<IActionResult> Orders(string? term,OrderStatus? status,DateTime? from,DateTime? through)
    {
        var q=db.Orders.AsNoTracking().Include(o=>o.Customer).Include(o=>o.Plan).Include(o=>o.City).AsQueryable();
        if(!string.IsNullOrWhiteSpace(term)){term=term.Trim();q=q.Where(o=>o.OrderNumber.Contains(term)||o.Customer.FullName.Contains(term)||o.Customer.Phone.Contains(term)||(o.Customer.Email!=null&&o.Customer.Email.Contains(term)));}
        if(status.HasValue)q=q.Where(o=>o.Status==status);if(from.HasValue)q=q.Where(o=>o.PlacedAtUtc>=from.Value.Date);if(through.HasValue)q=q.Where(o=>o.PlacedAtUtc<through.Value.Date.AddDays(1));
        ViewBag.Term=term;ViewBag.Status=status;ViewBag.From=from?.ToString("yyyy-MM-dd");ViewBag.Through=through?.ToString("yyyy-MM-dd");return View(await q.OrderByDescending(o=>o.PlacedAtUtc).Take(500).ToListAsync());
    }
    public async Task<IActionResult> Search(AdvancedSearchViewModel m)
    {
        var oq=db.Orders.AsNoTracking().Include(o=>o.Customer).Include(o=>o.Plan).AsQueryable();
        if(!string.IsNullOrWhiteSpace(m.OrderNumber))oq=oq.Where(o=>o.OrderNumber.Contains(m.OrderNumber));
        if(!string.IsNullOrWhiteSpace(m.Customer))oq=oq.Where(o=>o.Customer.FullName.Contains(m.Customer)||o.Customer.Phone.Contains(m.Customer)||(o.Customer.Email!=null&&o.Customer.Email.Contains(m.Customer)));
        if(m.ConnectionType.HasValue)oq=oq.Where(o=>o.ConnectionType==m.ConnectionType);if(m.OrderStatus.HasValue)oq=oq.Where(o=>o.Status==m.OrderStatus);if(m.From.HasValue)oq=oq.Where(o=>o.PlacedAtUtc>=m.From.Value.Date);if(m.Through.HasValue)oq=oq.Where(o=>o.PlacedAtUtc<m.Through.Value.Date.AddDays(1));
        m.Orders=await oq.OrderByDescending(o=>o.PlacedAtUtc).Take(200).ToListAsync();
        var cq=db.Connections.AsNoTracking().Include(c=>c.Customer).Include(c=>c.Plan).AsQueryable();
        if(!string.IsNullOrWhiteSpace(m.AccountId))cq=cq.Where(c=>c.AccountId.Contains(m.AccountId));if(!string.IsNullOrWhiteSpace(m.Customer))cq=cq.Where(c=>c.Customer.FullName.Contains(m.Customer)||c.Customer.Phone.Contains(m.Customer)||(c.Customer.Email!=null&&c.Customer.Email.Contains(m.Customer)));if(m.ConnectionType.HasValue)cq=cq.Where(c=>c.ConnectionType==m.ConnectionType);if(m.ConnectionStatus.HasValue)cq=cq.Where(c=>c.Status==m.ConnectionStatus);if(m.From.HasValue)cq=cq.Where(c=>c.ActivatedAtUtc>=m.From.Value.Date);if(m.Through.HasValue)cq=cq.Where(c=>c.ActivatedAtUtc<m.Through.Value.Date.AddDays(1));
        m.Connections=await cq.OrderBy(c=>c.AccountId).Take(200).ToListAsync();return View(m);
    }
    public async Task<IActionResult> Reports()
    {
        var m=new AdminReportsViewModel{Customers=await db.Customers.CountAsync(),ActiveCustomers=await db.Customers.CountAsync(c=>c.IsActive),Employees=await db.Employees.CountAsync(),RetailShops=await db.RetailShops.CountAsync(),Plans=await db.Plans.CountAsync(),Products=await db.Products.CountAsync(),LowStockProducts=await db.Products.CountAsync(p=>p.StockQuantity<=p.ReorderLevel),Vendors=await db.Vendors.CountAsync(),Orders=await db.Orders.CountAsync(),PendingOrders=await db.Orders.CountAsync(o=>o.Status==OrderStatus.Placed||o.Status==OrderStatus.UnderFeasibilityCheck),FeasibleOrders=await db.Orders.CountAsync(o=>o.Status==OrderStatus.Feasible),NotFeasibleOrders=await db.Orders.CountAsync(o=>o.Status==OrderStatus.NotFeasible),ConnectedOrders=await db.Orders.CountAsync(o=>o.Status==OrderStatus.Connected),Connections=await db.Connections.CountAsync(),ActiveConnections=await db.Connections.CountAsync(c=>c.Status==ConnectionStatus.Active),TemporarilyInactiveConnections=await db.Connections.CountAsync(c=>c.Status==ConnectionStatus.TemporarilyInactive),PermanentlyInactiveConnections=await db.Connections.CountAsync(c=>c.Status==ConnectionStatus.PermanentlyInactive),Bills=await db.Bills.CountAsync(),TotalBilled=await db.Bills.Where(b=>b.Status!=BillStatus.Cancelled).SumAsync(b=>(decimal?)b.TotalAmount)??0,TotalPaid=await db.Payments.SumAsync(p=>(decimal?)p.Amount)??0,Payments=await db.Payments.CountAsync(),FeedbackItems=await db.Feedbacks.CountAsync(),AverageRating=await db.Feedbacks.AverageAsync(f=>(decimal?)f.Rating)??0};
        m.Outstanding=m.TotalBilled-m.TotalPaid;m.OverdueBills=await db.Bills.CountAsync(b=>b.DueDate<DateOnly.FromDateTime(DateTime.Today)&&b.Status!=BillStatus.Paid&&b.Status!=BillStatus.Cancelled);m.EquipmentRequired=await db.Orders.Where(o=>(o.ConnectionType==ConnectionType.DialUp||o.ConnectionType==ConnectionType.Broadband)&&(o.Status==OrderStatus.Placed||o.Status==OrderStatus.UnderFeasibilityCheck||o.Status==OrderStatus.Feasible)).SumAsync(o=>(int?)o.Quantity)??0;
        m.EquipmentInStock=await db.Products.Where(p=>p.IsActive&&p.Category!=ProductCategory.Other).SumAsync(p=>(int?)p.StockQuantity)??0;
        m.EquipmentShortfall=Math.Max(0,m.EquipmentRequired-m.EquipmentInStock);
        return View(m);
    }
    public async Task<IActionResult> Feedback(){return View(await db.Feedbacks.AsNoTracking().Include(f=>f.Customer).Include(f=>f.Order).OrderByDescending(f=>f.SubmittedAtUtc).Take(500).ToListAsync());}
}
