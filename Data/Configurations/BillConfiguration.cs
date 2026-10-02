using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: Bills.</summary>
    public class BillConfiguration : IEntityTypeConfiguration<Bill>
    {
        public void Configure(EntityTypeBuilder<Bill> builder)
        {
            builder.ToTable("Bills", t =>
            {
                t.HasCheckConstraint("CK_Bills_Status_Values", ConfigurationHelpers.EnumInList<BillStatus>("Status"));
                t.HasCheckConstraint("CK_Bills_Period", "[PeriodEnd] >= [PeriodStart]");
                t.HasCheckConstraint("CK_Bills_DueDate", "[DueDate] >= [IssueDate]");
                t.HasCheckConstraint("CK_Bills_Amounts_NonNegative",
                    "[PlanCharge] >= 0 AND [UsageCharge] >= 0 AND [SecurityDepositCharge] >= 0 AND [ReplacementCharge] >= 0 " +
                    "AND [DiscountAmount] >= 0 AND [SubTotal] >= 0 AND [ServiceTaxAmount] >= 0 AND [TotalAmount] >= 0");
                t.HasCheckConstraint("CK_Bills_TaxRate", "[ServiceTaxRate] >= 0 AND [ServiceTaxRate] <= 100");
            });

            builder.HasKey(b => b.Id);

            // Period and dates are calendar dates (SQL 'date'), not date-times.
            builder.Property(b => b.PeriodStart).IsRequired().HasColumnType("date");
            builder.Property(b => b.PeriodEnd).IsRequired().HasColumnType("date");
            builder.Property(b => b.IssueDate).IsRequired().HasColumnType("date");
            builder.Property(b => b.DueDate).IsRequired().HasColumnType("date");

            builder.Property(b => b.PlanCharge).HasPrecision(18, 2);
            builder.Property(b => b.UsageCharge).HasPrecision(18, 2);
            builder.Property(b => b.SecurityDepositCharge).HasPrecision(18, 2);
            builder.Property(b => b.ReplacementCharge).HasPrecision(18, 2);
            builder.Property(b => b.DiscountAmount).HasPrecision(18, 2);
            builder.Property(b => b.SubTotal).HasPrecision(18, 2);
            builder.Property(b => b.ServiceTaxRate).HasPrecision(5, 2);
            builder.Property(b => b.ServiceTaxAmount).HasPrecision(18, 2);
            builder.Property(b => b.TotalAmount).HasPrecision(18, 2);

            builder.Property(b => b.Status).IsRequired().HasStringConversion();
            builder.Property(b => b.GeneratedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(b => b.RowVersion).IsRowVersion();

            builder.HasOne(b => b.Connection)
                   .WithMany(c => c.Bills)
                   .HasForeignKey(b => b.ConnectionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.GeneratedByEmployee)
                   .WithMany()
                   .HasForeignKey(b => b.GeneratedByEmployeeId)
                   .OnDelete(DeleteBehavior.Restrict);

            // One bill per connection per billing period (prevents accidental double-billing).
            builder.HasIndex(b => new { b.ConnectionId, b.PeriodStart }).IsUnique().HasDatabaseName("UX_Bills_Connection_Period");

            builder.HasIndex(b => b.Status).HasDatabaseName("IX_Bills_Status");
            builder.HasIndex(b => b.DueDate).HasDatabaseName("IX_Bills_DueDate");
        }
    }
}
