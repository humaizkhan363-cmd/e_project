namespace NexusServiceMarketingSystem.Models.Entities
{
    /// <summary>
    /// Customer feedback (the spec requires feedback to be collected). It may refer to an order,
    /// to a connection, or be general.
    /// </summary>
    public class Feedback
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        public int? ConnectionId { get; set; }
        public Connection? Connection { get; set; }

        /// <summary>Star rating from 1 (poor) to 5 (excellent).</summary>
        public int Rating { get; set; }

        public string Comments { get; set; } = string.Empty;

        /// <summary>Set by the database (UTC) when the feedback is saved.</summary>
        public DateTime SubmittedAtUtc { get; set; }
    }
}
