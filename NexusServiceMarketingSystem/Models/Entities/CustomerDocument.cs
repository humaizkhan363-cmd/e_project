using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Entities;

/// <summary>Metadata for a private customer file stored outside the public web root.</summary>
public class CustomerDocument
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int CityId { get; set; }
    public City City { get; set; } = null!;
    public CustomerDocumentType DocumentType { get; set; }
    public int DocumentYear { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Notes { get; set; }
    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;
    public DateTime UploadedAtUtc { get; set; }
}
