namespace NexusServiceMarketingSystem.Models.Account
{
    /// <summary>
    /// Role-claim string constants used in [Authorize(Roles = ...)] across every controller,
    /// so a role name is spelled exactly the same way everywhere. Staff roles reuse the
    /// EmployeeRole enum names directly (nameof(EmployeeRole.Admin) etc.); "Customer" has no
    /// matching enum member because customers are not employees, so it is defined here instead.
    /// </summary>
    public static class RoleNames
    {
        public const string Customer = "Customer";

        /// <summary>Claim type holding the numeric Employee.Id, present only for staff logins.</summary>
        public const string EmployeeIdClaim = "NexusEmployeeId";

        /// <summary>Claim type holding the numeric Customer.Id, present only for customer logins.</summary>
        public const string CustomerIdClaim = "NexusCustomerId";

        /// <summary>Claim type holding the person's display name (Employee.FullName or Customer.FullName).</summary>
        public const string DisplayNameClaim = "NexusDisplayName";
    }
}
