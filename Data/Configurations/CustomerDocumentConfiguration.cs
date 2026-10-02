using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Data.Configurations;

/// <summary>Table: CustomerDocuments (customer records filed by year and city).</summary>
public class CustomerDocumentConfiguration : IEntityTypeConfiguration<CustomerDocument>
{
    public void Configure(EntityTypeBuilder<CustomerDocument> b)
    {
        b.ToTable("CustomerDocuments", t => t.HasCheckConstraint("CK_CustomerDocuments_Year", "[DocumentYear] BETWEEN 2000 AND 2100"));
        b.HasKey(x=>x.Id);
        b.Property(x=>x.DocumentType).IsRequired().HasStringConversion();
        b.Property(x=>x.OriginalFileName).IsRequired().HasMaxLength(255);
        b.Property(x=>x.StorageName).IsRequired().HasMaxLength(80);
        b.Property(x=>x.ContentType).IsRequired().HasMaxLength(100);
        b.Property(x=>x.Notes).HasMaxLength(500);
        b.Property(x=>x.UploadedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        b.HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x=>x.City).WithMany().HasForeignKey(x=>x.CityId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x=>x.UploadedByUser).WithMany().HasForeignKey(x=>x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.CustomerId,x.DocumentYear,x.CityId});
    }
}
