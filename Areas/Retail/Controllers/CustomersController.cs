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

namespace NexusServiceMarketingSystem.Areas.Retail.Controllers;

/// <summary>Retail employees register customers and see the customers served by their shop.</summary>
[Area("Retail"), Authorize(Roles = nameof(EmployeeRole.RetailStaff))]
public class CustomersController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICustomerAccountService _customerAccounts;
    public CustomersController(AppDbContext db, ICustomerAccountService customerAccounts) { _db = db; _customerAccounts = customerAccounts; }

    // Customers with an order or connection at this shop, optionally searched by name, phone or email.
    public async Task<IActionResult> Index(string? search)
    {
        int employeeId = int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
        int? shopId = await _db.Employees.Where(e => e.Id == employeeId && e.IsActive).Select(e => e.RetailShopId).SingleOrDefaultAsync();
        if (shopId is null) return Forbid();
        var query = _db.Customers.Include(c => c.City).Include(c => c.User)
            .Where(c => c.Orders.Any(o => o.RetailShopId == shopId) || c.Connections.Any(x => x.Order.RetailShopId == shopId)).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(c => c.FullName.Contains(term) || c.Phone.Contains(term) || (c.Email != null && c.Email.Contains(term)));
        }
        ViewData["Search"] = search;
        return View(await query.OrderBy(c => c.FullName).Take(200).ToListAsync());
    }

    // New customer form.
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CustomerRegistrationViewModel();
        await PopulateCities(model);
        return View(model);
    }

    // Creates the customer and their login.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerRegistrationViewModel model)
    {
        if (!ModelState.IsValid) { await PopulateCities(model); return View(model); }
        try
        {
            var (customer, _) = await _customerAccounts.CreateAsync(model);
            TempData["StatusMessage"] = $"Customer '{customer.FullName}' and login account created. The customer can sign in with the credentials you issued.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateCities(model);
            return View(model);
        }
    }

    // A customer of this shop with orders and connections.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        int employeeId = int.Parse(User.FindFirst(RoleNames.EmployeeIdClaim)!.Value);
        int? shopId = await _db.Employees.Where(e => e.Id == employeeId && e.IsActive).Select(e => e.RetailShopId).SingleOrDefaultAsync();
        var customer = await _db.Customers.Include(c => c.City).Include(c => c.Orders).Include(c => c.Connections)
            .SingleOrDefaultAsync(c => c.Id == id && (c.Orders.Any(o => o.RetailShopId == shopId) || c.Connections.Any(x => x.Order.RetailShopId == shopId)));
        ViewData["RetailShopId"] = shopId;
        return customer is null ? NotFound() : View(customer);
    }

    // Active cities for the drop-down list.
    private async Task PopulateCities(CustomerRegistrationViewModel model)
    {
        model.Cities = await _db.Cities.Where(c => c.IsActive).OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToListAsync();
    }
}
