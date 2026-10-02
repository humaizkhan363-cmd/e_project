namespace NexusServiceMarketingSystem.Models.Enums
{
    /// <summary>
    /// Business role of an employee. This is the single source of truth for what a staff
    /// member may do (a customer login has no employee record and therefore no staff role).
    /// </summary>
    public enum EmployeeRole
    {
        /// <summary>Manager: employees, shops, vendors, plans, stock.</summary>
        Admin = 1,

        /// <summary>Accounts department: generates bills, records office payments.</summary>
        Accounts = 2,

        /// <summary>Technical staff: feasibility, creates/deactivates connections, equipment.</summary>
        Technical = 3,

        /// <summary>Retail outlet staff: places orders, records shop payments.</summary>
        RetailStaff = 4
    }
}
