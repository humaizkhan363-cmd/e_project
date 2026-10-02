using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Areas.Admin.Models;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Security;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public class EmployeesController : Controller
    {
        private static readonly EmployeeRole[] AssignableRoles = { EmployeeRole.RetailStaff, EmployeeRole.Technical, EmployeeRole.Accounts };
        private readonly AppDbContext _db;
        private readonly IPasswordHasherService _passwordHasher;

        public EmployeesController(AppDbContext db, IPasswordHasherService passwordHasher) { _db = db; _passwordHasher = passwordHasher; }

        public async Task<IActionResult> Index()
        {
            var employees = await _db.Employees.Include(e => e.RetailShop).ThenInclude(s => s!.City).Include(e => e.User)
                .OrderBy(e => e.FullName).ToListAsync();
            return View(employees);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new EmployeeFormViewModel();
            await PopulateLists(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeFormViewModel model)
        {
            ValidateRoleAndShop(model.Role, model.RetailShopId);
            await ValidateShopAssignment(model.Role, model.RetailShopId);
            await ValidateEmployee(model.Email, model.Username);
            if (!ModelState.IsValid) { await PopulateLists(model); return View(model); }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            var employee = new Employee { FullName = model.FullName.Trim(), Email = model.Email.Trim(), Phone = model.Phone.Trim(), Role = model.Role!.Value, RetailShopId = model.Role == EmployeeRole.RetailStaff ? model.RetailShopId : null, IsActive = model.IsActive };
            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();

            var user = new User { Username = model.Username.Trim(), IsActive = model.IsActive, EmployeeId = employee.Id };
            user.PasswordHash = _passwordHasher.HashPassword(user, model.InitialPassword);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["StatusMessage"] = $"Employee '{employee.FullName}' and login '{user.Username}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _db.Employees.Include(e => e.RetailShop).ThenInclude(s => s!.City).Include(e => e.User).SingleOrDefaultAsync(e => e.Id == id);
            return employee is null ? NotFound() : View(employee);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _db.Employees.Include(e => e.User).SingleOrDefaultAsync(e => e.Id == id);
            if (employee is null || employee.User is null) return NotFound();
            var model = new EmployeeEditFormViewModel { Id = employee.Id, FullName = employee.FullName, Email = employee.Email, Phone = employee.Phone, Role = employee.Role, RetailShopId = employee.RetailShopId, IsActive = employee.IsActive, Username = employee.User.Username };
            await PopulateLists(model, employee.RetailShopId);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeEditFormViewModel model)
        {
            if (id != model.Id) return BadRequest();
            var employee = await _db.Employees.Include(e => e.User).SingleOrDefaultAsync(e => e.Id == id);
            if (employee is null || employee.User is null) return NotFound();
            ValidateRoleAndShop(model.Role, model.RetailShopId);
            await ValidateShopAssignment(model.Role, model.RetailShopId, employee.RetailShopId);
            await ValidateEmployee(model.Email, model.Username, employee.Id, employee.User.Id);
            if (!ModelState.IsValid) { await PopulateLists(model, employee.RetailShopId); return View(model); }

            employee.FullName = model.FullName.Trim(); employee.Email = model.Email.Trim(); employee.Phone = model.Phone.Trim();
            employee.Role = model.Role!.Value; employee.RetailShopId = model.Role == EmployeeRole.RetailStaff ? model.RetailShopId : null; employee.IsActive = model.IsActive;
            employee.User.Username = model.Username.Trim();
            employee.User.IsActive = model.IsActive;
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = $"Employee '{employee.FullName}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var employee = await _db.Employees.Include(e => e.User).SingleOrDefaultAsync(e => e.Id == id);
            if (employee is null || employee.User is null) return NotFound();
            employee.IsActive = !employee.IsActive;
            employee.User.IsActive = employee.IsActive;
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = $"Employee '{employee.FullName}' {(employee.IsActive ? "activated" : "deactivated")}; login access {(employee.IsActive ? "enabled" : "disabled")}.";
            return RedirectToAction(nameof(Index));
        }

        private void ValidateRoleAndShop(EmployeeRole? role, int? shopId)
        {
            if (role is null || !AssignableRoles.Contains(role.Value))
                ModelState.AddModelError(nameof(EmployeeFormViewModel.Role), "Choose Retail Employee, Technical Employee, or Accounts Employee.");
            if (role == EmployeeRole.RetailStaff && shopId is null)
                ModelState.AddModelError(nameof(EmployeeFormViewModel.RetailShopId), "A retail employee must be assigned to a shop.");
            if (role != EmployeeRole.RetailStaff && shopId is not null)
                ModelState.AddModelError(nameof(EmployeeFormViewModel.RetailShopId), "Only retail employees can be assigned to a shop.");
        }

        private async Task ValidateEmployee(string email, string username, int? employeeId = null, int? userId = null)
        {
            string normalizedEmail = email.Trim(); string normalizedUsername = username.Trim();
            if (await _db.Employees.AnyAsync(e => e.Email == normalizedEmail && e.Id != employeeId))
                ModelState.AddModelError(nameof(EmployeeFormViewModel.Email), "An employee with this email already exists.");
            if (await _db.Users.AnyAsync(u => u.Username == normalizedUsername && u.Id != userId))
                ModelState.AddModelError(nameof(EmployeeFormViewModel.Username), "This username is already in use.");
        }

        private async Task ValidateShopAssignment(EmployeeRole? role, int? shopId, int? currentShopId = null)
        {
            if (role == EmployeeRole.RetailStaff && shopId is int id &&
                !await _db.RetailShops.AnyAsync(s => s.Id == id && (s.IsActive || s.Id == currentShopId)))
                ModelState.AddModelError(nameof(EmployeeFormViewModel.RetailShopId), "Select an active retail shop.");
        }

        private async Task PopulateLists(EmployeeFormViewModel model)
        {
            model.Shops = await ShopOptions(model.RetailShopId);
            model.Roles = RoleOptions(model.Role);
        }

        private async Task PopulateLists(EmployeeEditFormViewModel model, int? includeShopId = null)
        {
            model.Shops = await ShopOptions(model.RetailShopId ?? includeShopId);
            model.Roles = RoleOptions(model.Role);
        }

        private async Task<List<SelectListItem>> ShopOptions(int? includeId)
        {
            return await _db.RetailShops.Where(s => s.IsActive || s.Id == includeId).OrderBy(s => s.Name)
                .Select(s => new SelectListItem(s.Name + " — " + s.City.Name, s.Id.ToString())).ToListAsync();
        }

        private static List<SelectListItem> RoleOptions(EmployeeRole? selected) => AssignableRoles
            .Select(role => new SelectListItem(role switch { EmployeeRole.RetailStaff => "Retail Employee", EmployeeRole.Technical => "Technical Employee", _ => "Accounts Employee" }, role.ToString(), selected == role)).ToList();
    }
}
