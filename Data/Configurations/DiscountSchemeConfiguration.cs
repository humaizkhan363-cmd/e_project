using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Data.Seed;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: DiscountSchemes (bulk / corporate discount bands).</summary>
    public class DiscountSchemeConfiguration : IEntityTypeConfiguration<DiscountScheme>
    {
        public void Configure(EntityTypeBuilder<DiscountScheme> builder)
        {
            builder.ToTable("DiscountSchemes", t =>
            {
                t.HasCheckConstraint("CK_DiscountSchemes_Range",
                    "[MinConnections] >= 1 AND ([MaxConnections] IS NULL OR [MaxConnections] >= [MinConnections])");
                t.HasCheckConstraint("CK_DiscountSchemes_Percent",
                    "[DiscountPercent] >= 0 AND [DiscountPercent] <= 100");
            });

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
            builder.Property(d => d.DiscountPercent).HasPrecision(5, 2);
            builder.Property(d => d.AppliesToAdvance).IsRequired();
            builder.Property(d => d.AppliesToSecurityDeposit).IsRequired();
            builder.Property(d => d.IsActive).IsRequired();
            builder.Property(d => d.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(d => d.Name).IsUnique().HasDatabaseName("UX_DiscountSchemes_Name");

            // Two bands cannot start at the same number. (Full overlap checking is done by the
            // Admin screen in a later phase, because a CHECK constraint cannot compare rows.)
            builder.HasIndex(d => d.MinConnections).IsUnique().HasDatabaseName("UX_DiscountSchemes_Min");

            // Seed the four bulk bands from the specification.
            builder.HasData(SeedData.GetDiscountSchemes());
        }
    }
}
