using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Data.Configurations;

/// <summary>Table: ProductPurchases (equipment bought from vendors and the amount paid to them).</summary>
public class ProductPurchaseConfiguration : IEntityTypeConfiguration<ProductPurchase>
{
    public void Configure(EntityTypeBuilder<ProductPurchase> b)
    {
        b.ToTable("ProductPurchases", t => t.HasCheckConstraint("CK_ProductPurchases_Positive", "[Quantity] > 0 AND [UnitPrice] >= 0 AND [AmountPaid] >= 0 AND [AmountPaid] <= [Quantity] * [UnitPrice]"));
        b.HasKey(x => x.Id);
        b.Property(x => x.UnitPrice).HasPrecision(18,2);
        b.Property(x => x.AmountPaid).HasPrecision(18,2);
        b.Property(x => x.SupplierReference).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.RecordedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        b.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RecordedByEmployee).WithMany().HasForeignKey(x => x.RecordedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.PurchaseDate);
    }
}
