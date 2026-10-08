using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: Employees.</summary>
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employees", t =>
            {
                t.HasCheckConstraint("CK_Employees_Role_Values", ConfigurationHelpers.EnumInList<EmployeeRole>("Role"));

                // Retail staff must work at a shop; every other role must not be tied to one.
                t.HasCheckConstraint("CK_Employees_Role_Shop",
                    "([Role] = 'RetailStaff' AND [RetailShopId] IS NOT NULL) OR ([Role] <> 'RetailStaff' AND [RetailShopId] IS NULL)");
            });

            builder.HasKey(e => e.Id);

            builder.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
            builder.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            builder.Property(e => e.Role).IsRequired().HasStringConversion();
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasOne(e => e.RetailShop)
                   .WithMany(s => s.Employees)
                   .HasForeignKey(e => e.RetailShopId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UX_Employees_Email");
        }
    }
}
