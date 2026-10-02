using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: Payments.</summary>
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments", t =>
            {
                t.HasCheckConstraint("CK_Payments_Method_Values", ConfigurationHelpers.EnumInList<PaymentMethod>("Method"));
                t.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0");
            });

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount).HasPrecision(18, 2);
            builder.Property(p => p.Method).IsRequired().HasStringConversion();
            builder.Property(p => p.Reference).HasMaxLength(100);
            builder.Property(p => p.PaidAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasOne(p => p.Bill)
                   .WithMany(b => b.Payments)
                   .HasForeignKey(p => p.BillId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ReceivedByEmployee)
                   .WithMany()
                   .HasForeignKey(p => p.ReceivedByEmployeeId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Optional: null means the payment was made at the office rather than a shop.
            builder.HasOne(p => p.RetailShop)
                   .WithMany()
                   .HasForeignKey(p => p.RetailShopId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.PaidAtUtc).HasDatabaseName("IX_Payments_PaidAtUtc");
        }
    }
}
