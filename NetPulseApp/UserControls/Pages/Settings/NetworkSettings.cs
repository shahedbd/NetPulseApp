using DeviceDataModule;
using NetPulseApp.Forms;
using NetPulseApp.Managers;
using NetPulseApp.Service;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages.Settings
{
    /// <summary>
    /// Network &amp; IP — the public IP lookup options IpDataService already
    /// reads (provider order, fallback, IPv4 preference), plus the tools'
    /// remembered targets (moved from the old page's Data card).
    /// </summary>
    internal sealed class NetworkSettings : SettingsCategory
    {
        public override string Title => "Network & IP";
        public override string Subtitle => "How NetPulse finds your public IP and location";

        /// <summary>Dropdown order ↔ IpProvider.</summary>
        private static readonly (string Label, IpProvider Provider)[] Providers =
        {
            ("ipwho.is", IpProvider.IpWhoIs),
            ("ipapi.co", IpProvider.IpApiCo),
            ("Cloudflare (IP + country)", IpProvider.Cloudflare)
        };

        private ModernDropDown _cmbProvider;
        private ToggleSwitchControl _tglFallback;
        private ToggleSwitchControl _tglIpv4;
        private Button _btnClearHistory;

        protected override void BuildContent()
        {
            AddSection("Public IP lookup");
            var lookup = AddCard();

            _cmbProvider = MakeDropDown(Providers.Select(p => p.Label), 200);
            _cmbProvider.SelectionChanged += (s, e) =>
            {
                if (IsLoading || _cmbProvider.SelectedIndex < 0) return;
                Commit(c => c.PreferredProvider = Providers[_cmbProvider.SelectedIndex].Provider);
                RefreshPublicIp();
            };
            AddRow(lookup, "Preferred provider", _cmbProvider,
                "Tried first. ipwho.is and ipapi.co include ISP and city.",
                "ip", "api", "service", "geolocation", "location", "isp", "cloudflare");
            lookup.AddDivider();

            _tglFallback = new ToggleSwitchControl();
            _tglFallback.CheckedChanged += (s, e) => Commit(c => c.ApiAutoFallback = _tglFallback.Checked);
            AddRow(lookup, "Fall back to other providers", _tglFallback,
                "If the preferred one fails, try the others.",
                "fallback", "retry", "backup", "provider");
            lookup.AddDivider();

            _tglIpv4 = new ToggleSwitchControl();
            _tglIpv4.CheckedChanged += (s, e) => Commit(c => c.PreferIpv4 = _tglIpv4.Checked);
            AddRow(lookup, "Prefer IPv4 address", _tglIpv4,
                "Show IPv4 first when you have both IPv4 and IPv6.",
                "ipv4", "ipv6", "address", "public");

            AddSection("Tool history");
            var history = AddCard();
            _btnClearHistory = MakeButton(ClearLabel(TargetCount()), (s, e) => ClearToolHistory());
            _btnClearHistory.Enabled = TargetCount() > 0;
            AddRow(history, "Remembered targets", _btnClearHistory,
                "Used by Ping, Traceroute, DNS Lookup, Port Checker and WHOIS IP.",
                "history", "recent", "targets", "hosts", "clear", "ping", "dns", "whois", "port", "traceroute");

            LoadValues();
        }

        public override void ResetToDefaults()
        {
            Current.PreferredProvider = IpProvider.IpWhoIs;
            Current.ApiAutoFallback = true;
            Current.PreferIpv4 = true;
            SettingsService.Save();
        }

        private void LoadValues()
        {
            IsLoading = true;
            try
            {
                int index = Array.FindIndex(Providers, p => p.Provider == Current.PreferredProvider);
                _cmbProvider.SelectedIndex = index >= 0 ? index : 0;
                _tglFallback.Checked = Current.ApiAutoFallback;
                _tglIpv4.Checked = Current.PreferIpv4;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>IpDataService has no invalidate; a forced refresh re-queries in the new order.</summary>
        private static async void RefreshPublicIp()
        {
            try
            {
                await IpDataService.Instance.RefreshAsync(force: true);
            }
            catch
            {
                // Offline or every provider failed — the Dashboard shows that on its own.
            }
        }

        private static int TargetCount()
        {
            var c = Current;
            return c.PingTargets.Count + c.TraceTargets.Count + c.DnsTargets.Count
                 + c.PortCheckerHosts.Count + c.WhoisTargets.Count;
        }

        private static string ClearLabel(int count) => count > 0 ? $"Clear history ({count})" : "No history";

        private void ClearToolHistory()
        {
            bool confirmed = AlertNotificationService.Confirm(
                "This removes every remembered target from Ping, Traceroute, DNS Lookup, Port Checker and WHOIS IP. This cannot be undone.",
                "Clear tool history?",
                AlertMessageType.Warning,
                "Clear",
                "Cancel");
            if (!confirmed)
                return;

            var settings = Current;
            settings.PingTargets.Clear();
            settings.TraceTargets.Clear();
            settings.DnsTargets.Clear();
            settings.PortCheckerHosts.Clear();
            settings.WhoisTargets.Clear();
            SettingsService.Save();

            _btnClearHistory.Text = ClearLabel(0);
            _btnClearHistory.Enabled = false;
            ToastsNotificationManager.Instance.ShowSuccessNotification("Tool history cleared.");
        }
    }
}
