using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: RetailShops.</summary>
    public class RetailShopConfiguration : IEntityTypeConfiguration<RetailShop>
    {
        public void Configure(EntityTypeBuilder<RetailShop> builder)
        {
            builder.ToTable("RetailShops");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
            builder.Property(s => s.AddressLine).IsRequired().HasMaxLength(300);
            builder.Property(s => s.Phone).HasMaxLength(20);
            builder.Property(s => s.IsActive).IsRequired();

            // A shop belongs to one city; a city cannot be deleted while it still has shops.
            builder.HasOne(s => s.City)
                   .WithMany(c => c.RetailShops)
                   .HasForeignKey(s => s.CityId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Two shops in the same city cannot share a name.
            builder.HasIndex(s => new { s.CityId, s.Name }).IsUnique().HasDatabaseName("UX_RetailShops_City_Name");
        }
    }
}
