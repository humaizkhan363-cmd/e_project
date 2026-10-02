using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// A Nexus staff member (admin, accounts, technical or retail). Employee records are
    /// maintained only by the Admin. The login credentials live in <see cref="User"/>.
    /// </summary>
    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        /// <summary>Business role; decides which screens the employee may use.</summary>
        public EmployeeRole Role { get; set; }

        /// <summary>The shop a retail employee works at. Required for RetailStaff, must be empty for all other roles.</summary>
        public int? RetailShopId { get; set; }
        public RetailShop? RetailShop { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Set by the database (UTC) when the row is inserted.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>The employee's login account (null until credentials are issued).</summary>
        public User? User { get; set; }
    }
}
