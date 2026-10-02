using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Services.Workflows;

public sealed record ProvisionConnectionRequest(int OrderId, int TechnicalEmployeeId, int? ProductId, string? SerialNumber, int? LandlineConnectionId, string? PhoneNumber);

public interface IConnectionProvisioningService
{
    Task<IReadOnlyList<Connection>> ProvisionAsync(ProvisionConnectionRequest request, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(int connectionId, int technicalEmployeeId, ConnectionStatus status, CancellationToken cancellationToken = default);
}
