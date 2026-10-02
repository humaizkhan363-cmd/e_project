using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// A postpaid bill for one connection and one billing period. Generated and edited only by the
    /// Accounts department. The amount paid is the sum of its <see cref="Payments"/>; the amount due
    /// is <see cref="TotalAmount"/> minus that sum.
    /// </summary>
    public class Bill
    {
        public int Id { get; set; }

        public int ConnectionId { get; set; }
        public Connection Connection { get; set; } = null!;

        // ---- Billing period (dates only, no time-of-day) ---------------------------
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public DateOnly IssueDate { get; set; }
        public DateOnly DueDate { get; set; }

        // ---- Charges -----------------------------------------------------------------
        /// <summary>Plan rental / fee for the period.</summary>
        public decimal PlanCharge { get; set; }

        /// <summary>
        /// Total usage for the period: call charges calculated from the minutes below and the
        /// plan's per-minute rates, plus any other usage (e.g. extra internet hours).
        /// </summary>
        public decimal UsageCharge { get; set; }

        /// <summary>Landline: local call minutes in the period (charged at the plan's local rate).</summary>
        public int LocalMinutes { get; set; }

        /// <summary>Landline STD plans: STD call minutes in the period.</summary>
        public int StdMinutes { get; set; }

        /// <summary>Landline STD plans: messaging-for-mobiles minutes in the period.</summary>
        public int MobileMinutes { get; set; }

        /// <summary>Call charges calculated from the minutes above (part of <see cref="UsageCharge"/>).</summary>
        public decimal CallCharge { get; set; }

        /// <summary>The plan whose fee was charged on this bill (null when no plan fee was due).</summary>
        public int? BilledPlanId { get; set; }
        public Plan? BilledPlan { get; set; }

        /// <summary>
        /// Last day covered by the plan fee charged on this bill (period start + plan validity).
        /// A later bill starting before this date does not charge the plan fee again.
        /// </summary>
        public DateOnly? PlanValidUntil { get; set; }

        /// <summary>Security deposit billed with this bill (normally only the first one).</summary>
        public decimal SecurityDepositCharge { get; set; }

        /// <summary>Charge for equipment replaced because the customer damaged it.</summary>
        public decimal ReplacementCharge { get; set; }

        /// <summary>Discount from a bulk scheme, as a positive amount that is subtracted.</summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>Charges minus discount, before tax.</summary>
        public decimal SubTotal { get; set; }

        /// <summary>Service-tax percentage applied (the spec says 12.24), frozen on the bill.</summary>
        public decimal ServiceTaxRate { get; set; }

        public decimal ServiceTaxAmount { get; set; }

        /// <summary>Amount the customer must pay: SubTotal + ServiceTaxAmount.</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Unpaid amount of this connection's earlier bills at the time this bill was issued
        /// (balance brought forward). Shown on the bill so the customer sees the full amount due;
        /// it stays payable against those earlier bills, so it is not part of <see cref="TotalAmount"/>.
        /// </summary>
        public decimal PreviousBalance { get; set; }

        public BillStatus Status { get; set; } = BillStatus.Issued;

        /// <summary>Accounts employee who generated the bill.</summary>
        public int GeneratedByEmployeeId { get; set; }
        public Employee GeneratedByEmployee { get; set; } = null!;

        /// <summary>Set by the database (UTC) when the bill is inserted.</summary>
        public DateTime GeneratedAtUtc { get; set; }

        /// <summary>Concurrency token.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
