using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NexusServiceMarketingSystem.Areas.Admin.Models;

/// <summary>Form to record equipment bought from a vendor.</summary>
public class ProductPurchaseFormViewModel
{
    [Range(1,int.MaxValue)] public int VendorId { get; set; }
    [Range(1,int.MaxValue)] public int ProductId { get; set; }
    [Range(1,1000000)] public int Quantity { get; set; }
    [Range(typeof(decimal),"0","999999999")] public decimal UnitPrice { get; set; }
    [Range(typeof(decimal),"0","999999999")] [Display(Name="Amount paid to date")] public decimal AmountPaid { get; set; }
    [DataType(DataType.Date)] public DateOnly PurchaseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [StringLength(100)] [Display(Name="Supplier invoice / reference")] public string? SupplierReference { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    public List<SelectListItem> Vendors { get; set; } = new();
    public List<SelectListItem> Products { get; set; } = new();
}
