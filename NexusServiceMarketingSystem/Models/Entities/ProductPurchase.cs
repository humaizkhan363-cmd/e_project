namespace NexusServiceMarketingSystem.Models.Entities;

/// <summary>A supplier purchase and the stock received against it.</summary>
public class ProductPurchase
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public string? SupplierReference { get; set; }
    public string? Notes { get; set; }
    public int RecordedByEmployeeId { get; set; }
    public Employee RecordedByEmployee { get; set; } = null!;
    public DateTime RecordedAtUtc { get; set; }
}
