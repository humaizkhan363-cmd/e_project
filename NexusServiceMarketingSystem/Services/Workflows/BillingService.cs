using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Services.Workflows;

/// <summary>Calculates and saves postpaid bills and records payments; all money rules live here.</summary>
public sealed class BillingService : IBillingService
{
    private const decimal ServiceTaxRatePercent = 12.24m;
    private readonly AppDbContext _db;

    public BillingService(AppDbContext db) => _db = db;

    // Builds one bill: plan fee, usage, deposit / discount on the first bill, replacements, tax and balance brought forward.
    public async Task<Bill> GenerateBillAsync(GenerateBillRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == request.AccountsEmployeeId && e.IsActive && e.Role == EmployeeRole.Accounts, cancellationToken))
            throw new InvalidOperationException("An active Accounts Employee account is required to generate a bill.");
        if (request.PeriodEnd < request.PeriodStart)
            throw new InvalidOperationException("The billing period end must be on or after its start.");
        if (request.DueDate < request.IssueDate)
            throw new InvalidOperationException("The due date must be on or after the issue date.");
        if (request.OtherUsageCharge < 0 || request.LocalMinutes < 0 || request.StdMinutes < 0 || request.MobileMinutes < 0)
            throw new InvalidOperationException("Charges and minutes cannot be negative.");

        var connection = await _db.Connections.Include(c => c.Plan)
            .Include(c => c.Order).ThenInclude(o => o.DiscountScheme)
            .SingleOrDefaultAsync(c => c.Id == request.ConnectionId, cancellationToken)
            ?? throw new InvalidOperationException("Connection not found.");
        if (connection.Status == ConnectionStatus.PermanentlyInactive)
            throw new InvalidOperationException("A bill cannot be generated for a permanently inactive connection.");
        if (await _db.Bills.AnyAsync(b => b.ConnectionId == connection.Id && b.PeriodStart == request.PeriodStart, cancellationToken))
            throw new InvalidOperationException("A bill already exists for this connection and period start date.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        bool firstBill = !await _db.Bills.AnyAsync(b => b.ConnectionId == connection.Id, cancellationToken);

        // Plan fee: charged once per validity period (e.g. a quarterly plan is charged every 3 months, not on every bill).
        DateOnly? paidThrough = await PlanPaidThroughAsync(connection, cancellationToken);
        bool planFeeDue = paidThrough is null || request.PeriodStart > paidThrough.Value;
        decimal planCharge = planFeeDue ? connection.Plan.Price : 0m;

        // Call charges from the minutes used and the plan's per-minute rates (landline plans only).
        decimal callCharge = CallCharge(connection, request.LocalMinutes, request.StdMinutes, request.MobileMinutes);
        decimal usageCharge = RoundMoney(callCharge + request.OtherUsageCharge);

        // Equipment replaced because the customer spoiled it, not billed yet.
        List<ConnectionProduct> replacements = await _db.ConnectionProducts
            .Where(cp => cp.ConnectionId == connection.Id && cp.IsReplacement && cp.BilledOnBillId == null && cp.ReplacementChargeAmount > 0)
            .ToListAsync(cancellationToken);
        decimal replacementCharge = RoundMoney(replacements.Sum(cp => cp.ReplacementChargeAmount));

        // Balance brought forward: what is still unpaid on this connection's earlier bills.
        decimal previousBalance = await PreviousBalanceAsync(connection.Id, cancellationToken);

        decimal securityDeposit = firstBill ? connection.SecurityDepositAmount : 0m;
        decimal discount = firstBill && connection.Order.DiscountScheme?.AppliesToAdvance == true
            ? RoundMoney(planCharge * connection.Order.DiscountPercent / 100m)
            : 0m;
        decimal subTotal = RoundMoney(planCharge + usageCharge + securityDeposit + replacementCharge - discount);
        if (subTotal < 0)
            throw new InvalidOperationException("The discount cannot exceed the charges on this bill.");

        var bill = new Bill
        {
            ConnectionId = connection.Id,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            PlanCharge = planCharge,
            BilledPlanId = planFeeDue ? connection.PlanId : null,
            PlanValidUntil = planFeeDue ? PlanEnd(request.PeriodStart, connection.Plan.ValidityMonths) : null,
            LocalMinutes = request.LocalMinutes,
            StdMinutes = request.StdMinutes,
            MobileMinutes = request.MobileMinutes,
            CallCharge = callCharge,
            UsageCharge = usageCharge,
            SecurityDepositCharge = RoundMoney(securityDeposit),
            ReplacementCharge = replacementCharge,
            PreviousBalance = previousBalance,
            DiscountAmount = discount,
            SubTotal = subTotal,
            ServiceTaxRate = ServiceTaxRatePercent,
            ServiceTaxAmount = RoundMoney(subTotal * ServiceTaxRatePercent / 100m),
            Status = BillStatus.Issued,
            GeneratedByEmployeeId = request.AccountsEmployeeId
        };
        bill.TotalAmount = RoundMoney(bill.SubTotal + bill.ServiceTaxAmount);
        _db.Bills.Add(bill);
        // Mark the replacement charges as billed so they are never charged twice.
        foreach (ConnectionProduct replacement in replacements)
            replacement.BilledOnBill = bill;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return bill;
    }

    // Records a payment; it may not exceed what is still due, and the bill becomes Paid or PartiallyPaid.
    public async Task<Payment> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        if (!Enum.IsDefined(request.Method))
            throw new InvalidOperationException("Select a valid payment method.");

        var employee = await _db.Employees.SingleOrDefaultAsync(e => e.Id == request.EmployeeId && e.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("The employee account is missing or inactive.");
        if (employee.Role == EmployeeRole.RetailStaff)
        {
            if (request.RetailShopId is null || employee.RetailShopId != request.RetailShopId ||
                !await _db.RetailShops.AnyAsync(s => s.Id == request.RetailShopId && s.IsActive, cancellationToken))
                throw new InvalidOperationException("A retail payment must be recorded at the employee's active shop.");
        }
        else if (employee.Role != EmployeeRole.Accounts || request.RetailShopId is not null)
            throw new InvalidOperationException("Payments may only be recorded by Accounts Employees or Retail Employees.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var bill = await _db.Bills.Include(b => b.Payments).Include(b => b.Connection).ThenInclude(c => c.Order)
            .SingleOrDefaultAsync(b => b.Id == request.BillId, cancellationToken)
            ?? throw new InvalidOperationException("Bill not found.");
        if (bill.Status == BillStatus.Cancelled)
            throw new InvalidOperationException("A cancelled bill cannot accept payments.");
        if (employee.Role == EmployeeRole.RetailStaff && bill.Connection.Order.RetailShopId is int orderShopId
            && orderShopId != request.RetailShopId)
            throw new InvalidOperationException("Retail staff can record payment only for an order handled by their shop.");

        decimal paid = bill.Payments.Sum(p => p.Amount);
        decimal outstanding = RoundMoney(bill.TotalAmount - paid);
        if (request.Amount > outstanding)
            throw new InvalidOperationException($"Payment exceeds the outstanding balance of {outstanding:0.00}.");

        var payment = new Payment
        {
            BillId = bill.Id,
            Amount = RoundMoney(request.Amount),
            Method = request.Method,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            ReceivedByEmployeeId = employee.Id,
            RetailShopId = request.RetailShopId
        };
        _db.Payments.Add(payment);
        decimal remaining = RoundMoney(outstanding - payment.Amount);
        bill.Status = remaining == 0m ? BillStatus.Paid : BillStatus.PartiallyPaid;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }

    // Amount still due on one bill.
    public async Task<decimal> GetOutstandingAsync(int billId, CancellationToken cancellationToken = default)
    {
        var bill = await _db.Bills.Where(b => b.Id == billId)
            .Select(b => new { b.TotalAmount, Paid = b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Bill not found.");
        return RoundMoney(Math.Max(0m, bill.TotalAmount - bill.Paid));
    }

    // What the next bill of a connection will charge, shown to Accounts before generating it.
    public async Task<BillPreview> PreviewAsync(int connectionId, DateOnly periodStart, CancellationToken cancellationToken = default)
    {
        var connection = await _db.Connections.AsNoTracking().Include(c => c.Plan)
            .SingleOrDefaultAsync(c => c.Id == connectionId, cancellationToken)
            ?? throw new InvalidOperationException("Connection not found.");
        DateOnly? paidThrough = await PlanPaidThroughAsync(connection, cancellationToken);
        bool planFeeDue = paidThrough is null || periodStart > paidThrough.Value;
        decimal replacements = await _db.ConnectionProducts
            .Where(cp => cp.ConnectionId == connectionId && cp.IsReplacement && cp.BilledOnBillId == null)
            .SumAsync(cp => (decimal?)cp.ReplacementChargeAmount, cancellationToken) ?? 0m;
        bool landline = connection.ConnectionType == ConnectionType.Telephone;
        return new BillPreview(connection.AccountId, connection.Plan.Name, planFeeDue ? connection.Plan.Price : 0m, paidThrough,
            RoundMoney(replacements), await PreviousBalanceAsync(connectionId, cancellationToken),
            landline ? connection.Plan.LocalCallRatePerMinute : null,
            landline ? connection.Plan.StdCallRatePerMinute : null,
            landline ? connection.Plan.MobileMessagingRatePerMinute : null);
    }

    /// <summary>
    /// Last day already covered by a plan fee for the connection's current plan, or null when the fee has
    /// never been charged for it (new connection, or the plan was changed).
    /// </summary>
    private async Task<DateOnly?> PlanPaidThroughAsync(Connection connection, CancellationToken cancellationToken)
    {
        return await _db.Bills
            .Where(b => b.ConnectionId == connection.Id && b.Status != BillStatus.Cancelled
                && b.BilledPlanId == connection.PlanId && b.PlanValidUntil != null)
            .OrderByDescending(b => b.PlanValidUntil)
            .Select(b => b.PlanValidUntil)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Last day of a plan fee that starts on <paramref name="start"/> and is valid for the given months.</summary>
    private static DateOnly PlanEnd(DateOnly start, int validityMonths) => start.AddMonths(Math.Max(1, validityMonths)).AddDays(-1);

    /// <summary>Unpaid amount of the connection's earlier (not cancelled) bills.</summary>
    private async Task<decimal> PreviousBalanceAsync(int connectionId, CancellationToken cancellationToken)
    {
        var balances = await _db.Bills
            .Where(b => b.ConnectionId == connectionId && b.Status != BillStatus.Cancelled && b.Status != BillStatus.Paid)
            .Select(b => b.TotalAmount - (b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m))
            .ToListAsync(cancellationToken);
        return RoundMoney(balances.Where(x => x > 0).Sum());
    }

    /// <summary>
    /// Call charges = minutes x the plan's per-minute rate (local, STD, messaging for mobiles).
    /// Only landline plans have call rates; minutes entered for a rate the plan does not have are rejected.
    /// </summary>
    private static decimal CallCharge(Connection connection, int localMinutes, int stdMinutes, int mobileMinutes)
    {
        if (connection.ConnectionType != ConnectionType.Telephone)
        {
            if (localMinutes + stdMinutes + mobileMinutes > 0)
                throw new InvalidOperationException("Call minutes apply only to landline connections. Use 'Other usage' for internet usage.");
            return 0m;
        }

        Plan plan = connection.Plan;
        decimal total = 0m;
        total += Minutes(localMinutes, plan.LocalCallRatePerMinute, "local call");
        total += Minutes(stdMinutes, plan.StdCallRatePerMinute, "STD call");
        total += Minutes(mobileMinutes, plan.MobileMessagingRatePerMinute, "messaging for mobiles");
        return RoundMoney(total);

        static decimal Minutes(int minutes, decimal? rate, string name)
        {
            if (minutes == 0) return 0m;
            if (rate is null)
                throw new InvalidOperationException($"This plan has no {name} rate, so {name} minutes cannot be billed on it.");
            return minutes * rate.Value;
        }
    }

    private static decimal RoundMoney(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
