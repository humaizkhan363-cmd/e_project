using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Workflows;

public class BillGenerationViewModel
{
    [Range(1, int.MaxValue)]
    [Display(Name = "Connection")]
    public int ConnectionId { get; set; }
    [Required, DataType(DataType.Date)]
    [Display(Name = "Billing period starts")]
    public DateOnly PeriodStart { get; set; }
    [Required, DataType(DataType.Date)]
    [Display(Name = "Billing period ends")]
    public DateOnly PeriodEnd { get; set; }
    [Required, DataType(DataType.Date)]
    [Display(Name = "Issue date")]
    public DateOnly IssueDate { get; set; }
    [Required, DataType(DataType.Date)]
    [Display(Name = "Due date")]
    public DateOnly DueDate { get; set; }
    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Usage charges")]
    public decimal UsageCharge { get; set; }
    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Equipment replacement charges")]
    public decimal ReplacementCharge { get; set; }
    public List<SelectListItem> Connections { get; set; } = new();
}

public class PaymentFormViewModel
{
    public int BillId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal OutstandingAmount { get; set; }
    [Required, Range(typeof(decimal), "0.01", "999999999")]
    public decimal Amount { get; set; }
    [Required]
    public PaymentMethod? Method { get; set; }
    [StringLength(100)]
    public string? Reference { get; set; }
}

public class BillListItemViewModel
{
    public Bill Bill { get; set; } = null!;
    public decimal AmountPaid { get; set; }
    public decimal Outstanding { get; set; }
    public string DisplayStatus { get; set; } = string.Empty;
}

public class FeedbackFormViewModel
{
    [Range(1, 5)]
    [Display(Name = "Rating")]
    public int Rating { get; set; } = 5;
    [Required, StringLength(2000)]
    [Display(Name = "Comments")]
    public string Comments { get; set; } = string.Empty;
    [Display(Name = "Related order (optional)")]
    public int? OrderId { get; set; }
    [Display(Name = "Related connection (optional)")]
    public int? ConnectionId { get; set; }
    public List<SelectListItem> Orders { get; set; } = new();
    public List<SelectListItem> Connections { get; set; } = new();
}

public class AdvancedSearchViewModel
{
    public string? Search { get; set; }
    [Display(Name = "Order number")]
    public string? OrderNumber { get; set; }
    [Display(Name = "Account ID")]
    public string? AccountId { get; set; }
    [Display(Name = "Customer name, phone, or email")]
    public string? Customer { get; set; }
    [Display(Name = "Service type")]
    public ConnectionType? ConnectionType { get; set; }
    [Display(Name = "Order status")]
    public OrderStatus? OrderStatus { get; set; }
    [Display(Name = "Connection status")]
    public ConnectionStatus? ConnectionStatus { get; set; }
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }
    [DataType(DataType.Date)]
    public DateTime? Through { get; set; }
    public List<Order> Orders { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
}

public class AdminReportsViewModel
{
    public int Customers { get; set; }
    public int ActiveCustomers { get; set; }
    public int Employees { get; set; }
    public int RetailShops { get; set; }
    public int Plans { get; set; }
    public int Products { get; set; }
    public int LowStockProducts { get; set; }
    public int Vendors { get; set; }
    public int Orders { get; set; }
    public int PendingOrders { get; set; }
    public int FeasibleOrders { get; set; }
    public int NotFeasibleOrders { get; set; }
    public int ConnectedOrders { get; set; }
    public int Connections { get; set; }
    public int ActiveConnections { get; set; }
    public int TemporarilyInactiveConnections { get; set; }
    public int PermanentlyInactiveConnections { get; set; }
    public int Bills { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }
    public int OverdueBills { get; set; }
    /// <summary>Modems/routers needed by internet orders that are still open (placed, being checked or feasible).</summary>
    public int EquipmentRequired { get; set; }
    /// <summary>Modems/routers currently in stock.</summary>
    public int EquipmentInStock { get; set; }
    /// <summary>How many more units must be purchased to serve the open orders.</summary>
    public int EquipmentShortfall { get; set; }
    public int Payments { get; set; }
    public int FeedbackItems { get; set; }
    public decimal AverageRating { get; set; }
}
