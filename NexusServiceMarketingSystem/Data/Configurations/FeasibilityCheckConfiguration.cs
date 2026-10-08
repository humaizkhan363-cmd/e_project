using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: FeasibilityChecks.</summary>
    public class FeasibilityCheckConfiguration : IEntityTypeConfiguration<FeasibilityCheck>
    {
        public void Configure(EntityTypeBuilder<FeasibilityCheck> builder)
        {
            builder.ToTable("FeasibilityChecks", t =>
            {
                t.HasCheckConstraint("CK_FeasibilityChecks_CheckType_Values", ConfigurationHelpers.EnumInList<FeasibilityCheckType>("CheckType"));
                t.HasCheckConstraint("CK_FeasibilityChecks_Status_Values", ConfigurationHelpers.EnumInList<FeasibilityStatus>("Status"));
                t.HasCheckConstraint("CK_FeasibilityChecks_Distance", "[DistanceKm] IS NULL OR [DistanceKm] >= 0");
            });

            builder.HasKey(f => f.Id);

            builder.Property(f => f.CheckType).IsRequired().HasStringConversion();
            builder.Property(f => f.Status).IsRequired().HasStringConversion();
            builder.Property(f => f.DistanceKm).HasPrecision(8, 2);
            builder.Property(f => f.Remarks).HasMaxLength(500);
            builder.Property(f => f.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasOne(f => f.Order)
                   .WithMany(o => o.FeasibilityChecks)
                   .HasForeignKey(f => f.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.CheckedByEmployee)
                   .WithMany()
                   .HasForeignKey(f => f.CheckedByEmployeeId)
                   .OnDelete(DeleteBehavior.Restrict);

            // An order has at most one check of each type (landline / internet).
            builder.HasIndex(f => new { f.OrderId, f.CheckType }).IsUnique().HasDatabaseName("UX_FeasibilityChecks_Order_Type");
            builder.HasIndex(f => f.Status).HasDatabaseName("IX_FeasibilityChecks_Status");
        }
    }
}
