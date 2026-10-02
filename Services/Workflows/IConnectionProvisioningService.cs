using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Services.Workflows;

/// <summary>Input for creating the connection(s) of a feasible order.</summary>
public sealed record ProvisionConnectionRequest(int OrderId, int TechnicalEmployeeId, int? ProductId, string? SerialNumber, int? LandlineConnectionId, string? PhoneNumber);

/// <summary>A connection with unpaid bills past their due date (or a suspended one whose dues are now cleared).</summary>
public sealed record OverdueConnection(int ConnectionId, string AccountId, string CustomerName, string CustomerPhone,
    ConnectionStatus Status, int OverdueBills, decimal OverdueAmount, DateOnly? OldestDueDate);

/// <summary>Connection creation (account IDs, equipment, bundled landline), status changes and overdue suspension.</summary>
public interface IConnectionProvisioningService
{
    Task<IReadOnlyList<Connection>> ProvisionAsync(ProvisionConnectionRequest request, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(int connectionId, int technicalEmployeeId, ConnectionStatus status, CancellationToken cancellationToken = default);

    /// <summary>Active connections with overdue bills (postpaid: unpaid bills make the connection temporarily inactive).</summary>
    Task<IReadOnlyList<OverdueConnection>> GetOverdueAsync(CancellationToken cancellationToken = default);

    /// <summary>Temporarily inactive connections whose bills are all paid, so they can be reactivated.</summary>
    Task<IReadOnlyList<OverdueConnection>> GetClearedSuspendedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes active connections with overdue bills temporarily inactive. Pass a connection id to suspend one,
    /// or null to suspend every overdue connection. Returns how many were suspended.
    /// </summary>
    Task<int> SuspendOverdueAsync(int technicalEmployeeId, int? connectionId, CancellationToken cancellationToken = default);
}
