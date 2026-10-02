using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Services.Workflows;

/// <summary>
/// Input for one bill. Call charges are calculated from the minutes and the plan's per-minute rates;
/// <paramref name="OtherUsageCharge"/> covers any other usage (for example extra internet hours).
/// Replacement charges are not entered: unbilled equipment replacements are added automatically.
/// </summary>
public sealed record GenerateBillRequest(int ConnectionId, int AccountsEmployeeId, DateOnly PeriodStart, DateOnly PeriodEnd,
    DateOnly IssueDate, DateOnly DueDate, int LocalMinutes, int StdMinutes, int MobileMinutes, decimal OtherUsageCharge);

/// <summary>What the next bill of a connection will contain, shown to Accounts before generating it.</summary>
public sealed record BillPreview(string AccountId, string PlanName, decimal PlanCharge, DateOnly? PlanPaidThrough,
    decimal UnbilledReplacementCharges, decimal PreviousBalance, decimal? LocalRate, decimal? StdRate, decimal? MobileRate);

/// <summary>A payment against one bill, received by an Accounts employee (no shop) or a retail employee (their shop).</summary>
public sealed record RecordPaymentRequest(int BillId, int EmployeeId, int? RetailShopId, decimal Amount,
    PaymentMethod Method, string? Reference);

/// <summary>Bill generation (Accounts only), payment recording and the bill preview.</summary>
public interface IBillingService
{
    Task<Bill> GenerateBillAsync(GenerateBillRequest request, CancellationToken cancellationToken = default);
    Task<Payment> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default);
    Task<decimal> GetOutstandingAsync(int billId, CancellationToken cancellationToken = default);
    Task<BillPreview> PreviewAsync(int connectionId, DateOnly periodStart, CancellationToken cancellationToken = default);
}
