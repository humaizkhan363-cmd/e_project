using Microsoft.AspNetCore.Html;

namespace NexusServiceMarketingSystem.Helpers;

/// <summary>
/// Inline SVG line icons (24x24, stroke based, in the style of the Lucide icon set - ISC licence) used by the
/// dashboards, the home page and the plan cards. Icons are decorative: they are hidden from screen readers and
/// always sit next to a visible text label.
/// </summary>
public static class Icons
{
    // Path data per icon name; every icon is drawn with the same stroke settings.
    private static readonly Dictionary<string, string> Paths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["city"] = "<path d='M3 21h18M5 21V7l7-4 7 4v14M9 9h1M9 13h1M9 17h1M14 9h1M14 13h1M14 17h1'/>",
        ["store"] = "<path d='M3 9l1.5-5h15L21 9M3 9h18v2a3 3 0 0 1-6 0 3 3 0 0 1-6 0 3 3 0 0 1-6 0V9M5 13v8h14v-8M10 21v-5h4v5'/>",
        ["users"] = "<circle cx='9' cy='8' r='4'/><path d='M2 21v-1a6 6 0 0 1 12 0v1M16 3.1a4 4 0 0 1 0 7.8M22 21v-1a6 6 0 0 0-4-5.7'/>",
        ["user"] = "<circle cx='12' cy='8' r='4'/><path d='M4 21v-1a8 8 0 0 1 16 0v1'/>",
        ["layers"] = "<path d='M12 2 2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5'/>",
        ["percent"] = "<path d='M19 5 5 19'/><circle cx='6.5' cy='6.5' r='2.5'/><circle cx='17.5' cy='17.5' r='2.5'/>",
        ["truck"] = "<path d='M1 4h13v12H1zM14 9h4l4 4v3h-8'/><circle cx='5.5' cy='18.5' r='2'/><circle cx='17.5' cy='18.5' r='2'/>",
        ["box"] = "<path d='M21 8 12 3 3 8v8l9 5 9-5V8zM3 8l9 5 9-5M12 13v8'/>",
        ["cart"] = "<circle cx='9' cy='20' r='1.5'/><circle cx='18' cy='20' r='1.5'/><path d='M1 2h3l2.7 12.4a2 2 0 0 0 2 1.6h8.8a2 2 0 0 0 2-1.6L21 7H5.1'/>",
        ["clipboard"] = "<rect x='5' y='3' width='14' height='18' rx='2'/><path d='M9 3h6v3H9zM9 11h6M9 15h4'/>",
        ["search"] = "<circle cx='11' cy='11' r='7'/><path d='m20 20-3.5-3.5'/>",
        ["chart"] = "<path d='M3 3v18h18M8 17v-5M13 17V8M18 17v-9'/>",
        ["message"] = "<path d='M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z'/>",
        ["wifi"] = "<path d='M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01'/>",
        ["router"] = "<rect x='2' y='14' width='20' height='7' rx='2'/><path d='M6 18h.01M10 18h.01M15 10a4 4 0 0 0-6 0M18 7a8.5 8.5 0 0 0-12 0'/>",
        ["phone"] = "<path d='M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1.9.4 1.9.7 2.8a2 2 0 0 1-.5 2.1L8.1 9.9a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 2.1-.4c.9.3 1.9.6 2.8.7a2 2 0 0 1 1.7 2z'/>",
        ["radar"] = "<circle cx='12' cy='12' r='9'/><circle cx='12' cy='12' r='5'/><path d='M12 12 18 6'/><circle cx='12' cy='12' r='1'/>",
        ["plug"] = "<path d='M9 2v6M15 2v6M6 8h12v3a6 6 0 0 1-12 0V8zM12 17v5'/>",
        ["alert"] = "<path d='M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0zM12 9v4M12 17h.01'/>",
        ["wrench"] = "<path d='M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.8-3.8a6 6 0 0 1-7.9 7.9l-6.9 6.9a2.1 2.1 0 0 1-3-3l6.9-6.9a6 6 0 0 1 7.9-7.9l-3.8 3.8z'/>",
        ["receipt"] = "<path d='M4 2v20l3-2 3 2 2-2 2 2 3-2 3 2V2l-3 2-3-2-2 2-2-2-3 2zM8 8h8M8 12h8M8 16h5'/>",
        ["card"] = "<rect x='2' y='5' width='20' height='14' rx='2'/><path d='M2 10h20M6 15h4'/>",
        ["history"] = "<path d='M3 12a9 9 0 1 0 3-6.7L3 8M3 3v5h5M12 7v5l3 3'/>",
        ["plus"] = "<rect x='3' y='3' width='18' height='18' rx='3'/><path d='M12 8v8M8 12h8'/>",
        ["folder"] = "<path d='M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7z'/>",
        ["star"] = "<path d='m12 2 3.1 6.3 6.9 1-5 4.9 1.2 6.8-6.2-3.2-6.2 3.2L7 14.2 2 9.3l6.9-1L12 2z'/>",
        ["tag"] = "<path d='M20.6 13.4 13.4 20.6a2 2 0 0 1-2.8 0L2 12V2h10l8.6 8.6a2 2 0 0 1 0 2.8z'/><circle cx='7' cy='7' r='1.5'/>",
        ["shield"] = "<path d='M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z'/><path d='m9 12 2 2 4-4'/>",
        ["map"] = "<path d='M12 22s7-6.2 7-12a7 7 0 0 0-14 0c0 5.8 7 12 7 12z'/><circle cx='12' cy='10' r='2.5'/>",
        ["clock"] = "<circle cx='12' cy='12' r='9'/><path d='M12 7v5l3 2'/>",
        ["bolt"] = "<path d='M13 2 3 14h9l-1 8 10-12h-9l1-8z'/>",
        ["check"] = "<circle cx='12' cy='12' r='9'/><path d='m8 12 3 3 5-6'/>",
        ["arrow"] = "<path d='M5 12h14M13 6l6 6-6 6'/>",
        ["dollar"] = "<path d='M12 2v20M17 6.5C17 4.6 14.8 3.5 12 3.5S7 4.6 7 6.8 9.2 9.8 12 10.5s5 1.6 5 3.8-2.2 3.6-5 3.6-5-1.2-5-3.1'/>",
    };

    /// <summary>Renders the named icon as inline SVG (empty if the name is unknown).</summary>
    public static IHtmlContent Svg(string name, string cssClass = "")
    {
        if (!Paths.TryGetValue(name, out string? body)) return HtmlString.Empty;
        string cls = string.IsNullOrWhiteSpace(cssClass) ? "nx-svg" : "nx-svg " + cssClass;
        return new HtmlString(
            $"<svg class=\"{cls}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" " +
            $"stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">{body}</svg>");
    }
}
