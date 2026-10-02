using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Services.Workflows;

public sealed record GenerateBillRequest(int ConnectionId, int AccountsEmployeeId, DateOnly PeriodStart, DateOnly PeriodEnd,
    DateOnly IssueDate, DateOnly DueDate, decimal UsageCharge, decimal ReplacementCharge);

public sealed record RecordPaymentRequest(int BillId, int EmployeeId, int? RetailShopId, decimal Amount,
    PaymentMethod Method, string? Reference);

public interface IBillingService
{
    Task<Bill> GenerateBillAsync(GenerateBillRequest request, CancellationToken cancellationToken = default);
    Task<Payment> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default);
    Task<decimal> GetOutstandingAsync(int billId, CancellationToken cancellationToken = default);
}
