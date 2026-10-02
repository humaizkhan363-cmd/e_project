using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Security;

namespace NexusServiceMarketingSystem.Data.Seed
{
    /// <summary>
    /// Ensures exactly one Admin login exists so the application is usable immediately after
    /// migrations are applied. Runs once at application startup (see Program.cs) and is idempotent:
    /// it does nothing if an "admin" user already exists. This is deliberately NOT part of
    /// SeedData/HasData, because a real password hash needs the password hasher service and a
    /// fresh salt each time it is generated, which HasData (baked into a migration) cannot do.
    ///
    /// Default login: username "admin", password "Admin@123".
    /// CHANGE THIS PASSWORD after first login in a real deployment.
    /// </summary>
    public static class RuntimeAdminSeeder
    {
        public const string DefaultAdminUsername = "admin";
        private const string DefaultAdminPassword = "Admin@123";

        public static async Task SeedAsync(AppDbContext db, IPasswordHasherService passwordHasher, CancellationToken cancellationToken = default)
        {
            bool adminExists = await db.Users.AnyAsync(u => u.Username == DefaultAdminUsername, cancellationToken);
            if (adminExists)
            {
                return;
            }

            var employee = new Employee
            {
                FullName = "System Administrator",
                Email = "admin@nexus.local",
                Phone = "0000000000",
                Role = EmployeeRole.Admin,
                RetailShopId = null,
                IsActive = true
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync(cancellationToken); // need employee.Id before creating the User row

            var user = new User
            {
                Username = DefaultAdminUsername,
                EmployeeId = employee.Id,
                IsActive = true
            };
            user.PasswordHash = passwordHasher.HashPassword(user, DefaultAdminPassword);

            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
