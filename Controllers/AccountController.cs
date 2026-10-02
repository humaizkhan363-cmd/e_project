using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Account;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Models.Workflows;
using NexusServiceMarketingSystem.Services.Security;
using NexusServiceMarketingSystem.Services.Workflows;

namespace NexusServiceMarketingSystem.Controllers
{
    /// <summary>
    /// Handles login/logout for both staff (Employee) and customer logins that share the single
    /// Users table. The role that ends up in the auth cookie is the Employee's Role for staff,
    /// or the fixed string "Customer" for a customer login (see <see cref="RoleNames"/>).
    /// </summary>
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly ICustomerAccountService _customerAccounts;

        public AccountController(AppDbContext db, IPasswordHasherService passwordHasher, ICustomerAccountService customerAccounts)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _customerAccounts = customerAccounts;
        }

        // Login form.
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard();
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // Customer self-registration form.
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToDashboard();
            var model = new CustomerRegistrationViewModel();
            await PopulateCities(model);
            return View(model);
        }

        // Creates the customer and login, then signs the customer in.
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(CustomerRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCities(model);
                return View(model);
            }
            try
            {
                await _customerAccounts.CreateAsync(model);
                TempData["StatusMessage"] = "Customer account created. Sign in to place and track orders.";
                return RedirectToAction(nameof(Login));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateCities(model);
                return View(model);
            }
        }

        // Checks the username and password and issues the auth cookie with the user's role.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Username lookup is intentionally exact (the unique index is case-sensitive under the
            // database's default collation choice); the error message never reveals which part was wrong.
            var user = await _db.Users
                .Include(u => u.Employee)
                .Include(u => u.Customer)
                .SingleOrDefaultAsync(u => u.Username == model.Username);

            bool rehashNeeded = false;
            bool ok = user is not null && user.IsActive
                && _passwordHasher.VerifyPassword(user, user.PasswordHash, model.Password, out rehashNeeded);

            if (!ok || user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            if (rehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            }

            user.LastLoginAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            ClaimsPrincipal principal = BuildPrincipal(user);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

            if (Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl!);
            }

            return RedirectToDashboard();
        }

        // Signs the user out.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // Shown when a signed-in user opens a page of another role.
        [HttpGet]
        public IActionResult AccessDenied() => View();

        // Change password form.
        [HttpGet, Authorize]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        // Verifies the current password and stores the new hash.
        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId)) return Challenge();
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user is null) return Challenge();
            if (!_passwordHasher.VerifyPassword(user, user.PasswordHash, model.CurrentPassword, out _))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "The current password is incorrect.");
                return View(model);
            }
            user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Your password has been changed.";
            return RedirectToAction(nameof(ChangePassword));
        }

        // ---------------------------------------------------------------- helpers
        private static ClaimsPrincipal BuildPrincipal(Models.Entities.User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username)
            };

            if (user.Employee is not null)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Employee.Role.ToString()));
                claims.Add(new Claim(RoleNames.EmployeeIdClaim, user.Employee.Id.ToString()));
                claims.Add(new Claim(RoleNames.DisplayNameClaim, user.Employee.FullName));
            }
            else if (user.Customer is not null)
            {
                claims.Add(new Claim(ClaimTypes.Role, RoleNames.Customer));
                claims.Add(new Claim(RoleNames.CustomerIdClaim, user.Customer.Id.ToString()));
                claims.Add(new Claim(RoleNames.DisplayNameClaim, user.Customer.FullName));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        /// <summary>Sends a freshly signed-in user to the home page of their role's area.</summary>
        private IActionResult RedirectToDashboard()
        {
            string? role = User.FindFirstValue(ClaimTypes.Role);
            string area = role switch
            {
                nameof(EmployeeRole.Admin) => "Admin",
                nameof(EmployeeRole.RetailStaff) => "Retail",
                nameof(EmployeeRole.Technical) => "Technical",
                nameof(EmployeeRole.Accounts) => "Accounts",
                RoleNames.Customer => "Customer",
                _ => ""
            };

            if (string.IsNullOrEmpty(area))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            return RedirectToAction("Index", "Home", new { area });
        }

        // Active cities for the registration form.
        private async Task PopulateCities(CustomerRegistrationViewModel model)
        {
            model.Cities = await _db.Cities.Where(c => c.IsActive).OrderBy(c => c.Name)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(c.Name, c.Id.ToString())).ToListAsync();
        }
    }
}
