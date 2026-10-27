// Helper/AppConfig.cs
using NetPulseApp.Forms;
using NetPulseApp.UserControls.Pages;
using NetPulseApp.Service.Icons;

namespace NetPulseApp.Helper
{
    /// <summary>
    /// Single source of truth for everything that changes when bootstrapping a
    /// new app from this template: identity, layout, theme colors, and the
    /// sidebar's nav items. No other file should need structural changes.
    /// </summary>
    public static class AppConfig
    {
        // ── App identity ─────────────────────────────────────────────────────
        public static string AppName = "Net Pulse";
        public static string AppSubtitle = "Network Diagnostic Toolkit";
        public static string AppVersion = "v1.0.0.0";

        public static string AppIconPath => Path.Combine(Application.StartupPath, "Resources", "180x180.png");

        public static string FaviconPath => Path.Combine(Application.StartupPath, "Resources", "favicon.ico");

        //Device Data Constants ─────────────────────────────────────────────────────
        public static int AppReleaseVersion = 1000;
        public static string AppDataFolderName = "NetPulseApp";
        public static string NetSpeedPlusMsStoreLink = "ms-windows-store://pdp/?productid=9P00PF8JTJ1L";
        public static string CPUZxMsStoreLink = "ms-windows-store://pdp/?productid=9P5G6W4FPNS2";
        public static string CPUZxProMsStoreLink = "ms-windows-store://pdp/?productid=9N5RXJCZB734";

        //API Constants
        public static string apiSecretKey = "c96524b3-dad4-4146-aa4a-7e6b99b92d8b";
        public static string apiUrlLocal = "http://localhost:85/api/deviceinstallationinfoapi/add-new";
        public static string apiUrlProd = "https://storeapi.zerobytebd.com/api/deviceinstallationinfoapi/add-new";
        public static string CountryNameAPIServiceURL = "https://ipapi.co/country_name/";

        // ── Public IP providers (Service/Ip/*, copied from IpAddressApp) ─────
        // Keyless and HTTPS only. Order is priority order.
        public static string IpWhoIsEndpoint = "https://ipwho.is/";
        public static string IpApiCoEndpoint = "https://ipapi.co/json/";
        public static string CloudflareTraceEndpoint = "https://1.1.1.1/cdn-cgi/trace";

        /// <summary>Family-pinned public-address endpoints, each its own fallback chain.</summary>
        public static List<string> Ipv4ProbeEndpoints = new()
        {
            "https://api4.ipify.org",
            "https://ipv4.icanhazip.com",
            "https://v4.ident.me"
        };

        public static List<string> Ipv6ProbeEndpoints = new()
        {
            "https://api6.ipify.org",
            "https://ipv6.icanhazip.com",
            "https://v6.ident.me"
        };

        /// <summary>Per-provider budget before the chain moves on.</summary>
        public static int IpProviderTimeoutSeconds = 6;

        /// <summary>Per-endpoint budget for one address probe.</summary>
        public static int IpProbeTimeoutSeconds = 4;

        /// <summary>How long a successful lookup counts as current (avoids free-tier rate limits).</summary>
        public static int IpFreshnessSeconds = 60;

        // ── System tray ──────────────────────────────────────────────────────
        // When true, closing the window hides it to the tray instead of
        // exiting (restore via the tray icon's "Show App" / double-click, or
        // exit via its "Exit" item). When false, the close button just
        // closes the app normally and no tray icon is ever shown.
        public static bool EnableSystemTray = true;

        // ── About dialog content ────────────────────────────────────────────
        // Everything Forms/AboutForm.cs shows — edit these, not AboutForm.cs.
        public static string AppDescription = "A reusable Windows Desktop application template.";
        public static string DeveloperName = "Your Company Name";
        public static string WebsiteUrl = "https://example.com";
        public static string SupportEmail = "support@example.com";

        public static List<string> AboutFeatures = new()
        {
            "Built on WinForms + .NET 10",
            "Single-file central configuration (AppConfig.cs)",
            "Dark / Light theme support",
            "DPI-aware — scales cleanly from 100% to 200%",
            "Collapsible sidebar navigation",
            "Test data 01",
            "Test data 02",
            "Test data 03",
            "Test data 04",
        };

        // ── Layout dimensions (base values at 96 DPI) ───────────────────────────
        public static int TitleBarHeight = 35;
        public static int HeaderHeight = 60;
        public static int SidebarWidth = 200; //250
        public static int SidebarCollapsedWidth = 60;

