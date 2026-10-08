using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// One feasibility check for an order (distance from the exchange, server availability, etc.).
    /// An order has at most one check per <see cref="FeasibilityCheckType"/>.
    /// </summary>
    public class FeasibilityCheck
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public FeasibilityCheckType CheckType { get; set; }
        public FeasibilityStatus Status { get; set; } = FeasibilityStatus.Pending;

        /// <summary>Distance from the serving exchange, in kilometres.</summary>
        public decimal? DistanceKm { get; set; }

        /// <summary>Whether server capacity was available for an internet connection.</summary>
        public bool? ServerAvailable { get; set; }

        public string? Remarks { get; set; }

        /// <summary>Technical employee who performed the check (null while still pending).</summary>
        public int? CheckedByEmployeeId { get; set; }
        public Employee? CheckedByEmployee { get; set; }

        public DateTime? CheckedAtUtc { get; set; }

        /// <summary>Set by the database (UTC) when the row is inserted.</summary>
        public DateTime CreatedAtUtc { get; set; }
    }
}
