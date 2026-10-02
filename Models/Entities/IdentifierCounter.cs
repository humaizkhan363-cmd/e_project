namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// Infrastructure table behind the identifier generator: one row per counter "scope"
    /// (for example "ORDER:D" or "ACCOUNT:D001") holding the last serial handed out.
    /// Rows are created and incremented ONLY by the identifier generator service.
    /// </summary>
    public class IdentifierCounter
    {
        /// <summary>Name of the table; shared with the generator's SQL so the two cannot drift apart.</summary>
        public const string TableName = "IdentifierCounters";

        /// <summary>Counter name, e.g. "ORDER:B" or "ACCOUNT:T004".</summary>
        public string Scope { get; set; } = string.Empty;

        /// <summary>The last serial number issued for this scope.</summary>
        public long LastValue { get; set; }
    }
}
