namespace DeviceDataModule
{
    public class AppSettingsVm
    {
        
        public int AppVersion { get; set; } = 0;
        public int LaunchCount { get; set; } = 0;
        public string LastAppVersion { get; set; } = string.Empty;
        public DateTime? LastUpdateDate { get; set; } = null;
        public DateTime? LastPromotionDate { get; set; } = null;
        public int PromotionCount { get; set; }
        public bool IsNewInstallation { get; set; }

        // ── User preferences ─────────────────────────────────────────────
        public bool IsDarkMode { get; set; } = true;
        public bool IsSidebarCollapsed { get; set; } = false;

        // Default true — matches the app's existing unconditional
        // minimize-to-tray behavior (AppConfig.EnableSystemTray), so
        // existing installs see no behavior change until they opt out.
        public bool MinimizeToTrayOnClose { get; set; } = true;

        // ── Tool state ───────────────────────────────────────────────────
        /// <summary>Recent ping targets (most recent first), shown in the
        /// Ping page's target dropdown. Capped by the page on save.</summary>
        public List<string> PingTargets { get; set; } = new();

        /// <summary>Recent traceroute targets (most recent first), shown in
        /// the Traceroute page's target dropdown. Capped by the page on save.</summary>
        public List<string> TraceTargets { get; set; } = new();

        /// <summary>Recent DNS lookup domains (most recent first), shown in
        /// the DNS Lookup page's domain dropdown. Capped by the page on save.</summary>
        public List<string> DnsTargets { get; set; } = new();

        /// <summary>Recent port-check hosts (most recent first), shown in
        /// the Port Checker page's host dropdown. Capped by the page on save.</summary>
        public List<string> PortCheckerHosts { get; set; } = new();

        /// <summary>Recent WHOIS targets (most recent first), shown in the
        /// WHOIS IP page's target dropdown. Capped by the page on save.</summary>
        public List<string> WhoisTargets { get; set; } = new();

        // ── Public IP lookup (Service/IpDataService.cs) ──────────────────
        public bool PreferIpv4 { get; set; } = true;
        public IpProvider PreferredProvider { get; set; } = IpProvider.IpWhoIs;
        public bool ApiAutoFallback { get; set; } = true;

    }

    /// <summary>
    /// Preferred external IP/geolocation provider. The app always falls back
    /// through the remaining providers when <see cref="AppSettingsVm.ApiAutoFallback"/>
    /// is true — this only chooses which one is tried first. Keyless providers
    /// only: an API key shipped inside an MSIX is extractable.
    /// </summary>
    public enum IpProvider
    {
        IpWhoIs = 0,      // ipwho.is        — no key, HTTPS, full geo + ASN
        IpApiCo = 1,      // ipapi.co        — no key, HTTPS, full geo
        Cloudflare = 2    // /cdn-cgi/trace  — no key, IP + country only (last resort)
    }
}
