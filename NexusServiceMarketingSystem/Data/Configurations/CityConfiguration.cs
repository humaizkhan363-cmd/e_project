using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Common;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: Cities.</summary>
    public class CityConfiguration : IEntityTypeConfiguration<City>
    {
        public void Configure(EntityTypeBuilder<City> builder)
        {
            builder.ToTable("Cities", t =>
            {
                // The code is embedded in every account ID, so it must always be exactly three digits.
                t.HasCheckConstraint("CK_Cities_Code_Format",
                    ConfigurationHelpers.ExactLike("Code", IdentifierFormat.DigitsLikePattern(IdentifierFormat.CityCodeLength)));
            });

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Code).IsRequired().HasColumnType("char(3)");
            builder.Property(c => c.IsActive).IsRequired();

            // Business uniqueness: no two cities share a name or a code.
            builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName("UX_Cities_Name");
            builder.HasIndex(c => c.Code).IsUnique().HasDatabaseName("UX_Cities_Code");
        }
    }
}
