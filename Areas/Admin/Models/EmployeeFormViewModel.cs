using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Admin.Models
{
    public class EmployeeFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public EmployeeRole? Role { get; set; }

        [Display(Name = "Retail shop")]
        public int? RetailShopId { get; set; }

        public bool IsActive { get; set; } = true;

        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Initial password")]
        public string InitialPassword { get; set; } = string.Empty;

        public List<SelectListItem> Shops { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();
    }

    public class EmployeeEditFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public EmployeeRole? Role { get; set; }

        [Display(Name = "Retail shop")]
        public int? RetailShopId { get; set; }

        public bool IsActive { get; set; } = true;

        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        public List<SelectListItem> Shops { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();
    }
}
