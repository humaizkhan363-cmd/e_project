namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// A manufacturer/supplier from whom Nexus buys modems, routers and other equipment.
    /// Maintained only by the Admin.
    /// </summary>
    public class Vendor
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string? AddressLine { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Set by the database (UTC) when the row is inserted.</summary>
        public DateTime CreatedAtUtc { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
