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

    }
}