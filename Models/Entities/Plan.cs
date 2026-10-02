using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// A purchasable plan (Dial-Up, Broadband or Landline). Maintained (insert / update /
    /// delete / search) only by the Admin. Which optional columns apply depends on <see cref="Kind"/>.
    /// </summary>
    public class Plan
    {
        public int Id { get; set; }

        /// <summary>Unique, human-readable plan name.</summary>
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>Dial-Up, Broadband or Telephone.</summary>
        public ConnectionType ConnectionType { get; set; }

        /// <summary>Hourly / Unlimited (internet) or LocalRental / StdRental (landline).</summary>
        public PlanKind Kind { get; set; }

        /// <summary>Hourly plans only: hours included in the plan.</summary>
        public int? IncludedHours { get; set; }

        /// <summary>Unlimited plans only: line speed in Kbps.</summary>
        public int? SpeedKbps { get; set; }

        /// <summary>How long the plan (or hour bundle) is valid, in months.</summary>
        public int ValidityMonths { get; set; }

        /// <summary>Plan fee / rental for the validity period.</summary>
        public decimal Price { get; set; }

        /// <summary>Refundable security deposit charged for this plan.</summary>
        public decimal SecurityDeposit { get; set; }

        /// <summary>Landline plans: local call charge per minute.</summary>
        public decimal? LocalCallRatePerMinute { get; set; }

        /// <summary>STD landline plans: STD call charge per minute.</summary>
        public decimal? StdCallRatePerMinute { get; set; }

        /// <summary>STD landline plans: messaging-for-mobiles charge per minute.</summary>
        public decimal? MobileMessagingRatePerMinute { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Set by the database (UTC) when the row is inserted.</summary>
        public DateTime CreatedAtUtc { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Connection> Connections { get; set; } = new List<Connection>();
    }
}
