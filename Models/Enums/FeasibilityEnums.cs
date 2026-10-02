namespace NexusServiceMarketingSystem.Models.Enums
{
    /// <summary>
    /// What a feasibility check covers. A dial-up order for a customer without a Nexus landline
    /// needs both a Landline and an Internet check; a customer who already holds a Nexus landline
    /// needs the Internet check only.
    /// </summary>
    public enum FeasibilityCheckType
    {
        Landline = 1,
        Internet = 2
    }

    /// <summary>Outcome of a single feasibility check.</summary>
    public enum FeasibilityStatus
    {
        Pending = 1,
        Feasible = 2,
        NotFeasible = 3
    }
}
