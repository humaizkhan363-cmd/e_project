using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: Feedbacks.</summary>
    public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
    {
        public void Configure(EntityTypeBuilder<Feedback> builder)
        {
            builder.ToTable("Feedbacks", t =>
            {
                t.HasCheckConstraint("CK_Feedbacks_Rating", "[Rating] >= 1 AND [Rating] <= 5");
            });

            builder.HasKey(f => f.Id);

            builder.Property(f => f.Rating).IsRequired();
            builder.Property(f => f.Comments).IsRequired().HasMaxLength(2000);
            builder.Property(f => f.SubmittedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasOne(f => f.Customer)
                   .WithMany(c => c.Feedbacks)
                   .HasForeignKey(f => f.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Optional links: feedback may be about an order, a connection, or general.
            builder.HasOne(f => f.Order)
                   .WithMany()
                   .HasForeignKey(f => f.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Connection)
                   .WithMany(c => c.Feedbacks)
                   .HasForeignKey(f => f.ConnectionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(f => f.SubmittedAtUtc).HasDatabaseName("IX_Feedbacks_SubmittedAtUtc");
        }
    }
}
