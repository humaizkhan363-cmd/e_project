using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.Areas.Admin.Models
{
    public class DiscountSchemeFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Scheme name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Minimum connections must be at least 1.")]
        [Display(Name = "Minimum Connections")]
        public int MinConnections { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Maximum connections must be at least 1 when given.")]
        [Display(Name = "Maximum Connections (leave blank for no upper limit)")]
        public int? MaxConnections { get; set; }

        [Range(0, 100, ErrorMessage = "Discount percent must be between 0 and 100.")]
        [Display(Name = "Discount Percent")]
        public decimal DiscountPercent { get; set; }

        [Display(Name = "Applies to Advance Payment")]
        public bool AppliesToAdvance { get; set; } = true;

        [Display(Name = "Applies to Security Deposit")]
        public bool AppliesToSecurityDeposit { get; set; } = true;

        public bool IsActive { get; set; } = true;
    }
}
