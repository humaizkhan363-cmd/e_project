using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Data.Configurations
{
    /// <summary>Table: Users (login accounts).</summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", t =>
            {
                // Exactly one owner: an employee OR a customer, never both, never neither.
                t.HasCheckConstraint("CK_Users_OneOwner",
                    "([EmployeeId] IS NOT NULL AND [CustomerId] IS NULL) OR ([EmployeeId] IS NULL AND [CustomerId] IS NOT NULL)");
            });

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            builder.Property(u => u.IsActive).IsRequired();
            builder.Property(u => u.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            // One login per employee.
            builder.HasOne(u => u.Employee)
                   .WithOne(e => e.User)
                   .HasForeignKey<User>(u => u.EmployeeId)
                   .OnDelete(DeleteBehavior.Restrict);

            // One login per customer.
            builder.HasOne(u => u.Customer)
                   .WithOne(c => c.User)
                   .HasForeignKey<User>(u => u.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(u => u.Username).IsUnique().HasDatabaseName("UX_Users_Username");
            builder.HasIndex(u => u.EmployeeId).IsUnique().HasFilter("[EmployeeId] IS NOT NULL").HasDatabaseName("UX_Users_EmployeeId");
            builder.HasIndex(u => u.CustomerId).IsUnique().HasFilter("[CustomerId] IS NOT NULL").HasDatabaseName("UX_Users_CustomerId");
        }
    }
}
