using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Dashboard;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Services.Workflows;

namespace NexusServiceMarketingSystem.Services.Dashboard;

/// <summary>
/// Read-only figures for the role dashboards: counters, the "needs attention" feed and charts.
/// Every query is AsNoTracking and limited to what the signed-in role may see
/// (a retail employee only sees their own shop, a customer only their own account).
/// </summary>
public interface IDashboardService
{
    Task<DashboardViewModel> AdminAsync(string name, CancellationToken ct = default);
    Task<DashboardViewModel> RetailAsync(int employeeId, string name, CancellationToken ct = default);
    Task<DashboardViewModel> TechnicalAsync(string name, CancellationToken ct = default);
    Task<DashboardViewModel> AccountsAsync(string name, CancellationToken ct = default);
    Task<DashboardViewModel> CustomerAsync(int customerId, string name, CancellationToken ct = default);
}

public sealed class DashboardService(AppDbContext db, IConnectionProvisioningService provisioning) : IDashboardService
{
    private static readonly OrderStatus[] OpenStatuses = { OrderStatus.Placed, OrderStatus.UnderFeasibilityCheck, OrderStatus.Feasible };

    private static DateTime MonthStartUtc => new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime TodayStartUtc => DateTime.UtcNow.Date;

    public async Task<DashboardViewModel> AdminAsync(string name, CancellationToken ct = default)
    {
        int customers = await db.Customers.CountAsync(c => c.IsActive, ct);
        int openOrders = await db.Orders.CountAsync(o => OpenStatuses.Contains(o.Status), ct);
        int active = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Active, ct);
        decimal collected = await db.Payments.Where(p => p.PaidAtUtc >= MonthStartUtc).SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        decimal outstanding = await OutstandingAsync(db.Bills, ct);
        int lowStock = await db.Products.CountAsync(p => p.IsActive && p.StockQuantity <= p.ReorderLevel, ct);

        var m = new DashboardViewModel
        {
            Eyebrow = "Administration",
            Title = "Admin Dashboard",
            Lead = $"Welcome back, {name}. Here is how Nexus is doing today.",
            FeedTitle = "Latest orders",
            FeedMore = new FeedLink("All orders", "Admin", "Operations", "Orders"),
            ChartTitle = "Orders by status"
        };
        m.Kpis.Add(new KpiCard("Active customers", customers, "users", "info", "Registered and active", "Admin", "Customers", "Index"));
        m.Kpis.Add(new KpiCard("Orders in progress", openOrders, "clipboard", "violet", "Placed, under check or feasible", "Admin", "Operations", "Orders"));
        m.Kpis.Add(new KpiCard("Active connections", active, "plug", "ok", "Dial-Up, Broadband & Landline", "Admin", "Operations", "Search"));
        m.Kpis.Add(new KpiCard("Collected this month", collected, "money", "ok", "All shops and Accounts", "Admin", "Operations", "Reports", "$", 2));
        m.Kpis.Add(new KpiCard("Outstanding", outstanding, "receipt", outstanding > 0 ? "warn" : "ok", "Unpaid on issued bills", "Admin", "Operations", "Reports", "$", 2));
        m.Kpis.Add(new KpiCard("Low stock items", lowStock, "box", lowStock > 0 ? "bad" : "ok", "At or below reorder level", "Admin", "Products", "Index"));

        await AddOrderFeedAsync(m, db.Orders, "Admin", "Operations", "Orders", withId: false, ct);
        await AddStatusChartAsync(m, db.Orders, ct);

