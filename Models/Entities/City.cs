namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// A city inside Nexus' territory. Every city has a unique three-digit numeric code that
    /// becomes characters 2-4 of the account ID of every connection provided in that city.
    /// </summary>
    public class City
    {
        public int Id { get; set; }

        /// <summary>City display name (unique).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Three-digit numeric code, e.g. "001" (unique, stored as char(3)).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>False when Nexus no longer serves the city (kept, never deleted, for history).</summary>
        public bool IsActive { get; set; } = true;

        public ICollection<RetailShop> RetailShops { get; set; } = new List<RetailShop>();
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    }
}
