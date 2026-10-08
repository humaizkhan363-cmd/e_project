namespace NexusServiceMarketingSystem.Models.Dashboard
{
    /// <summary>
    /// One dashboard page for any role: headline counters, a "needs attention" feed,
    /// an optional bar chart and quick actions. Built by <c>DashboardService</c>.
    /// </summary>
    public sealed class DashboardViewModel
    {
        public string Eyebrow { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Lead { get; init; } = string.Empty;
        public List<KpiCard> Kpis { get; } = new();
        public string FeedTitle { get; init; } = "Needs attention";
        public FeedLink? FeedMore { get; init; }
        public List<FeedItem> Feed { get; } = new();
        public string FeedEmpty { get; init; } = "Nothing waiting - you are all caught up.";
        public string? ChartTitle { get; init; }
        public List<ChartBar> Chart { get; } = new();
        public List<QuickAction> Actions { get; } = new();
    }

    /// <summary>A headline number. Tone: info, ok, warn, bad or violet. Prefix e.g. "$".</summary>
    public sealed record KpiCard(string Label, decimal Value, string Icon, string Tone, string Hint,
        string? Area, string? Controller, string? Action, string? Prefix = null, int Decimals = 0);

    public sealed record FeedItem(string Title, string Detail, string Icon, string BadgeText, string BadgeTone,
        string? Area, string? Controller, string? Action, object? RouteValues = null);

    public sealed record FeedLink(string Text, string Area, string Controller, string Action);

    public sealed record ChartBar(string Label, int Value);

    public sealed record QuickAction(string Label, string Icon, string Area, string Controller, string Action);
}
