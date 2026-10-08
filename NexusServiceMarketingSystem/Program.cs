using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Data.Seed;
using NexusServiceMarketingSystem.Services.Dashboard;
using NexusServiceMarketingSystem.Services.Identifiers;
using NexusServiceMarketingSystem.Services.Security;
using NexusServiceMarketingSystem.Services.Workflows;
using NexusServiceMarketingSystem.Services.Documents;

namespace NexusServiceMarketingSystem
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // The specification quotes every price in US dollars, so format money/dates with the en-US culture on every server.
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Database: Entity Framework Core with SQL Server. The schema is created by migrations only.
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in appsettings.json.");
            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

            // Generates order numbers and account IDs (users can never type these in).
            builder.Services.AddScoped<IIdentifierGenerator, IdentifierGenerator>();

            // Login/logout (cookie auth) and password hashing. Both ship inside the ASP.NET Core
            // shared framework already referenced by the Web SDK, so no extra package is needed.
            builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
            builder.Services.AddScoped<ICustomerAccountService, CustomerAccountService>();
            builder.Services.AddScoped<IOrderWorkflowService, OrderWorkflowService>();
            builder.Services.AddScoped<IConnectionProvisioningService, ConnectionProvisioningService>();
            builder.Services.AddScoped<IBillingService, BillingService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddSingleton<ICustomerDocumentStorage, CustomerDocumentStorage>();
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.SlidingExpiration = true;
                });
            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Applies checked-in EF migrations before seeding the local Admin login.
            using (IServiceScope scope = app.Services.CreateScope())
            {
                AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                IPasswordHasherService hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();
                await db.Database.MigrateAsync();
                await RuntimeAdminSeeder.SeedAsync(db, hasher);
                await RuntimeSampleDataSeeder.SeedAsync(db);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
