using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Workflows;

public class OrderFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a customer.")]
    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Required]
    [Display(Name = "Service type")]
    public ConnectionType? ConnectionType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a plan.")]
    public int PlanId { get; set; }

    /// <summary>Dial-up only, when the customer has no Nexus landline: the plan for the telephone line applied for together.</summary>
    [Display(Name = "Landline plan (dial-up without a Nexus landline)")]
    public int? LandlinePlanId { get; set; }

    public List<SelectListItem> LandlinePlans { get; set; } = new();

    [Range(1, int.MaxValue, ErrorMessage = "Select an installation city.")]
    [Display(Name = "Installation city")]
    public int CityId { get; set; }

    [Required, StringLength(300)]
    [Display(Name = "Installation address")]
    public string InstallationAddress { get; set; } = string.Empty;

    [Range(1, 1000)]
    [Display(Name = "Number of connections")]
    public int Quantity { get; set; } = 1;

    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Plans { get; set; } = new();
    public List<SelectListItem> Cities { get; set; } = new();
    public List<SelectListItem> ConnectionTypes { get; set; } = new();
}

public class OrderSearchViewModel
{
    [Display(Name = "Order number")]
    public string? OrderNumber { get; set; }
    [Display(Name = "Customer name, email, or phone")]
    public string? Customer { get; set; }
    [Display(Name = "Service type")]
    public ConnectionType? ConnectionType { get; set; }
    public OrderStatus? Status { get; set; }
    [DataType(DataType.Date)]
    [Display(Name = "From")]
    public DateTime? From { get; set; }
    [DataType(DataType.Date)]
    [Display(Name = "Through")]
    public DateTime? Through { get; set; }
    public List<Order> Results { get; set; } = new();
}

public class OrderDetailsViewModel
{
    public Order Order { get; set; } = null!;
    public bool CanProcess { get; set; }
}

public class FeasibilityCheckFormViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public FeasibilityCheckType CheckType { get; set; }
    [Required]
    public FeasibilityStatus? Status { get; set; }
    [Range(typeof(decimal), "0", "100000")]
    [Display(Name = "Distance from exchange (km)")]
    public decimal? DistanceKm { get; set; }
    [Display(Name = "Internet server capacity available")]
    public bool? ServerAvailable { get; set; }
    [StringLength(500)]
    public string? Remarks { get; set; }
}

public class ConnectionProvisionViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public ConnectionType ConnectionType { get; set; }
    public int Quantity { get; set; }
    [Display(Name = "Equipment product")]
    public int? ProductId { get; set; }
    [Display(Name = "Device serial number (one connection only)")]
    [StringLength(60)]
    public string? SerialNumber { get; set; }
    [Display(Name = "Nexus landline for dial-up")]
    public int? LandlineConnectionId { get; set; }
    /// <summary>One number per new telephone line, separated by commas (checked by the provisioning service).</summary>
    [StringLength(2000)]
    [Display(Name = "Assigned telephone number(s), one per new line, separated by commas")]
    public string? PhoneNumber { get; set; }
    /// <summary>True when the dial-up order also applies for a new telephone line (created during provisioning).</summary>
    public bool IncludesNewLandline { get; set; }
    public string? LandlinePlanName { get; set; }
    public List<SelectListItem> Products { get; set; } = new();
    public List<SelectListItem> Landlines { get; set; } = new();
}

public class ConnectionStatusFormViewModel
{
    public int Id { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public ConnectionStatus Status { get; set; }
    [StringLength(500)]
    public string? Remarks { get; set; }
}
