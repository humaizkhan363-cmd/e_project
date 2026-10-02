using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.Areas.Admin.Models
{
    public class VendorFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vendor name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone is required.")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Address")]
        public string? AddressLine { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
