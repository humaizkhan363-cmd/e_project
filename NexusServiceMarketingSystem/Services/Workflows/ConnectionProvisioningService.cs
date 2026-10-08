using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Identifiers;

namespace NexusServiceMarketingSystem.Services.Workflows;

/// <summary>Creates connections from feasible orders and manages their status.</summary>
public sealed class ConnectionProvisioningService : IConnectionProvisioningService
{
    private readonly AppDbContext _db;
    private readonly IIdentifierGenerator _identifiers;

    public ConnectionProvisioningService(AppDbContext db, IIdentifierGenerator identifiers)
    {
        _db = db;
        _identifiers = identifiers;
    }

    // Creates the order's connections with account IDs, issues equipment from stock and marks the order Connected.
    public async Task<IReadOnlyList<Connection>> ProvisionAsync(ProvisionConnectionRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == request.TechnicalEmployeeId && e.IsActive && e.Role == EmployeeRole.Technical, cancellationToken))
            throw new InvalidOperationException("An active Technical Employee account is required.");

        var order = await _db.Orders.Include(o => o.Plan).Include(o => o.LandlinePlan).Include(o => o.DiscountScheme).Include(o => o.Connections)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");
        if (order.Status != OrderStatus.Feasible)
            throw new InvalidOperationException("A connection can only be created after all required feasibility checks pass.");
        if (order.Connections.Count > 0)
            throw new InvalidOperationException("This order already has a connection. Create only the remaining connections from its details.");

        bool requiresEquipment = order.ConnectionType is ConnectionType.DialUp or ConnectionType.Broadband;
        Product? product = null;
        if (requiresEquipment)
        {
            if (request.ProductId is null)
                throw new InvalidOperationException("Select a modem, router, or other equipment for the internet connection.");
            product = await _db.Products.SingleOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("The selected equipment is unavailable.");
            if (product.StockQuantity < order.Quantity)
                throw new InvalidOperationException($"Insufficient stock. {product.StockQuantity} unit(s) are available, but {order.Quantity} are required.");
            if (order.Quantity > 1 && !string.IsNullOrWhiteSpace(request.SerialNumber))
                throw new InvalidOperationException("Enter device serial numbers individually after provisioning, or omit the serial for a multi-connection order.");
        }
        else if (request.ProductId is not null)
            throw new InvalidOperationException("Telephone-only connections do not require internet equipment.");

        // Dial-up applied for together with a new telephone line: the line is created here, one per dial-up connection.
        bool bundledLandline = order.ConnectionType == ConnectionType.DialUp && order.LandlinePlanId is not null;

        Connection? landline = null;
        if (order.ConnectionType == ConnectionType.DialUp && !bundledLandline)
        {
            landline = await _db.Connections.SingleOrDefaultAsync(c => c.Id == request.LandlineConnectionId
                && c.CustomerId == order.CustomerId && c.ConnectionType == ConnectionType.Telephone && c.Status == ConnectionStatus.Active, cancellationToken);
            if (landline is null)
                throw new InvalidOperationException("Choose the customer's active Nexus landline for this dial-up connection.");
        }
        else if (request.LandlineConnectionId is not null)
            throw new InvalidOperationException(bundledLandline
                ? "This order includes a new telephone line, so do not choose an existing landline."
                : "Only a dial-up connection can be linked to a Nexus landline.");

        // Every new telephone line gets its own number: one number per connection.
        bool needsPhoneNumbers = order.ConnectionType == ConnectionType.Telephone || bundledLandline;
        IReadOnlyList<string> phoneNumbers = Array.Empty<string>();
        if (needsPhoneNumbers)
            phoneNumbers = ParsePhoneNumbers(request.PhoneNumber, order.Quantity);
        else if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new InvalidOperationException("A telephone number is only assigned to a new telephone line.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var connections = new List<Connection>();
        decimal deposit = DiscountedDeposit(order, order.Plan.SecurityDeposit);

        for (int i = 0; i < order.Quantity; i++)
        {
            Connection? lineForDialUp = landline;
            if (bundledLandline)
            {
                // Create the bundled telephone line first, then link the dial-up connection to it.
                lineForDialUp = new Connection
                {
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    PlanId = order.LandlinePlanId!.Value,
                    ConnectionType = ConnectionType.Telephone,
                    CityId = order.CityId,
                    InstallationAddress = order.InstallationAddress,
                    PhoneNumber = phoneNumbers[i],
                    Status = ConnectionStatus.Active,
                    SecurityDepositAmount = DiscountedDeposit(order, order.LandlinePlan!.SecurityDeposit),
                    CreatedByEmployeeId = request.TechnicalEmployeeId,
                    StatusChangedAtUtc = DateTime.UtcNow
                };
                await _identifiers.AssignAccountIdAsync(lineForDialUp, cancellationToken);
                _db.Connections.Add(lineForDialUp);
                connections.Add(lineForDialUp);
            }

            var connection = new Connection
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                PlanId = order.PlanId,
                ConnectionType = order.ConnectionType,
                CityId = order.CityId,
                InstallationAddress = order.InstallationAddress,
                PhoneNumber = order.ConnectionType == ConnectionType.Telephone ? phoneNumbers[i] : null,
                LandlineConnection = lineForDialUp,
                Status = ConnectionStatus.Active,
                SecurityDepositAmount = deposit,
                CreatedByEmployeeId = request.TechnicalEmployeeId,
                StatusChangedAtUtc = DateTime.UtcNow
            };
            await _identifiers.AssignAccountIdAsync(connection, cancellationToken);
            _db.Connections.Add(connection);
            if (product is not null)
            {
                _db.ConnectionProducts.Add(new ConnectionProduct
                {
                    Connection = connection,
                    ProductId = product.Id,
                    Quantity = 1,
                    SerialNumber = i == 0 ? request.SerialNumber?.Trim() : null,
                    IsReplacement = false,
                    Notes = "Issued during connection activation."
                });
            }
            connections.Add(connection);
        }

        if (product is not null)
            product.StockQuantity -= order.Quantity;
        order.Status = OrderStatus.Connected;
        order.StatusChangedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return connections;
    }

    /// <summary>Security deposit after the order's bulk discount (when the scheme covers the deposit).</summary>
    private static decimal DiscountedDeposit(Order order, decimal deposit)
    {
        if (order.DiscountScheme?.AppliesToSecurityDeposit != true)
            return deposit;
        return decimal.Round(deposit * (100m - order.DiscountPercent) / 100m, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Splits the entered telephone numbers (separated by commas, semicolons or new lines) and checks that
    /// there is exactly one valid, distinct number per connection.
    /// </summary>
    private static IReadOnlyList<string> ParsePhoneNumbers(string? input, int quantity)
    {
        var numbers = (input ?? string.Empty)
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (numbers.Count != quantity)
            throw new InvalidOperationException(quantity == 1
                ? "Enter the telephone number assigned to the new line."
                : $"Enter {quantity} telephone numbers (one per new line), separated by commas.");
        foreach (string number in numbers)
        {
            bool valid = number.Length is >= 5 and <= 20
                && number.All(ch => char.IsAsciiDigit(ch) || ch is '+' or '-' or ' ' or '(' or ')')
                && number.Count(char.IsAsciiDigit) >= 5;
            if (!valid)
                throw new InvalidOperationException($"'{number}' is not a valid telephone number.");
        }
        if (numbers.Distinct(StringComparer.Ordinal).Count() != numbers.Count)
            throw new InvalidOperationException("Each new line needs a different telephone number.");
        return numbers;
    }

    // Changes a connection's status; a permanently inactive connection can never be reactivated.
    public async Task ChangeStatusAsync(int connectionId, int technicalEmployeeId, ConnectionStatus status, CancellationToken cancellationToken = default)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == technicalEmployeeId && e.IsActive && e.Role == EmployeeRole.Technical, cancellationToken))
            throw new InvalidOperationException("An active Technical Employee account is required.");
        var connection = await _db.Connections.SingleOrDefaultAsync(c => c.Id == connectionId, cancellationToken)
            ?? throw new InvalidOperationException("Connection not found.");
        if (connection.Status == ConnectionStatus.PermanentlyInactive && status != ConnectionStatus.PermanentlyInactive)
            throw new InvalidOperationException("A permanently inactive connection cannot be reactivated.");
        connection.Status = status;
        connection.StatusChangedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    // Active connections that have at least one overdue bill, oldest due date first.
    public async Task<IReadOnlyList<OverdueConnection>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var rows = await BillingStatusAsync(cancellationToken);
        return rows.Where(x => x.Status == ConnectionStatus.Active && x.OverdueBills > 0)
            .OrderBy(x => x.OldestDueDate).Select(x => x.ToRecord()).ToList();
    }

    // Temporarily inactive connections with nothing left to pay.
    public async Task<IReadOnlyList<OverdueConnection>> GetClearedSuspendedAsync(CancellationToken cancellationToken = default)
    {
        var rows = await BillingStatusAsync(cancellationToken);
        return rows.Where(x => x.Status == ConnectionStatus.TemporarilyInactive && x.UnpaidAmount <= 0)
            .OrderBy(x => x.AccountId).Select(x => x.ToRecord()).ToList();
    }

    // Sets overdue connections temporarily inactive (one, or all of them).
    public async Task<int> SuspendOverdueAsync(int technicalEmployeeId, int? connectionId, CancellationToken cancellationToken = default)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == technicalEmployeeId && e.IsActive && e.Role == EmployeeRole.Technical, cancellationToken))
            throw new InvalidOperationException("An active Technical Employee account is required.");

        // Re-check the rule on the server: only active connections that still have overdue bills are suspended.
        List<int> ids = (await GetOverdueAsync(cancellationToken)).Select(x => x.ConnectionId)
            .Where(id => connectionId is null || id == connectionId).ToList();
        if (ids.Count == 0)
            return 0;

        var connections = await _db.Connections.Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken);
        foreach (Connection connection in connections)
        {
            connection.Status = ConnectionStatus.TemporarilyInactive;
            connection.StatusChangedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return connections.Count;
    }

    /// <summary>
    /// Billing position of every connection that is not permanently closed: number and amount of bills that are
    /// past their due date and not fully paid, and the total unpaid amount. Cancelled bills are ignored.
    /// The open bills are loaded first and summed here, because SQL Server cannot aggregate over the payments subquery.
    /// </summary>
    private async Task<List<BillingStatusRow>> BillingStatusAsync(CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        var connections = await _db.Connections.AsNoTracking()
            .Where(c => c.Status != ConnectionStatus.PermanentlyInactive)
            .Select(c => new { c.Id, c.AccountId, c.Customer.FullName, c.Customer.Phone, c.Status })
            .ToListAsync(cancellationToken);
        var openBills = (await _db.Bills.AsNoTracking()
                .Where(b => b.Status != BillStatus.Cancelled && b.Status != BillStatus.Paid
                    && b.Connection.Status != ConnectionStatus.PermanentlyInactive)
                .Select(b => new { b.ConnectionId, b.DueDate, b.TotalAmount, Paid = b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m })
                .ToListAsync(cancellationToken))
            .Select(b => new { b.ConnectionId, b.DueDate, Due = b.TotalAmount - b.Paid })
            .Where(b => b.Due > 0)
            .ToLookup(b => b.ConnectionId);

        return connections.Select(c =>
        {
            var unpaid = openBills[c.Id].ToList();
            var overdue = unpaid.Where(b => b.DueDate < today).ToList();
            return new BillingStatusRow
            {
                ConnectionId = c.Id,
                AccountId = c.AccountId,
                CustomerName = c.FullName,
                CustomerPhone = c.Phone,
                Status = c.Status,
                OverdueBills = overdue.Count,
                OverdueAmount = overdue.Sum(b => b.Due),
                OldestDueDate = overdue.Count == 0 ? null : overdue.Min(b => b.DueDate),
                UnpaidAmount = unpaid.Sum(b => b.Due)
            };
        }).ToList();
    }

    /// <summary>Row of <see cref="BillingStatusAsync"/>; converts to the public record.</summary>
    private sealed class BillingStatusRow
    {
        public int ConnectionId { get; init; }
        public string AccountId { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string CustomerPhone { get; init; } = string.Empty;
        public ConnectionStatus Status { get; init; }
        public int OverdueBills { get; init; }
        public decimal OverdueAmount { get; init; }
        public DateOnly? OldestDueDate { get; init; }
        public decimal UnpaidAmount { get; init; }

        public OverdueConnection ToRecord() =>
            new(ConnectionId, AccountId, CustomerName, CustomerPhone, Status, OverdueBills, OverdueAmount, OldestDueDate);
    }
}
