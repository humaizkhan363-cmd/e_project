using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Data
{
    /// <summary>
    /// The Entity Framework Core database context for the Nexus Service Marketing System (SQL Server).
    /// The schema is defined by the classes in <c>Data/Configurations</c> and changed only through
    /// migrations (never EnsureCreated).
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // ---- Security ----
        public DbSet<User> Users => Set<User>();

        // ---- People and places ----
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<City> Cities => Set<City>();
        public DbSet<RetailShop> RetailShops => Set<RetailShop>();

        // ---- Suppliers, equipment and the plan catalogue ----
        public DbSet<Vendor> Vendors => Set<Vendor>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductPurchase> ProductPurchases => Set<ProductPurchase>();
        public DbSet<Plan> Plans => Set<Plan>();
        public DbSet<DiscountScheme> DiscountSchemes => Set<DiscountScheme>();

        // ---- Orders and connections ----
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<FeasibilityCheck> FeasibilityChecks => Set<FeasibilityCheck>();
        public DbSet<Connection> Connections => Set<Connection>();
        public DbSet<ConnectionProduct> ConnectionProducts => Set<ConnectionProduct>();

        // ---- Billing and feedback ----
        public DbSet<Bill> Bills => Set<Bill>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();
        public DbSet<CustomerDocument> CustomerDocuments => Set<CustomerDocument>();

        // ---- Infrastructure ----
        /// <summary>Serial counters behind the order-number and account-ID generators. Touched only by IdentifierGenerator.</summary>
        public DbSet<IdentifierCounter> IdentifierCounters => Set<IdentifierCounter>();

        /// <summary>Applies every IEntityTypeConfiguration class found in this assembly (keys, FKs, indexes, checks, seed data).</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        /// <summary>Default for every money column: decimal(18,2). Columns needing another precision override it explicitly.</summary>
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        }
    }
}
