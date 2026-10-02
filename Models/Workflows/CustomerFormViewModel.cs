using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Workflows;

public class CustomerFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Customer type")]
    public CustomerType CustomerType { get; set; } = CustomerType.Individual;

    [StringLength(150)]
    [Display(Name = "Company name")]
    public string? CompanyName { get; set; }

    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(300)]
    [Display(Name = "Address")]
    public string AddressLine { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Postal code")]
    public string? PostalCode { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a city.")]
    public int CityId { get; set; }

    public List<SelectListItem> Cities { get; set; } = new();
}

public class CustomerStatusViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string City { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int OrdersCount { get; set; }
    public int ConnectionsCount { get; set; }
    public string? Username { get; set; }
}
