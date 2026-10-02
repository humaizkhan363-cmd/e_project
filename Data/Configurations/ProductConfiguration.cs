using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: Products (equipment and its stock).</summary>
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products", t =>
            {
                t.HasCheckConstraint("CK_Products_Category_Values", ConfigurationHelpers.EnumInList<ProductCategory>("Category"));
                t.HasCheckConstraint("CK_Products_Stock_NonNegative", "[StockQuantity] >= 0 AND [ReorderLevel] >= 0");
                t.HasCheckConstraint("CK_Products_Prices_NonNegative", "[PurchasePrice] >= 0 AND [ReplacementCharge] >= 0");
            });

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Sku).IsRequired().HasMaxLength(40);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
            builder.Property(p => p.Category).IsRequired().HasStringConversion();
            builder.Property(p => p.Description).HasMaxLength(500);
            builder.Property(p => p.PurchasePrice).HasPrecision(18, 2);
            builder.Property(p => p.ReplacementCharge).HasPrecision(18, 2);
            builder.Property(p => p.IsActive).IsRequired();
            builder.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(p => p.RowVersion).IsRowVersion();

            builder.HasOne(p => p.Vendor)
                   .WithMany(v => v.Products)
                   .HasForeignKey(p => p.VendorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("UX_Products_Sku");
            builder.HasIndex(p => p.Name).HasDatabaseName("IX_Products_Name");
        }
    }
}
