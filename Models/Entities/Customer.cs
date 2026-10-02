using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// A person or company that orders or holds Nexus connections. The contact number is
    /// searchable (advanced search by the number given when applying).
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public CustomerType CustomerType { get; set; } = CustomerType.Individual;

        /// <summary>Company name; required when <see cref="CustomerType"/> is Corporate.</summary>
        public string? CompanyName { get; set; }

        public string? Email { get; set; }

        /// <summary>Contact number provided when applying for a connection.</summary>
        public string Phone { get; set; } = string.Empty;

        public string AddressLine { get; set; } = string.Empty;
        public string? PostalCode { get; set; }

        public int CityId { get; set; }
        public City City { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        /// <summary>Set by the database (UTC) when the row is inserted.</summary>
        public DateTime CreatedAtUtc { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Connection> Connections { get; set; } = new List<Connection>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

        /// <summary>The customer's login account, if one was created.</summary>
        public User? User { get; set; }
    }
}
