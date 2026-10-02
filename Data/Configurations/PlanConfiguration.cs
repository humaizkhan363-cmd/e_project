using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Data.Seed;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: Plans (the plan / charge catalogue maintained by the Admin).</summary>
    public class PlanConfiguration : IEntityTypeConfiguration<Plan>
    {
        public void Configure(EntityTypeBuilder<Plan> builder)
        {
            builder.ToTable("Plans", t =>
            {
                t.HasCheckConstraint("CK_Plans_ConnectionType_Letter", ConfigurationHelpers.ConnectionTypeLetter("ConnectionType"));
                t.HasCheckConstraint("CK_Plans_Kind_Values", ConfigurationHelpers.EnumInList<PlanKind>("Kind"));

                // Landline kinds only for Telephone plans; Hourly / Unlimited only for Dial-Up or Broadband.
                t.HasCheckConstraint("CK_Plans_Kind_Matches_Type",
                    "([Kind] IN ('LocalRental', 'StdRental') AND [ConnectionType] = 'T') " +
                    "OR ([Kind] IN ('Hourly', 'Unlimited') AND [ConnectionType] IN ('D', 'B'))");

                // Hourly plans must say how many hours; unlimited plans must say the speed.
                t.HasCheckConstraint("CK_Plans_Hourly_Hours",
                    "[Kind] <> 'Hourly' OR ([IncludedHours] IS NOT NULL AND [IncludedHours] > 0)");
                t.HasCheckConstraint("CK_Plans_Unlimited_Speed",
                    "[Kind] <> 'Unlimited' OR ([SpeedKbps] IS NOT NULL AND [SpeedKbps] > 0)");

                t.HasCheckConstraint("CK_Plans_Amounts",
                    "[ValidityMonths] > 0 AND [Price] >= 0 AND [SecurityDeposit] >= 0");
            });

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
            builder.Property(p => p.Description).HasMaxLength(500);
            builder.Property(p => p.ConnectionType).IsRequired().HasConnectionTypeCode();
            builder.Property(p => p.Kind).IsRequired().HasStringConversion();
            builder.Property(p => p.Price).HasPrecision(18, 2);
            builder.Property(p => p.SecurityDeposit).HasPrecision(18, 2);
            builder.Property(p => p.LocalCallRatePerMinute).HasPrecision(10, 4);
            builder.Property(p => p.StdCallRatePerMinute).HasPrecision(10, 4);
            builder.Property(p => p.MobileMessagingRatePerMinute).HasPrecision(10, 4);
            builder.Property(p => p.IsActive).IsRequired();
            builder.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(p => p.Name).IsUnique().HasDatabaseName("UX_Plans_Name");
            builder.HasIndex(p => p.ConnectionType).HasDatabaseName("IX_Plans_ConnectionType");

            // Seed the plan catalogue from the specification.
            builder.HasData(SeedData.GetPlans());
        }
    }
}
