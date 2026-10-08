namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// A Nexus retail outlet where customers enquire, place orders and pay bills.
    /// Maintained by the Admin.
    /// </summary>
    public class RetailShop
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string? Phone { get; set; }

        public int CityId { get; set; }
        public City City { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        /// <summary>Retail employees working at this shop.</summary>
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();

        /// <summary>Orders placed at this shop.</summary>
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
