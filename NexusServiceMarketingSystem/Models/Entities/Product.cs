using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// A piece of equipment (modem, router...) held in stock and issued with connections.
    /// </summary>
    public class Product
    {
        public int Id { get; set; }

        /// <summary>Stock-keeping code (unique).</summary>
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ProductCategory Category { get; set; }
        public string? Description { get; set; }

        public int VendorId { get; set; }
        public Vendor Vendor { get; set; } = null!;

        /// <summary>What Nexus pays the vendor per unit.</summary>
        public decimal PurchasePrice { get; set; }

        /// <summary>What the customer is charged if the unit is replaced because they damaged it.</summary>
        public decimal ReplacementCharge { get; set; }

        /// <summary>Units currently in stock (never negative).</summary>
        public int StockQuantity { get; set; }

        /// <summary>When stock falls to this level the product should be re-ordered.</summary>
        public int ReorderLevel { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Set by the database (UTC) when the row is inserted.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>Concurrency token so two clerks cannot overwrite each other's stock changes.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<ConnectionProduct> ConnectionProducts { get; set; } = new List<ConnectionProduct>();
    }
}
