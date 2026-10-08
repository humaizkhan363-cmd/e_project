namespace NexusServiceMarketingSystem.Models.Enums
{
    /// <summary>Life-cycle of a connection order, as tracked through the order number.</summary>
    public enum OrderStatus
    {
        /// <summary>Order placed at a retail shop; nothing checked yet.</summary>
        Placed = 1,

        /// <summary>Technical staff are checking whether the area is feasible.</summary>
        UnderFeasibilityCheck = 2,

        /// <summary>Area is feasible; connection can be created.</summary>
        Feasible = 3,

        /// <summary>Area is not feasible; no connection will be provided.</summary>
        NotFeasible = 4,

        /// <summary>Connection has been created for the order.</summary>
        Connected = 5,

        /// <summary>Order was withdrawn or cancelled.</summary>
        Cancelled = 6
    }
}
