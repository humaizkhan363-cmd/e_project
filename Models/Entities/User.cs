namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// A login account. Each account belongs to exactly ONE owner: either an
    /// <see cref="Employee"/> (staff) or a <see cref="Customer"/> (enforced by a database
    /// CHECK constraint). The role is not stored here: it comes from the employee record,
    /// so there is a single source of truth. Passwords are never stored, only a salted hash.
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        /// <summary>Login name (unique, case-insensitive under the default collation).</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Salted password hash (never the password itself).</summary>
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>Set by the database (UTC) when the row is inserted.</summary>
        public DateTime CreatedAtUtc { get; set; }

        public DateTime? LastLoginAtUtc { get; set; }

        /// <summary>Owner when this is a staff login; otherwise null.</summary>
        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        /// <summary>Owner when this is a customer login; otherwise null.</summary>
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
    }
}
