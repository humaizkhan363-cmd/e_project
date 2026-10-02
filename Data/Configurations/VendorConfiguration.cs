using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: Vendors.</summary>
    public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
    {
        public void Configure(EntityTypeBuilder<Vendor> builder)
        {
            builder.ToTable("Vendors");
            builder.HasKey(v => v.Id);

            builder.Property(v => v.Name).IsRequired().HasMaxLength(150);
            builder.Property(v => v.ContactPerson).HasMaxLength(100);
            builder.Property(v => v.Email).HasMaxLength(256);
            builder.Property(v => v.Phone).IsRequired().HasMaxLength(20);
            builder.Property(v => v.AddressLine).HasMaxLength(300);
            builder.Property(v => v.IsActive).IsRequired();
            builder.Property(v => v.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(v => v.Name).IsUnique().HasDatabaseName("UX_Vendors_Name");
        }
    }
}
