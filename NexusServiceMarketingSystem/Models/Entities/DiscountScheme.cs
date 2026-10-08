namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// A bulk / corporate discount band: when a customer takes a number of connections inside
    /// [MinConnections, MaxConnections] they get <see cref="DiscountPercent"/> off the advance
    /// and/or the security deposit. Both limits are inclusive; a null maximum means "and above".
    /// </summary>
    public class DiscountScheme
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Smallest number of connections that qualifies (inclusive).</summary>
        public int MinConnections { get; set; }

        /// <summary>Largest number of connections that qualifies (inclusive); null = no upper limit.</summary>
        public int? MaxConnections { get; set; }

        /// <summary>Percentage discount, 0 to 100.</summary>
        public decimal DiscountPercent { get; set; }

        public bool AppliesToAdvance { get; set; } = true;
        public bool AppliesToSecurityDeposit { get; set; } = true;
        public bool IsActive { get; set; } = true;

        /// <summary>Set by the database (UTC) when the row is inserted.</summary>
        public DateTime CreatedAtUtc { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
