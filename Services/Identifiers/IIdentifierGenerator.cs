using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Services.Identifiers
{
    /// <summary>
    /// Creates the two system-generated identifiers: the 11-character order number and the
    /// 16-character account ID. Users never type these; controllers must never accept them from a form.
    ///
    /// CONCURRENCY: every call atomically increments a counter row in the database, so two requests
    /// can never receive the same value. If the call runs inside the same database transaction that
    /// inserts the order / connection, a failed insert also rolls the counter back, so no serial
    /// numbers are skipped. Outside a transaction the value is still unique, but a failed insert can
    /// leave a gap.
    /// </summary>
    public interface IIdentifierGenerator
    {
        /// <summary>Returns the next order number for the type, e.g. "D0000000001".</summary>
        Task<string> GenerateOrderNumberAsync(ConnectionType type, CancellationToken cancellationToken = default);

        /// <summary>Returns the next account ID for the type and 3-digit city code, e.g. "B001000000000001".</summary>
        Task<string> GenerateAccountIdAsync(ConnectionType type, string cityCode, CancellationToken cancellationToken = default);

        /// <summary>Generates an order number for <paramref name="order"/> and stores it on the order.</summary>
        Task AssignOrderNumberAsync(Order order, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates an account ID for <paramref name="connection"/> (using its type and its city's code)
        /// and stores it on the connection.
        /// </summary>
        Task AssignAccountIdAsync(Connection connection, CancellationToken cancellationToken = default);
    }
}
