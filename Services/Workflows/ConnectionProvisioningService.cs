using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Identifiers;

namespace NexusServiceMarketingSystem.Services.Workflows;

public sealed class ConnectionProvisioningService : IConnectionProvisioningService
{
    private readonly AppDbContext _db;
    private readonly IIdentifierGenerator _identifiers;

    public ConnectionProvisioningService(AppDbContext db, IIdentifierGenerator identifiers)
    {
        _db = db;
        _identifiers = identifiers;
    }

    public async Task<IReadOnlyList<Connection>> ProvisionAsync(ProvisionConnectionRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == request.TechnicalEmployeeId && e.IsActive && e.Role == EmployeeRole.Technical, cancellationToken))
            throw new InvalidOperationException("An active Technical Employee account is required.");

        var order = await _db.Orders.Include(o => o.Plan).Include(o => o.DiscountScheme).Include(o => o.Connections)
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

        Connection? landline = null;
        if (order.ConnectionType == ConnectionType.DialUp)
        {
            landline = await _db.Connections.SingleOrDefaultAsync(c => c.Id == request.LandlineConnectionId
                && c.CustomerId == order.CustomerId && c.ConnectionType == ConnectionType.Telephone && c.Status == ConnectionStatus.Active, cancellationToken);
            if (landline is null)
                throw new InvalidOperationException("Dial-up requires an active Nexus telephone connection for this customer. Provision a telephone order first if needed.");
        }
        else if (request.LandlineConnectionId is not null)
            throw new InvalidOperationException("Only a dial-up connection can be linked to a Nexus landline.");

        if (order.ConnectionType == ConnectionType.Telephone && string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new InvalidOperationException("Enter the telephone number assigned to the customer.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var connections = new List<Connection>();
        decimal deposit = order.Plan.SecurityDeposit;
        if (order.DiscountScheme?.AppliesToSecurityDeposit == true)
            deposit = decimal.Round(deposit * (100m - order.DiscountPercent) / 100m, 2, MidpointRounding.AwayFromZero);

        for (int i = 0; i < order.Quantity; i++)
        {
            var connection = new Connection
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                PlanId = order.PlanId,
                ConnectionType = order.ConnectionType,
                CityId = order.CityId,
                InstallationAddress = order.InstallationAddress,
                PhoneNumber = order.ConnectionType == ConnectionType.Telephone ? request.PhoneNumber?.Trim() : null,
                LandlineConnectionId = landline?.Id,
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
}
