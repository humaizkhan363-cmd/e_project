using System.ComponentModel.DataAnnotations;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Areas.Admin.Models
{
    /// <summary>
    /// Create/Edit form for Plan. Which fields are required depends on ConnectionType and Kind
    /// (mirrors the CK_Plans_* database CHECK constraints); the controller validates this
    /// combination server-side in addition to these attributes, since a single annotation
    /// cannot express "required only when another field has a certain value".
    /// </summary>
    public class PlanFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Plan name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Connection Type")]
        public ConnectionType ConnectionType { get; set; }

        [Required]
        public PlanKind Kind { get; set; }

        [Range(1, 10000)]
        [Display(Name = "Included Hours (Hourly plans only)")]
        public int? IncludedHours { get; set; }

        [Range(1, 100000)]
        [Display(Name = "Speed, Kbps (Unlimited plans only)")]
        public int? SpeedKbps { get; set; }

        [Range(1, 60, ErrorMessage = "Validity must be between 1 and 60 months.")]
        [Display(Name = "Validity (months)")]
        public int ValidityMonths { get; set; }

        [Range(0, 999999)]
        public decimal Price { get; set; }

        [Range(0, 999999)]
        [Display(Name = "Security Deposit")]
        public decimal SecurityDeposit { get; set; }

        [Range(0, 9999)]
        [Display(Name = "Local Call Rate / min (Landline only)")]
        public decimal? LocalCallRatePerMinute { get; set; }

        [Range(0, 9999)]
        [Display(Name = "STD Call Rate / min (Landline STD only)")]
        public decimal? StdCallRatePerMinute { get; set; }

        [Range(0, 9999)]
        [Display(Name = "Mobile Messaging Rate / min (Landline STD only)")]
        public decimal? MobileMessagingRatePerMinute { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Checks the ConnectionType/Kind/field combination against the same rules the
        /// database CHECK constraints enforce, so a bad combination is rejected with a clear
        /// message instead of a raw SQL error.</summary>
        public IEnumerable<string> ValidateBusinessRules()
        {
            bool isLandlineKind = Kind is PlanKind.LocalRental or PlanKind.StdRental;
            bool isInternetKind = Kind is PlanKind.Hourly or PlanKind.Unlimited;

            if (isLandlineKind && ConnectionType != ConnectionType.Telephone)
            {
                yield return "Local/STD rental plans must have Connection Type = Telephone.";
            }

            if (isInternetKind && ConnectionType == ConnectionType.Telephone)
            {
                yield return "Hourly/Unlimited plans must have Connection Type = Dial-Up or Broadband.";
            }

            if (Kind == PlanKind.Hourly && (IncludedHours is null or <= 0))
            {
                yield return "Hourly plans need a positive Included Hours value.";
            }

            if (Kind == PlanKind.Unlimited && (SpeedKbps is null or <= 0))
            {
                yield return "Unlimited plans need a positive Speed (Kbps) value.";
            }
        }
    }
}
