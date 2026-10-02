using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Identifiers;

namespace NexusServiceMarketingSystem.Services.Workflows;

public sealed class OrderWorkflowService : IOrderWorkflowService
{
    private readonly AppDbContext _db;
    private readonly IIdentifierGenerator _identifiers;

    public OrderWorkflowService(AppDbContext db, IIdentifierGenerator identifiers)
    {
        _db = db;
        _identifiers = identifiers;
    }

    public async Task<Order> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity < 1 || request.Quantity > 1000)
            throw new InvalidOperationException("The requested connection quantity is outside the allowed range.");
        if (string.IsNullOrWhiteSpace(request.InstallationAddress) || request.InstallationAddress.Trim().Length > 300)
            throw new InvalidOperationException("Enter a valid installation address.");
        if ((request.RetailShopId is null) != (request.PlacedByEmployeeId is null))
            throw new InvalidOperationException("A staff order must have both its retail shop and employee recorded.");

        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == request.CustomerId && c.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("The selected customer is missing or inactive.");
        var plan = await _db.Plans.SingleOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("The selected plan is missing or inactive.");
        if (plan.ConnectionType != request.ConnectionType)
            throw new InvalidOperationException("The selected plan does not match the requested service type.");
        if (!await _db.Cities.AnyAsync(c => c.Id == request.CityId && c.IsActive, cancellationToken))
            throw new InvalidOperationException("Select an active installation city.");

        if (request.PlacedByEmployeeId is int employeeId && request.RetailShopId is int shopId)
        {
            bool validStaffOrder = await _db.Employees.AnyAsync(e => e.Id == employeeId && e.IsActive && e.Role == EmployeeRole.RetailStaff && e.RetailShopId == shopId, cancellationToken)
                && await _db.RetailShops.AnyAsync(s => s.Id == shopId && s.IsActive, cancellationToken);
            if (!validStaffOrder)
                throw new InvalidOperationException("Only an active employee assigned to the selected active retail shop may place a shop order.");
        }

        var qualifyingSchemes = await _db.DiscountSchemes
            .Where(d => d.IsActive && d.MinConnections <= request.Quantity && (d.MaxConnections == null || d.MaxConnections >= request.Quantity))
            .OrderBy(d => d.Id)
            .ToListAsync(cancellationToken);
        if (qualifyingSchemes.Count > 1)
            throw new InvalidOperationException("More than one active discount scheme matches this quantity. Ask an Admin to resolve the overlapping scheme bands.");
        var scheme = qualifyingSchemes.SingleOrDefault();

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var order = new Order
        {
            CustomerId = customer.Id,
            ConnectionType = request.ConnectionType,
            PlanId = plan.Id,
            CityId = request.CityId,
            InstallationAddress = request.InstallationAddress.Trim(),
            Quantity = request.Quantity,
            RetailShopId = request.RetailShopId,
            PlacedByEmployeeId = request.PlacedByEmployeeId,
            DiscountSchemeId = scheme?.Id,
            DiscountPercent = scheme?.DiscountPercent ?? 0m,
            Status = OrderStatus.Placed
        };
        await _identifiers.AssignOrderNumberAsync(order, cancellationToken);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return order;
    }

    public async Task<IReadOnlyList<FeasibilityCheck>> StartFeasibilityAsync(int orderId, int technicalEmployeeId, CancellationToken cancellationToken = default)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == technicalEmployeeId && e.IsActive && e.Role == EmployeeRole.Technical, cancellationToken))
            throw new InvalidOperationException("An active Technical Employee account is required.");

        var order = await _db.Orders.Include(o => o.FeasibilityChecks).SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");
        if (order.Status is not (OrderStatus.Placed or OrderStatus.UnderFeasibilityCheck))
            throw new InvalidOperationException("Only a placed order can enter or resume a feasibility check.");

        bool hasNexusLandline = await _db.Connections.AnyAsync(c => c.CustomerId == order.CustomerId
            && c.ConnectionType == ConnectionType.Telephone && c.Status == ConnectionStatus.Active, cancellationToken);
        var requiredTypes = order.ConnectionType switch
        {
            ConnectionType.Telephone => new[] { FeasibilityCheckType.Landline },
            ConnectionType.Broadband => new[] { FeasibilityCheckType.Internet },
            ConnectionType.DialUp when hasNexusLandline => new[] { FeasibilityCheckType.Internet },
            ConnectionType.DialUp => new[] { FeasibilityCheckType.Landline, FeasibilityCheckType.Internet },
            _ => throw new InvalidOperationException("Unknown service type.")
        };

        foreach (FeasibilityCheckType type in requiredTypes)
        {
            if (!order.FeasibilityChecks.Any(f => f.CheckType == type))
                _db.FeasibilityChecks.Add(new FeasibilityCheck { OrderId = order.Id, CheckType = type, Status = FeasibilityStatus.Pending });
        }
        order.Status = OrderStatus.UnderFeasibilityCheck;
        order.StatusChangedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await _db.FeasibilityChecks.Where(f => f.OrderId == order.Id).OrderBy(f => f.CheckType).ToListAsync(cancellationToken);
    }

    public async Task<FeasibilityCheck> CompleteFeasibilityAsync(int checkId, int technicalEmployeeId, FeasibilityStatus status, decimal? distanceKm, bool? serverAvailable, string? remarks, CancellationToken cancellationToken = default)
    {
        if (status == FeasibilityStatus.Pending)
            throw new InvalidOperationException("Choose Feasible or Not Feasible to complete the check.");
        if (distanceKm < 0)
            throw new InvalidOperationException("Distance cannot be negative.");
        if (!await _db.Employees.AnyAsync(e => e.Id == technicalEmployeeId && e.IsActive && e.Role == EmployeeRole.Technical, cancellationToken))
            throw new InvalidOperationException("An active Technical Employee account is required.");

        var check = await _db.FeasibilityChecks.Include(f => f.Order).ThenInclude(o => o.FeasibilityChecks)
            .SingleOrDefaultAsync(f => f.Id == checkId, cancellationToken)
            ?? throw new InvalidOperationException("Feasibility check not found.");
        if (check.Status != FeasibilityStatus.Pending)
            throw new InvalidOperationException("This feasibility check has already been completed.");
        if (check.CheckType == FeasibilityCheckType.Internet && serverAvailable is null)
            throw new InvalidOperationException("Record whether internet server capacity is available.");

        check.Status = status;
        check.DistanceKm = distanceKm;
        check.ServerAvailable = check.CheckType == FeasibilityCheckType.Internet ? serverAvailable : null;
        check.Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
        check.CheckedByEmployeeId = technicalEmployeeId;
        check.CheckedAtUtc = DateTime.UtcNow;

        var order = check.Order;
        if (order.FeasibilityChecks.Any(f => f.Status == FeasibilityStatus.NotFeasible))
            order.Status = OrderStatus.NotFeasible;
        else if (order.FeasibilityChecks.Count > 0 && order.FeasibilityChecks.All(f => f.Status == FeasibilityStatus.Feasible))
            order.Status = OrderStatus.Feasible;
        else
            order.Status = OrderStatus.UnderFeasibilityCheck;
        order.StatusChangedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return check;
    }
}
