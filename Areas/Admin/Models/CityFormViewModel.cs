using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.Areas.Admin.Models
{
    /// <summary>Create/Edit form for City. A separate model from the entity keeps the form's
    /// validation messages and shape independent of how the database is configured.</summary>
    public class CityFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "City name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "City code is required.")]
        [RegularExpression("^[0-9]{3}$", ErrorMessage = "City code must be exactly 3 digits, e.g. 001.")]
        [Display(Name = "City Code (3 digits)")]
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
