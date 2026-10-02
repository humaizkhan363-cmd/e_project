using Microsoft.AspNetCore.Mvc.Rendering;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Workflows;

/// <summary>
/// Drop-down lists shared by the retail and the customer order forms, so both show plans the same way.
/// </summary>
public static class OrderFormLists
{
    /// <summary>All plans, grouped by service type (the group label is the service name used by the page script).</summary>
    public static List<SelectListItem> GroupedPlans(IEnumerable<Plan> plans)
    {
        var groups = new Dictionary<ConnectionType, SelectListGroup>();
        return plans.Select(p =>
        {
            if (!groups.TryGetValue(p.ConnectionType, out SelectListGroup? group))
            {
                group = new SelectListGroup { Name = p.ConnectionType.ToString() };
                groups[p.ConnectionType] = group;
            }
            return new SelectListItem { Text = p.Name + " - " + p.Price.ToString("C"), Value = p.Id.ToString(), Group = group };
        }).ToList();
    }

    /// <summary>Landline plans offered with a dial-up order when the customer has no Nexus landline yet.</summary>
    public static List<SelectListItem> LandlinePlans(IEnumerable<Plan> plans)
    {
        return plans.Where(p => p.ConnectionType == ConnectionType.Telephone)
            .Select(p => new SelectListItem(p.Name + " - " + p.Price.ToString("C"), p.Id.ToString()))
            .ToList();
    }

    /// <summary>The three service types (Dial-Up, Broadband, Telephone).</summary>
    public static List<SelectListItem> ConnectionTypes()
    {
        return Enum.GetValues<ConnectionType>().Select(x => new SelectListItem(x.ToString(), ((int)x).ToString())).ToList();
    }
}
