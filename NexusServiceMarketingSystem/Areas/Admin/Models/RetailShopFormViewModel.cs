using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NexusServiceMarketingSystem.Areas.Admin.Models
{
    /// <summary>Create / edit form for a retail shop.</summary>
    public class RetailShopFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(300)]
        [Display(Name = "Address")]
        public string AddressLine { get; set; } = string.Empty;

        [Phone, StringLength(20)]
        public string? Phone { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a city.")]
        [Display(Name = "City")]
        public int CityId { get; set; }

        public bool IsActive { get; set; } = true;
        public List<SelectListItem> Cities { get; set; } = new();
    }
}
