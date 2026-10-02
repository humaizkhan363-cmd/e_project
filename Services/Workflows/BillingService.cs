using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Services.Workflows;

public sealed class BillingService : IBillingService
{
    private const decimal ServiceTaxRatePercent = 12.24m;
    private readonly AppDbContext _db;

    public BillingService(AppDbContext db) => _db = db;

    public async Task<Bill> GenerateBillAsync(GenerateBillRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == request.AccountsEmployeeId && e.IsActive && e.Role == EmployeeRole.Accounts, cancellationToken))
            throw new InvalidOperationException("An active Accounts Employee account is required to generate a bill.");
        if (request.PeriodEnd < request.PeriodStart)
            throw new InvalidOperationException("The billing period end must be on or after its start.");
        if (request.DueDate < request.IssueDate)
            throw new InvalidOperationException("The due date must be on or after the issue date.");
        if (request.UsageCharge < 0 || request.ReplacementCharge < 0)
            throw new InvalidOperationException("Charges cannot be negative.");

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
        decimal planCharge = connection.Plan.Price;
        decimal securityDeposit = firstBill ? connection.SecurityDepositAmount : 0m;
        decimal discount = firstBill && connection.Order.DiscountScheme?.AppliesToAdvance == true
            ? RoundMoney(planCharge * connection.Order.DiscountPercent / 100m)
            : 0m;
        decimal subTotal = RoundMoney(planCharge + request.UsageCharge + securityDeposit + request.ReplacementCharge - discount);
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
            UsageCharge = RoundMoney(request.UsageCharge),
            SecurityDepositCharge = RoundMoney(securityDeposit),
            ReplacementCharge = RoundMoney(request.ReplacementCharge),
            DiscountAmount = discount,
            SubTotal = subTotal,
            ServiceTaxRate = ServiceTaxRatePercent,
            ServiceTaxAmount = RoundMoney(subTotal * ServiceTaxRatePercent / 100m),
            Status = BillStatus.Issued,
            GeneratedByEmployeeId = request.AccountsEmployeeId
        };
        bill.TotalAmount = RoundMoney(bill.SubTotal + bill.ServiceTaxAmount);
        _db.Bills.Add(bill);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return bill;
    }

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

    public async Task<decimal> GetOutstandingAsync(int billId, CancellationToken cancellationToken = default)
    {
        var bill = await _db.Bills.Where(b => b.Id == billId)
            .Select(b => new { b.TotalAmount, Paid = b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Bill not found.");
        return RoundMoney(Math.Max(0m, bill.TotalAmount - bill.Paid));
    }

    private static decimal RoundMoney(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
