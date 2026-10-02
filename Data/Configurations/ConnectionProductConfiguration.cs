using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: ConnectionProducts (equipment issued to connections).</summary>
    public class ConnectionProductConfiguration : IEntityTypeConfiguration<ConnectionProduct>
    {
        public void Configure(EntityTypeBuilder<ConnectionProduct> builder)
        {
            builder.ToTable("ConnectionProducts", t =>
            {
                t.HasCheckConstraint("CK_ConnectionProducts_Quantity", "[Quantity] >= 1");
                t.HasCheckConstraint("CK_ConnectionProducts_Returned", "[ReturnedAtUtc] IS NULL OR [ReturnedAtUtc] >= [IssuedAtUtc]");
                t.HasCheckConstraint("CK_ConnectionProducts_ReplacementCharge", "[ReplacementChargeAmount] >= 0");
            });

            builder.HasKey(cp => cp.Id);

            builder.Property(cp => cp.Quantity).IsRequired();
            builder.Property(cp => cp.SerialNumber).HasMaxLength(60);
            builder.Property(cp => cp.IsReplacement).IsRequired();
            builder.Property(cp => cp.IssuedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(cp => cp.Notes).HasMaxLength(500);
            builder.Property(cp => cp.ReplacementChargeAmount).HasPrecision(18, 2);

            // Set when Accounts bills the replacement charge, so it is billed exactly once.
            builder.HasOne(cp => cp.BilledOnBill)
                   .WithMany()
                   .HasForeignKey(cp => cp.BilledOnBillId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cp => cp.Connection)
                   .WithMany(c => c.ConnectionProducts)
                   .HasForeignKey(cp => cp.ConnectionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cp => cp.Product)
                   .WithMany(p => p.ConnectionProducts)
                   .HasForeignKey(cp => cp.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            // A physical device (product + serial number) can only be issued once at a time.
            // Rows without a serial number are not constrained.
            builder.HasIndex(cp => new { cp.ProductId, cp.SerialNumber })
                   .IsUnique()
                   .HasFilter("[SerialNumber] IS NOT NULL")
                   .HasDatabaseName("UX_ConnectionProducts_Product_Serial");
        }
    }
}