        // ── Theme & colors ───────────────────────────────────────────────────
        public static Color PrimaryColor = Color.FromArgb(0, 191, 166); // #00BFA6
        public static Color BackgroundColor = Color.FromArgb(18, 18, 18); // #121212
        public static Color SurfaceColor = Color.FromArgb(35, 35, 35); // #232323
        public static Color TextPrimaryColor = Color.FromArgb(230, 230, 230); // #E6E6E6
        public static Color TextSecondaryColor = Color.FromArgb(180, 180, 180); // #B4B4B4

        // Title bar's own "chrome" color — fixed across both dark and light
        // theme (like PrimaryColor) so the title bar always reads as its own
        // distinct strip rather than blending into the header/content below it.
        // Same value as PrimaryColor, so the title bar, header buttons (see
        // HeaderButtonService.BrandColor), and About dialog's Close button all
        // read as one consistent identity instead of unrelated colors.
        public static Color TitleBarColor = PrimaryColor;

        // ── Navigation items ─────────────────────────────────────────────────
        // Add, remove, or rename nav items here without touching any control
        // or manager code — NavigationManager renders and wires these up.
        public static List<NavItem> NavItems = new()
        {
            new NavItem { Label = "Dashboard", Icon = IconChar.Gauge,        IconColor = Color.FromArgb(59, 130, 246),  ControlType = typeof(DashboardControl) },
            new NavItem { Label = "Diagnose",  Icon = IconChar.CheckCircle,   IconColor = Color.FromArgb(16, 185, 129),  ControlType = typeof(DiagnoseControl)  },
            new NavItem { Label = "Speed Test", Icon = IconChar.Download,    IconColor = Color.FromArgb(139, 92, 246),  ControlType = typeof(SpeedTestControl) },
            new NavItem { Label = "Wi-Fi",     Icon = IconChar.Wifi,          IconColor = Color.FromArgb(236, 72, 153),  ControlType = typeof(WifiControl)      },
            new NavItem { Label = "Ping",      Icon = IconChar.SatelliteDish, IconColor = Color.FromArgb(34, 197, 94),   ControlType = typeof(PingControl)      },
            new NavItem { Label = "Traceroute", Icon = IconChar.Globe,       IconColor = Color.FromArgb(99, 102, 241),  ControlType = typeof(TracerouteControl) },
            new NavItem { Label = "DNS Lookup", Icon = IconChar.MagnifyingGlass, IconColor = Color.FromArgb(6, 182, 212), ControlType = typeof(DnsLookupControl) },
            new NavItem { Label = "Port Checker", Icon = IconChar.Plug,      IconColor = Color.FromArgb(20, 184, 166),  ControlType = typeof(PortCheckerControl) },
            new NavItem { Label = "WHOIS IP",   Icon = IconChar.AddressCard,  IconColor = Color.FromArgb(244, 63, 94),   ControlType = typeof(WhoisControl)     },
            new NavItem { Label = "Subnet Calc", Icon = IconChar.Calculator,   IconColor = Color.FromArgb(245, 158, 11),  ControlType = typeof(SubnetCalcControl) },
            new NavItem { Label = "Settings",  Icon = IconChar.Gear,          IconColor = Color.FromArgb(100, 116, 139), ControlType = typeof(SettingsPageControl) },
            // ShowModal instead of ControlType — About is a dialog, not a
            // content page, so there's nothing to dock into the content panel.
            new NavItem { Label = "About",     Icon = IconChar.InfoCircle,    IconColor = Color.FromArgb(148, 163, 184), ShowModal = () => new AboutForm().ShowDialog() },
        };
    }

    /// <summary>A single sidebar navigation entry.</summary>
    public class NavItem
    {
        public string Label { get; set; }
        public IconChar Icon { get; set; }

        /// <summary>Icon's own brand color — shown at all times, active or
        /// not, so the sidebar reads as colorful rather than monochrome.</summary>
        public Color IconColor { get; set; }

        /// <summary>Content page to load into the content panel. Mutually
        /// exclusive with ShowModal — set exactly one of the two.</summary>
        public Type ControlType { get; set; }

        /// <summary>Opens a modal dialog instead of loading a page (e.g. an
        /// About box). When set, ControlType is ignored and the sidebar's
        /// active-page highlight doesn't change.</summary>
        public Action ShowModal { get; set; }
    }
}
