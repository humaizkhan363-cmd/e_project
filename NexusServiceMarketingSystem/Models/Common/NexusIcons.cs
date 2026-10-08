namespace NexusServiceMarketingSystem.Models.Common
{
    /// <summary>
    /// Inline stroke icons (24x24 viewBox, Lucide-style paths) shared by the sidebar and the dashboards.
    /// </summary>
    public static class NexusIcons
    {
        private static readonly Dictionary<string, string> Paths = new()
        {
            ["home"] = "<path d=\"M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z\"/>",
            ["city"] = "<path d=\"M12 21s-7-6.2-7-11.5a7 7 0 0 1 14 0C19 14.8 12 21 12 21z\"/><circle cx=\"12\" cy=\"9.5\" r=\"2.5\"/>",
            ["shop"] = "<path d=\"M4 9h16l-1.5-5h-13zM5 9v11h14V9M9 20v-6h6v6\"/>",
            ["users"] = "<circle cx=\"9\" cy=\"8\" r=\"3.5\"/><path d=\"M2.5 20a6.5 6.5 0 0 1 13 0M16 4.5a3.5 3.5 0 0 1 0 7M18 14a6.5 6.5 0 0 1 3.5 6\"/>",
            ["layers"] = "<path d=\"m12 3 9 5-9 5-9-5zM3 13l9 5 9-5M3 17.5l9 5 9-5\"/>",
            ["percent"] = "<path d=\"M19 5 5 19\"/><circle cx=\"7\" cy=\"7\" r=\"2.5\"/><circle cx=\"17\" cy=\"17\" r=\"2.5\"/>",
            ["truck"] = "<path d=\"M2 6h11v10H2zM13 10h4l4 3.5V16h-8\"/><circle cx=\"6.5\" cy=\"17.5\" r=\"1.8\"/><circle cx=\"17\" cy=\"17.5\" r=\"1.8\"/>",
            ["box"] = "<path d=\"m12 2.5 8.5 4.5v10L12 21.5 3.5 17V7zM3.5 7 12 11.5 20.5 7M12 11.5v10\"/>",
            ["cart"] = "<path d=\"M2.5 3h2.5l2.4 12h11l2-8H6.2\"/><circle cx=\"9\" cy=\"19.5\" r=\"1.5\"/><circle cx=\"17\" cy=\"19.5\" r=\"1.5\"/>",
            ["user"] = "<circle cx=\"12\" cy=\"8\" r=\"4\"/><path d=\"M4 21a8 8 0 0 1 16 0\"/>",
            ["clipboard"] = "<path d=\"M9 4h6v3H9zM9 5.5H6v15.5h12V5.5h-3M9 12h6M9 16h4\"/>",
            ["search"] = "<circle cx=\"11\" cy=\"11\" r=\"7\"/><path d=\"m20.5 20.5-4.5-4.5\"/>",
            ["chart"] = "<path d=\"M4 20V10M10 20V4M16 20v-7M22 20H2\"/>",
            ["message"] = "<path d=\"M4 4h16v12H8l-4 4z\"/>",
            ["plug"] = "<path d=\"M9 2v5M15 2v5M6 7h12v4a6 6 0 0 1-12 0zM12 17v5\"/>",
            ["alert"] = "<path d=\"M12 3 2 20.5h20zM12 10v4.5M12 17.5v.5\"/>",
            ["receipt"] = "<path d=\"M5 2.5h14v19l-3.5-2-3.5 2-3.5-2-3.5 2zM9 8h6M9 12h6M9 16h3\"/>",
            ["wallet"] = "<path d=\"M3 6.5h16a2 2 0 0 1 2 2V19H5a2 2 0 0 1-2-2zM3 6.5 15.5 3v3.5M16 13h2\"/>",
            ["clock"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3.5 2\"/>",
            ["file"] = "<path d=\"M6 2.5h8l5 5v14H6zM14 2.5v5h5M9 13h7M9 17h5\"/>",
            ["check"] = "<path d=\"M9 11.5 11.5 14 16 9.5\"/><path d=\"M12 2.5 4 6v6c0 5 3.5 8.3 8 9.5 4.5-1.2 8-4.5 8-9.5V6z\"/>",
            ["tag"] = "<path d=\"M3 3h8l10 10-8 8L3 11zM7.5 7.5h.01\"/>",
            ["radar"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><circle cx=\"12\" cy=\"12\" r=\"4.5\"/><path d=\"M12 12 18.5 5.5\"/>",
            ["star"] = "<path d=\"m12 3 2.8 5.8 6.2.9-4.5 4.4 1 6.2L12 17.4l-5.5 2.9 1-6.2L3 9.7l6.2-.9z\"/>",
            ["plus"] = "<path d=\"M12 5v14M5 12h14\"/>",
            ["money"] = "<rect x=\"2.5\" y=\"6\" width=\"19\" height=\"12\" rx=\"2\"/><circle cx=\"12\" cy=\"12\" r=\"2.5\"/><path d=\"M6 9.5v.01M18 14.5v.01\"/>",
            ["trend"] = "<path d=\"M3 17l6-6 4 4 8-8M15 7h6v6\"/>",
            ["party"] = "<path d=\"M5 13l4 4L19 7\"/>"
        };

        /// <summary>Returns the &lt;svg&gt; markup for an icon name (falls back to "box").</summary>
        public static string Svg(string name, string cssClass = "nx-ico") =>
            $"<svg class=\"{cssClass}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">{(Paths.TryGetValue(name, out var p) ? p : Paths["box"])}</svg>";
    }
}
