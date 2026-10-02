using System.ComponentModel.DataAnnotations;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Admin.Models
{
    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "SKU is required.")]
        [StringLength(40)]
        [Display(Name = "SKU")]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public ProductCategory Category { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select a vendor.")]
        [Display(Name = "Vendor")]
        public int VendorId { get; set; }

        [Range(0, 9999999, ErrorMessage = "Purchase price cannot be negative.")]
        [Display(Name = "Purchase Price")]
        public decimal PurchasePrice { get; set; }

        [Range(0, 9999999, ErrorMessage = "Replacement charge cannot be negative.")]
        [Display(Name = "Replacement Charge")]
        public decimal ReplacementCharge { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Reorder level cannot be negative.")]
        [Display(Name = "Reorder Level")]
        public int ReorderLevel { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Populated by the controller for the Vendor dropdown; not bound from the form.</summary>
        public List<VendorOption> VendorOptions { get; set; } = new();

        public class VendorOption
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