        m.Actions.Add(new QuickAction("New employee", "users", "Admin", "Employees", "Create"));
        m.Actions.Add(new QuickAction("New plan", "layers", "Admin", "Plans", "Create"));
        m.Actions.Add(new QuickAction("Record purchase", "cart", "Admin", "Purchases", "Create"));
        m.Actions.Add(new QuickAction("New customer", "user", "Admin", "Customers", "Create"));
        m.Actions.Add(new QuickAction("Reports", "chart", "Admin", "Operations", "Reports"));
        return m;
    }

    public async Task<DashboardViewModel> RetailAsync(int employeeId, string name, CancellationToken ct = default)
    {
        var shop = await db.Employees.AsNoTracking().Where(e => e.Id == employeeId).Select(e => e.RetailShop).SingleOrDefaultAsync(ct);
        int shopId = shop?.Id ?? 0;
        IQueryable<Order> orders = db.Orders.Where(o => o.RetailShopId == shopId);

        int today = await orders.CountAsync(o => o.PlacedAtUtc >= TodayStartUtc, ct);
        int open = await orders.CountAsync(o => OpenStatuses.Contains(o.Status), ct);
        int connections = await db.Connections.CountAsync(c => c.Order.RetailShopId == shopId && c.Status == ConnectionStatus.Active, ct);
        decimal collectedToday = await db.Payments.Where(p => p.RetailShopId == shopId && p.PaidAtUtc >= TodayStartUtc).SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        decimal toCollect = await OutstandingAsync(db.Bills.Where(b => b.Connection.Order.RetailShopId == null || b.Connection.Order.RetailShopId == shopId), ct);

        var m = new DashboardViewModel
        {
            Eyebrow = shop is null ? "Retail outlet" : shop.Name,
            Title = "Retail Dashboard",
            Lead = $"Welcome, {name}. Register customers, place orders and collect payments for your shop.",
            FeedTitle = "Latest orders at this shop",
            FeedMore = new FeedLink("All orders", "Retail", "Orders", "Index"),
            ChartTitle = "Shop orders by status"
        };
        m.Kpis.Add(new KpiCard("Orders today", today, "clipboard", "info", "Placed at this shop", "Retail", "Orders", "Index"));
        m.Kpis.Add(new KpiCard("Open orders", open, "check", "violet", "Waiting for Technical", "Retail", "Orders", "Index"));
        m.Kpis.Add(new KpiCard("Active connections", connections, "plug", "ok", "Provided through this shop", "Retail", "Connections", "Index"));
        m.Kpis.Add(new KpiCard("Collected today", collectedToday, "money", "ok", "Payments at this shop", "Retail", "Payments", "History", "$", 2));
        m.Kpis.Add(new KpiCard("Still to collect", toCollect, "wallet", toCollect > 0 ? "warn" : "ok", "Unpaid bills you may collect", "Retail", "Payments", "Index", "$", 2));

        await AddOrderFeedAsync(m, orders, "Retail", "Orders", "Details", withId: true, ct);
        await AddStatusChartAsync(m, orders, ct);

        m.Actions.Add(new QuickAction("New customer", "user", "Retail", "Customers", "Create"));
        m.Actions.Add(new QuickAction("New order", "plus", "Retail", "Orders", "Create"));
        m.Actions.Add(new QuickAction("Collect payment", "wallet", "Retail", "Payments", "Index"));
        m.Actions.Add(new QuickAction("Plans & prices", "tag", "", "Catalog", "Plans"));
        return m;
    }

    public async Task<DashboardViewModel> TechnicalAsync(string name, CancellationToken ct = default)
    {
        int waiting = await db.Orders.CountAsync(o => o.Status == OrderStatus.Placed || o.Status == OrderStatus.UnderFeasibilityCheck, ct);
        int ready = await db.Orders.CountAsync(o => o.Status == OrderStatus.Feasible, ct);
        int active = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Active, ct);
        int overdue = (await provisioning.GetOverdueAsync(ct)).Count;
        int lowStock = await db.Products.CountAsync(p => p.IsActive && p.StockQuantity <= p.ReorderLevel, ct);

        var m = new DashboardViewModel
        {
            Eyebrow = "Technical team",
            Title = "Technical Dashboard",
            Lead = $"Welcome, {name}. Work the feasibility queue, create connections and look after equipment.",
            FeedTitle = "Feasibility queue",
            FeedMore = new FeedLink("Open queue", "Technical", "Feasibility", "Index"),
            ChartTitle = "Connections by type"
        };
        m.Kpis.Add(new KpiCard("Waiting for check", waiting, "check", waiting > 0 ? "warn" : "ok", "Placed or under feasibility", "Technical", "Feasibility", "Index"));
        m.Kpis.Add(new KpiCard("Ready to connect", ready, "plug", "violet", "Feasible orders", "Technical", "Connections", "Index"));
        m.Kpis.Add(new KpiCard("Active connections", active, "radar", "ok", "Currently in service", "Technical", "Connections", "Index"));
        m.Kpis.Add(new KpiCard("Overdue connections", overdue, "alert", overdue > 0 ? "bad" : "ok", "Unpaid past due date", "Technical", "Connections", "Overdue"));
        m.Kpis.Add(new KpiCard("Low stock items", lowStock, "box", lowStock > 0 ? "bad" : "ok", "Modems, routers, handsets", "Technical", "Products", "Index"));

        await AddOrderFeedAsync(m, db.Orders.Where(o => OpenStatuses.Contains(o.Status)), "Technical", "Feasibility", "Index", withId: false, ct);
        var byType = await db.Connections.Where(c => c.Status != ConnectionStatus.PermanentlyInactive)
            .GroupBy(c => c.ConnectionType).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        foreach (ConnectionType t in Enum.GetValues<ConnectionType>())
            m.Chart.Add(new ChartBar(TypeName(t), byType.FirstOrDefault(x => x.Key == t)?.Count ?? 0));

        m.Actions.Add(new QuickAction("Feasibility", "check", "Technical", "Feasibility", "Index"));
        m.Actions.Add(new QuickAction("Connections", "plug", "Technical", "Connections", "Index"));
        m.Actions.Add(new QuickAction("Overdue", "alert", "Technical", "Connections", "Overdue"));
        m.Actions.Add(new QuickAction("Equipment", "box", "Technical", "Products", "Index"));
        return m;
    }

    public async Task<DashboardViewModel> AccountsAsync(string name, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        IQueryable<Bill> unpaid = db.Bills.Where(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid);
        int active = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Active, ct);
        int unpaidCount = await unpaid.CountAsync(ct);
        int overdueBills = await unpaid.CountAsync(b => b.DueDate < today, ct);
        decimal collected = await db.Payments.Where(p => p.PaidAtUtc >= MonthStartUtc).SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        decimal outstanding = await OutstandingAsync(db.Bills, ct);

        var m = new DashboardViewModel
        {
            Eyebrow = "Accounts department",
            Title = "Accounts Dashboard",
            Lead = $"Welcome, {name}. Generate bills (12.24% service tax) and record payments received at the office.",
            FeedTitle = "Oldest unpaid bills",
            FeedMore = new FeedLink("All bills", "Accounts", "Bills", "Index"),
            FeedEmpty = "Every bill is paid.",
            ChartTitle = "Bills by status"
        };
        m.Kpis.Add(new KpiCard("Active connections", active, "plug", "info", "Can be billed", "Accounts", "Bills", "Create"));
        m.Kpis.Add(new KpiCard("Unpaid bills", unpaidCount, "receipt", unpaidCount > 0 ? "warn" : "ok", "Issued or partially paid", "Accounts", "Bills", "Index"));
        m.Kpis.Add(new KpiCard("Overdue bills", overdueBills, "alert", overdueBills > 0 ? "bad" : "ok", "Past their due date", "Accounts", "Bills", "Index"));
        m.Kpis.Add(new KpiCard("Collected this month", collected, "money", "ok", "Shops and Accounts", "Accounts", "Bills", "Index", "$", 2));
        m.Kpis.Add(new KpiCard("Outstanding", outstanding, "wallet", outstanding > 0 ? "warn" : "ok", "Total still to be paid", "Accounts", "Bills", "Index", "$", 2));

        var bills = await unpaid.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer)
            .OrderBy(b => b.DueDate).Take(6)
            .Select(b => new { b.Id, b.Connection.AccountId, b.Connection.Customer.FullName, b.DueDate, Due = b.TotalAmount - (b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m) })
            .ToListAsync(ct);
        foreach (var b in bills)
        {
            bool late = b.DueDate < today;
            m.Feed.Add(new FeedItem($"{b.AccountId} · {b.FullName}", $"{b.Due:C} due {b.DueDate:dd MMM yyyy}", "receipt",
                late ? "Overdue" : "Unpaid", late ? "bad" : "warn", "Accounts", "Bills", "Payment", new { id = b.Id }));
        }
        var byStatus = await db.Bills.GroupBy(b => b.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        foreach (BillStatus s in Enum.GetValues<BillStatus>())
            m.Chart.Add(new ChartBar(Split(s.ToString()), byStatus.FirstOrDefault(x => x.Key == s)?.Count ?? 0));

        m.Actions.Add(new QuickAction("Generate bill", "plus", "Accounts", "Bills", "Create"));
        m.Actions.Add(new QuickAction("Bills & payments", "receipt", "Accounts", "Bills", "Index"));
        m.Actions.Add(new QuickAction("Advanced search", "search", "", "Search", "Index"));
        return m;
    }

    public async Task<DashboardViewModel> CustomerAsync(int customerId, string name, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        IQueryable<Bill> mine = db.Bills.Where(b => b.Connection.CustomerId == customerId);
        decimal due = await OutstandingAsync(mine, ct);
        int active = await db.Connections.CountAsync(c => c.CustomerId == customerId && c.Status == ConnectionStatus.Active, ct);
        int open = await db.Orders.CountAsync(o => o.CustomerId == customerId && OpenStatuses.Contains(o.Status), ct);
        DateOnly? nextDue = await mine.Where(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid)
            .OrderBy(b => b.DueDate).Select(b => (DateOnly?)b.DueDate).FirstOrDefaultAsync(ct);

        var m = new DashboardViewModel
        {
            Eyebrow = "Customer portal",
            Title = "My Account",
            Lead = $"Welcome, {name}. Follow your orders, connections and bills in one place.",
            FeedTitle = "My recent orders",
            FeedMore = new FeedLink("All my orders", "Customer", "Orders", "Index"),
            FeedEmpty = "No orders yet - choose a plan to get connected."
        };
        m.Kpis.Add(new KpiCard("Amount due", due, "wallet", due > 0 ? "warn" : "ok",
            nextDue is null ? "Nothing to pay" : $"Next due date {nextDue:dd MMM yyyy}", "Customer", "Portal", "Bills", "$", 2));
        m.Kpis.Add(new KpiCard("Active connections", active, "plug", "ok", "In service now", "Customer", "Portal", "Connections"));
        m.Kpis.Add(new KpiCard("Orders in progress", open, "clipboard", "violet", "Being checked or connected", "Customer", "Orders", "Index"));

        await AddOrderFeedAsync(m, db.Orders.Where(o => o.CustomerId == customerId), "Customer", "Orders", "Details", withId: true, ct);

        m.Actions.Add(new QuickAction("New order", "plus", "Customer", "Orders", "Create"));
        m.Actions.Add(new QuickAction("My bills", "receipt", "Customer", "Portal", "Bills"));
        m.Actions.Add(new QuickAction("My profile", "user", "Customer", "Portal", "Profile"));
        m.Actions.Add(new QuickAction("Send feedback", "star", "Customer", "Portal", "Feedback"));
        return m;
    }

    // Total of bills that are issued or partially paid, minus what has been paid on them.
    // (Two sums: SQL Server cannot aggregate over a sub-query in one SUM.)
    private static async Task<decimal> OutstandingAsync(IQueryable<Bill> bills, CancellationToken ct)
    {
        IQueryable<Bill> open = bills.Where(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid);
        decimal billed = await open.SumAsync(b => (decimal?)b.TotalAmount, ct) ?? 0m;
        decimal paid = await open.SelectMany(b => b.Payments).SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        return billed - paid;
    }

    private static async Task AddOrderFeedAsync(DashboardViewModel m, IQueryable<Order> orders, string area, string controller, string action, bool withId, CancellationToken ct)
    {
        var rows = await orders.AsNoTracking().OrderByDescending(o => o.PlacedAtUtc).Take(6)
            .Select(o => new { o.Id, o.OrderNumber, o.Customer.FullName, o.ConnectionType, o.Status, o.PlacedAtUtc, Plan = o.Plan.Name })
            .ToListAsync(ct);
        foreach (var o in rows)
        {
            m.Feed.Add(new FeedItem($"{o.OrderNumber} · {o.FullName}",
                $"{TypeName(o.ConnectionType)} · {o.Plan} · {o.PlacedAtUtc.ToLocalTime():dd MMM, HH:mm}",
                o.ConnectionType == ConnectionType.Telephone ? "message" : "plug",
                Split(o.Status.ToString()), StatusTone(o.Status), area, controller, action, withId ? new { id = o.Id } : null));
        }
    }

    private static async Task AddStatusChartAsync(DashboardViewModel m, IQueryable<Order> orders, CancellationToken ct)
    {
        var byStatus = await orders.GroupBy(o => o.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        foreach (OrderStatus s in Enum.GetValues<OrderStatus>())
            m.Chart.Add(new ChartBar(Split(s.ToString()), byStatus.FirstOrDefault(x => x.Key == s)?.Count ?? 0));
    }

    /// <summary>Badge tone for an order status, shared with the views' status colours.</summary>
    public static string StatusTone(OrderStatus s) => s switch
    {
        OrderStatus.Placed => "idle",
        OrderStatus.UnderFeasibilityCheck => "info",
        OrderStatus.Feasible => "warn",
        OrderStatus.Connected => "ok",
        _ => "bad"
    };

    private static string TypeName(ConnectionType t) => t switch
    {
        ConnectionType.DialUp => "Dial-Up",
        ConnectionType.Broadband => "Broadband",
        _ => "Landline"
    };

    // "UnderFeasibilityCheck" -> "Under feasibility check"
    private static string Split(string pascal) =>
        string.Concat(pascal.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + char.ToLowerInvariant(c) : c.ToString()));
}
