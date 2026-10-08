using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: IdentifierCounters (serial counters for order numbers and account IDs).</summary>
    public class IdentifierCounterConfiguration : IEntityTypeConfiguration<IdentifierCounter>
    {
        public void Configure(EntityTypeBuilder<IdentifierCounter> builder)
        {
            builder.ToTable(IdentifierCounter.TableName, t =>
            {
                t.HasCheckConstraint("CK_IdentifierCounters_LastValue", "[LastValue] >= 0");
            });

            // The scope name is the primary key, so two counters can never share a name and
            // the generator's single-statement upsert always targets exactly one row.
            builder.HasKey(c => c.Scope);

            builder.Property(c => c.Scope).IsRequired().HasMaxLength(40).IsUnicode(false);
            builder.Property(c => c.LastValue).IsRequired();
        }
    }
}
