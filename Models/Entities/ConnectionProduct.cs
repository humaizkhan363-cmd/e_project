namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// Equipment (modem / router...) issued to a connection. Also records replacements
    /// so a replacement charge can be billed when a customer damaged the original.
    /// </summary>
    public class ConnectionProduct
    {
        public int Id { get; set; }

        public int ConnectionId { get; set; }
        public Connection Connection { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        /// <summary>Number of units issued (at least 1).</summary>
        public int Quantity { get; set; } = 1;

        /// <summary>Device serial number; unique per product when present.</summary>
        public string? SerialNumber { get; set; }

        /// <summary>True when this unit replaced a previous one (may carry a replacement charge).</summary>
        public bool IsReplacement { get; set; }

        /// <summary>Set by the database (UTC) when the row is inserted.</summary>
        public DateTime IssuedAtUtc { get; set; }

        /// <summary>Set when the unit is returned; null while still with the customer.</summary>
        public DateTime? ReturnedAtUtc { get; set; }

        public string? Notes { get; set; }
    }
}
