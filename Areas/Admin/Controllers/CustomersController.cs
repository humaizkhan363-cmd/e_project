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

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = nameof(EmployeeRole.Admin))]
public class CustomersController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICustomerAccountService _customerAccounts;
    public CustomersController(AppDbContext db, ICustomerAccountService customerAccounts) { _db = db; _customerAccounts = customerAccounts; }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Customers.Include(c => c.City).Include(c => c.User).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(c => c.FullName.Contains(term) || c.Phone.Contains(term) || (c.Email != null && c.Email.Contains(term)) || c.Id.ToString() == term);
        }
        var customers = await query.OrderBy(c => c.FullName).Take(300).ToListAsync();
        ViewData["Search"] = search;
        return View(customers);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CustomerRegistrationViewModel();
        await PopulateCities(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerRegistrationViewModel model)
    {
        if (!ModelState.IsValid) { await PopulateCities(model); return View(model); }
        try
        {
            var (customer, _) = await _customerAccounts.CreateAsync(model);
            TempData["StatusMessage"] = $"Customer '{customer.FullName}' and login account created.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateCities(model);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _db.Customers.Include(c => c.City).Include(c => c.User)
            .Include(c => c.Orders).Include(c => c.Connections).SingleOrDefaultAsync(c => c.Id == id);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var customer = await _db.Customers.Include(c => c.User).SingleOrDefaultAsync(c => c.Id == id);
        if (customer is null) return NotFound();
        customer.IsActive = !customer.IsActive;
        if (customer.User is not null) customer.User.IsActive = customer.IsActive;
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = $"Customer '{customer.FullName}' {(customer.IsActive ? "activated" : "deactivated")}; login access {(customer.IsActive ? "enabled" : "disabled")}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCities(CustomerRegistrationViewModel model)
    {
        model.Cities = await _db.Cities.Where(c => c.IsActive).OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToListAsync();
    }
}
