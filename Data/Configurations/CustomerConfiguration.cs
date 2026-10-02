using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: Customers.</summary>
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers", t =>
            {
                t.HasCheckConstraint("CK_Customers_CustomerType_Values", ConfigurationHelpers.EnumInList<CustomerType>("CustomerType"));

                // A corporate customer must have a company name.
                t.HasCheckConstraint("CK_Customers_Corporate_Company",
                    "[CustomerType] <> 'Corporate' OR [CompanyName] IS NOT NULL");
            });

            builder.HasKey(c => c.Id);

            builder.Property(c => c.FullName).IsRequired().HasMaxLength(150);
            builder.Property(c => c.CustomerType).IsRequired().HasStringConversion();
            builder.Property(c => c.CompanyName).HasMaxLength(150);
            builder.Property(c => c.Email).HasMaxLength(256);
            builder.Property(c => c.Phone).IsRequired().HasMaxLength(20);
            builder.Property(c => c.AddressLine).IsRequired().HasMaxLength(300);
            builder.Property(c => c.PostalCode).HasMaxLength(20);
            builder.Property(c => c.IsActive).IsRequired();
            builder.Property(c => c.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasOne(c => c.City)
                   .WithMany(city => city.Customers)
                   .HasForeignKey(c => c.CityId)
                   .OnDelete(DeleteBehavior.Restrict);

            // An e-mail address, when given, identifies one customer (rows without e-mail are not constrained).
            builder.HasIndex(c => c.Email).IsUnique().HasFilter("[Email] IS NOT NULL").HasDatabaseName("UX_Customers_Email");

            // Advanced search: by customer name and by the contact number given when applying.
            builder.HasIndex(c => c.FullName).HasDatabaseName("IX_Customers_FullName");
            builder.HasIndex(c => c.Phone).HasDatabaseName("IX_Customers_Phone");
        }
    }
}
