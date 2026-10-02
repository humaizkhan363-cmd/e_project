using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Identifiers;

namespace NexusServiceMarketingSystem.Services.Workflows;

/// <summary>Places orders and runs the feasibility workflow.</summary>
public sealed class OrderWorkflowService : IOrderWorkflowService
{
    private readonly AppDbContext _db;
    private readonly IIdentifierGenerator _identifiers;

    public OrderWorkflowService(AppDbContext db, IIdentifierGenerator identifiers)
    {
        _db = db;
        _identifiers = identifiers;
    }

    // Validates and saves a new order with its generated order number and bulk discount band.
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

        // Dial-up needs a Nexus landline. A customer who has none applies for the telephone line and the
        // dial-up internet together by also choosing a landline plan (both feasibility checks are then run).
        Plan? landlinePlan = await ResolveLandlinePlanAsync(request, cancellationToken);

        if (request.PlacedByEmployeeId is int employeeId && request.RetailShopId is int shopId)
        {
            bool validStaffOrder = await _db.Employees.AnyAsync(e => e.Id == employeeId && e.IsActive && e.Role == EmployeeRole.RetailStaff && e.RetailShopId == shopId, cancellationToken)
                && await _db.RetailShops.AnyAsync(s => s.Id == shopId && s.IsActive, cancellationToken);
            if (!validStaffOrder)
                throw new InvalidOperationException("Only an active employee assigned to the selected active retail shop may place a shop order.");
        }

        // Bulk / corporate scheme: the band depends on ALL connections the customer takes, i.e. the ones
        // already provided (not permanently closed), other open orders, and this order.
        int bulkCount = await CountCustomerConnectionsAsync(customer.Id, cancellationToken)
            + (landlinePlan is null ? request.Quantity : request.Quantity * 2);
        var qualifyingSchemes = await _db.DiscountSchemes
            .Where(d => d.IsActive && d.MinConnections <= bulkCount && (d.MaxConnections == null || d.MaxConnections >= bulkCount))
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
            LandlinePlanId = landlinePlan?.Id,
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

    /// <summary>
    /// Validates the optional landline plan of a dial-up order and returns it (null when not needed).
    /// Rules: only dial-up orders may carry one; it is required when the customer has no active Nexus
    /// landline and not allowed when they already have one (the dial-up is then linked to that line).
    /// </summary>
    private async Task<Plan?> ResolveLandlinePlanAsync(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.ConnectionType != ConnectionType.DialUp)
        {
            if (request.LandlinePlanId is not null)
                throw new InvalidOperationException("A landline plan can only be added to a dial-up order.");
            return null;
        }

        bool hasNexusLandline = await _db.Connections.AnyAsync(c => c.CustomerId == request.CustomerId
            && c.ConnectionType == ConnectionType.Telephone && c.Status == ConnectionStatus.Active, cancellationToken);
        if (hasNexusLandline)
        {
            if (request.LandlinePlanId is not null)
                throw new InvalidOperationException("This customer already has an active Nexus landline, so the dial-up will use it. Leave the landline plan empty.");
            return null;
        }

        if (request.LandlinePlanId is null)
            throw new InvalidOperationException("This customer has no Nexus landline. Dial-up needs one, so also choose a landline plan to apply for the telephone line and dial-up together.");

        return await _db.Plans.SingleOrDefaultAsync(p => p.Id == request.LandlinePlanId && p.IsActive
                   && p.ConnectionType == ConnectionType.Telephone, cancellationToken)
               ?? throw new InvalidOperationException("The selected landline plan is missing, inactive, or not a landline plan.");
    }

    /// <summary>
    /// Number of connections the customer already takes for the bulk-scheme band: provided connections that are
    /// not permanently inactive, plus the quantities of their orders still being processed.
    /// </summary>
    private async Task<int> CountCustomerConnectionsAsync(int customerId, CancellationToken cancellationToken)
    {
        int provided = await _db.Connections.CountAsync(c => c.CustomerId == customerId
            && c.Status != ConnectionStatus.PermanentlyInactive, cancellationToken);
        // A dial-up order that bundles a telephone line will create two connections per unit.
        int pending = await _db.Orders.Where(o => o.CustomerId == customerId
                && (o.Status == OrderStatus.Placed || o.Status == OrderStatus.UnderFeasibilityCheck || o.Status == OrderStatus.Feasible))
            .SumAsync(o => (int?)(o.LandlinePlanId == null ? o.Quantity : o.Quantity * 2), cancellationToken) ?? 0;
        return provided + pending;
    }

    // Creates the checks the order needs: landline, internet, or both.
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
            // Telephone line applied for together with the dial-up: both the line and the internet are checked.
            ConnectionType.DialUp when order.LandlinePlanId is not null => new[] { FeasibilityCheckType.Landline, FeasibilityCheckType.Internet },
            // Customer already holds a Nexus landline: only the internet is checked.
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

    // Records one check result and updates the order status from all its checks.
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
