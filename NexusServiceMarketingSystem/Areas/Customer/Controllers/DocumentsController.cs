using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Models.Workflows;
using NexusServiceMarketingSystem.Services.Documents;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

/// <summary>
/// Customer records filing (address proofs, application forms, receipts), filed by year and area (city).
/// Used by the customer, by Admin, and by retail staff for customers of their shop.
/// </summary>
[Area("Customer"), Authorize]
public class DocumentsController(AppDbContext db, ICustomerDocumentStorage storage) : Controller
{
    // Lists a customer's documents and shows the upload form.
    [HttpGet]
    public async Task<IActionResult> Index(int customerId)
    {
        if (!await CanAccess(customerId)) return Forbid();
        return View(await Build(customerId));
    }

    // Saves the uploaded file (outside wwwroot) and files it under the customer's city and the chosen year.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(CustomerDocumentFormViewModel m)
    {
        if (!await CanAccess(m.CustomerId)) return Forbid();
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == m.CustomerId);
        if (customer is null) return NotFound();
        if (!ModelState.IsValid) return View("Index", await Build(m.CustomerId, m));

        string? saved = null;
        try
        {
            var file = await storage.SaveAsync(m.File!);
            saved = file.StorageName;
            db.CustomerDocuments.Add(new CustomerDocument
            {
                CustomerId = customer.Id,
                CityId = customer.CityId,
                DocumentType = m.DocumentType!.Value,
                DocumentYear = m.DocumentYear,
                OriginalFileName = Path.GetFileName(m.File!.FileName),
                StorageName = file.StorageName,
                ContentType = file.ContentType,
                SizeBytes = file.SizeBytes,
                Notes = m.Notes?.Trim(),
                UploadedByUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
            });
            await db.SaveChangesAsync();
            TempData["StatusMessage"] = "Customer document saved.";
            return RedirectToAction(nameof(Index), new { customerId = m.CustomerId });
        }
        catch (InvalidOperationException ex)
        {
            // Rejected file (type / size): remove anything already written and show the message.
            if (saved is not null) storage.Delete(saved);
            ModelState.AddModelError(nameof(m.File), ex.Message);
            return View("Index", await Build(m.CustomerId, m));
        }
        catch
        {
            // Database failure: do not leave an orphan file behind.
            if (saved is not null) storage.Delete(saved);
            throw;
        }
    }

    // Streams a stored document to a user who may see that customer's records.
    public async Task<IActionResult> Download(int id)
    {
        var doc = await db.CustomerDocuments.Include(d => d.Customer).SingleOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();
        if (!await CanAccess(doc.CustomerId)) return Forbid();
        string path = storage.GetPath(doc.StorageName);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, doc.ContentType, doc.OriginalFileName);
    }

    // Builds the page model: the upload form plus the customer's documents ordered by year, then city.
    private async Task<CustomerDocumentFormViewModel> Build(int customerId, CustomerDocumentFormViewModel? model = null)
    {
        var c = await db.Customers.SingleOrDefaultAsync(x => x.Id == customerId)
            ?? throw new InvalidOperationException("Customer not found.");
        model ??= new CustomerDocumentFormViewModel { CustomerId = customerId, DocumentYear = DateTime.Today.Year };
        model.CustomerName = c.FullName;
        model.Documents = await db.CustomerDocuments.AsNoTracking().Where(d => d.CustomerId == customerId)
            .OrderByDescending(d => d.DocumentYear).ThenBy(d => d.City.Name).ThenByDescending(d => d.UploadedAtUtc)
            .Select(d => new SelectListItem(d.DocumentYear + " — " + d.City.Name + " — " + d.DocumentType + " — " + d.OriginalFileName, d.Id.ToString()))
            .ToListAsync();
        return model;
    }

    // Admin: any customer. Customer: only themself. Retail staff: customers with an order or connection of their shop.
    private async Task<bool> CanAccess(int customerId)
    {
        if (User.IsInRole(nameof(EmployeeRole.Admin)))
            return await db.Customers.AnyAsync(c => c.Id == customerId);
        if (User.IsInRole(RoleNames.Customer))
            return User.FindFirst(RoleNames.CustomerIdClaim)?.Value == customerId.ToString();
        if (!User.IsInRole(nameof(EmployeeRole.RetailStaff)) || !int.TryParse(User.FindFirst(RoleNames.EmployeeIdClaim)?.Value, out var employeeId))
            return false;

        var shop = await db.Employees.Where(e => e.Id == employeeId && e.IsActive && e.Role == EmployeeRole.RetailStaff)
            .Select(e => e.RetailShopId).SingleOrDefaultAsync();
        return shop.HasValue && await db.Customers.AnyAsync(c => c.Id == customerId
            && (c.Orders.Any(o => o.RetailShopId == shop) || c.Connections.Any(x => x.Order.RetailShopId == shop)));
    }
}
