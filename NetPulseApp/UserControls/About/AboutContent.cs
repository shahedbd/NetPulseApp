// UserControls/About/AboutContent.cs
using NetPulseApp.Service.Icons;

namespace NetPulseApp.UserControls.About
{
    /// <summary>One card on the About screen.</summary>
    public sealed class AboutCardSpec
    {
        public IconChar Icon { get; init; }
        public Color Accent { get; init; }
        public string Title { get; init; } = "";
        public (IconChar Icon, string Text, Color Accent)[] Rows { get; init; } = Array.Empty<(IconChar, string, Color)>();
        public string? Note { get; init; }
    }

    /// <summary>
    /// The feature cards the About screen shows, kept in one place. Same
    /// shape as CpuZxPro's AboutContent / AboutTierPlan, minus the tiers —
    /// every MVP feature is free (decision D11), so there is no upgrade card.
    /// Row colors match each tool's sidebar icon (AppConfig.NavItems).
    /// </summary>
    public static class AboutContent
    {
        private static readonly Color Blue = Color.FromArgb(59, 130, 246);
        private static readonly Color Green = Color.FromArgb(16, 185, 129);
        private static readonly Color Purple = Color.FromArgb(139, 92, 246);
        private static readonly Color Pink = Color.FromArgb(236, 72, 153);
        private static readonly Color Indigo = Color.FromArgb(99, 102, 241);
        private static readonly Color Cyan = Color.FromArgb(6, 182, 212);
        private static readonly Color Teal = Color.FromArgb(20, 184, 166);
        private static readonly Color Rose = Color.FromArgb(244, 63, 94);
        private static readonly Color Amber = Color.FromArgb(245, 158, 11);
        private static readonly Color Slate = Color.FromArgb(100, 116, 139);

        public static AboutCardSpec[] Cards { get; } =
        {
            new()
            {
                Icon = IconChar.Gauge, Accent = Blue, Title = "Monitor & Diagnose",
                Rows = new[]
                {
                    (IconChar.Gauge, "Live dashboard — latency, jitter, A–F grade, public IP", Blue),
                    (IconChar.CheckCircle, "Diagnose My Connection — plain-English fix steps", Green),
                    (IconChar.Download, "Internet speed test with saved history", Purple),
                    (IconChar.Wifi, "Wi-Fi signal, band, channel and link speed", Pink),
                }
            },
            new()
            {
                Icon = IconChar.NetworkWired, Accent = Indigo, Title = "Network Tools",
                Rows = new[]
                {
                    (IconChar.SatelliteDish, "Ping with live latency graph", Green),
                    (IconChar.Globe, "Traceroute, hop by hop", Indigo),
                    (IconChar.MagnifyingGlass, "DNS lookup — A, AAAA, MX, TXT, NS and more", Cyan),
                    (IconChar.Plug, "Port checker with common-service presets", Teal),
                    (IconChar.AddressCard, "WHOIS for IP addresses", Rose),
                    (IconChar.Calculator, "IPv4 / IPv6 subnet calculator", Amber),
                }
            },
            new()
            {
                Icon = IconChar.Star, Accent = Slate, Title = "Essentials",
                Rows = new[]
                {
                    (IconChar.Copy, "Copy and export results from every tool", Slate),
                    (IconChar.Moon, "Dark / light theme, sharp at any DPI", Slate),
                    (IconChar.WindowRestore, "Keeps running in the system tray", Slate),
                },
                Note = "Every feature is free. No account required."
            },
        };
    }
}
