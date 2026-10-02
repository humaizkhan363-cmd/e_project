using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Services.Workflows;

public sealed record PlaceOrderRequest(
    int CustomerId,
    ConnectionType ConnectionType,
    int PlanId,
    int CityId,
    string InstallationAddress,
    int Quantity,
    int? RetailShopId,
    int? PlacedByEmployeeId);

public interface IOrderWorkflowService
{
    Task<Order> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeasibilityCheck>> StartFeasibilityAsync(int orderId, int technicalEmployeeId, CancellationToken cancellationToken = default);
    Task<FeasibilityCheck> CompleteFeasibilityAsync(int checkId, int technicalEmployeeId, FeasibilityStatus status, decimal? distanceKm, bool? serverAvailable, string? remarks, CancellationToken cancellationToken = default);
}
