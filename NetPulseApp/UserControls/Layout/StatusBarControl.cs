// UserControls/Layout/StatusBarControl.cs
using System.Net.Sockets;
using NetPulseApp.Managers;
using NetPulseApp.Model;
using NetPulseApp.Service;
using NetPulseApp.Service.Dpi;
using NetPulseApp.Service.Network;

namespace NetPulseApp.UserControls.Layout
{
    /// <summary>
    /// Shell status bar under the content: "● Online · Local IP · Public IP".
    /// Always alive, so it owns the network-change-driven public IP refresh
    /// (debounced) and records each result through NetworkChangeMonitor —
    /// the Dashboard repaints from the same IpRecorded event.
    /// </summary>
    public class StatusBarControl : UserControl
    {
        private const int BaseHeight = 26;
        private const int DebounceMs = 3000;

        private readonly ThemeManager _themeManager = ThemeManager.Instance;
        private readonly Label _lblState;
        private readonly Label _lblDetails;
        private readonly System.Windows.Forms.Timer _debounce = new() { Interval = DebounceMs };
        private string _publicIp = "—";
        private bool _online = true;

        public StatusBarControl()
        {
            Dock = DockStyle.Bottom;
            Height = DpiAwareService.Scale(BaseHeight);

            _lblState = new Label
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            _lblDetails = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            // Fill first, Left last — Left claims its strip before Fill.
            Controls.Add(_lblDetails);
            Controls.Add(_lblState);
            ApplyPadding();

            _debounce.Tick += async (s, e) => { _debounce.Stop(); await RefreshAsync(); };
            NetworkChangeMonitor.NetworkChanged += OnNetworkChanged;
            NetworkChangeMonitor.IpRecorded += OnIpRecorded;
            NetworkChangeMonitor.OnlineChanged += OnOnlineChanged;
            NetworkChangeMonitor.Start();

            ApplyTheme();
        }

        /// <summary>Paints cached values, then runs the first live lookup.</summary>
        public void Start()
        {
            var cached = IpDataService.Instance.Current ?? IpDataService.Instance.LoadCached();
            if (cached != null) _publicIp = cached.PrimaryIp();
            UpdateText();
            _ = RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            if (IsDisposed) return;
            try
            {
                var info = await IpDataService.Instance.RefreshAsync();
                if (IsDisposed) return;
                if (info != null) NetworkChangeMonitor.Record(info);
                else NetworkChangeMonitor.RecordFailure();
            }
            catch { /* lookup failures are reported via RecordFailure, never thrown */ }
            UpdateText();
        }

        // OS events arrive on a thread-pool thread and in bursts — marshal,
        // then debounce so one adapter change triggers one lookup.
        private void OnNetworkChanged()
        {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke(new Action(() => { UpdateText(); _debounce.Stop(); _debounce.Start(); }));
        }

        private void OnIpRecorded(IpInfo info)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => OnIpRecorded(info))); return; }
            _publicIp = string.IsNullOrEmpty(info.PrimaryIp()) ? "—" : info.PrimaryIp();
            UpdateText();
        }

        private void OnOnlineChanged(bool online)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => OnOnlineChanged(online))); return; }
            _online = online;
            UpdateText();
        }

        private void UpdateText()
        {
            string localIp = LocalIp();
            bool hasLink = localIp != "—";
            bool online = hasLink && _online;

            var theme = _themeManager.CurrentTheme;
            _lblState.Text = online ? "● Online" : hasLink ? "● No internet" : "● Offline";
            _lblState.ForeColor = online ? theme.SuccessColor : hasLink ? theme.WarningColor : theme.ErrorColor;
            _lblDetails.Text = $"   ·   Local IP {localIp}   ·   Public IP {(online ? _publicIp : "—")}";
        }

        private static string LocalIp()
        {
            try
            {
                var nic = DashboardService.FindActiveInterface();
                var v4 = nic?.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                return v4?.Address.ToString() ?? "—";
            }
            catch { return "—"; }
        }

        private void ApplyPadding() =>
            Padding = new Padding(DpiAwareService.Scale(12), 0, DpiAwareService.Scale(12), 0);

        public void RefreshForDpi()
        {
            Height = DpiAwareService.Scale(BaseHeight);
            ApplyPadding();
        }

        public void ApplyTheme()
        {
            var theme = _themeManager.CurrentTheme;
            BackColor = theme.HeaderBackground;
            _lblDetails.ForeColor = theme.SecondaryTextColor;
            UpdateText();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NetworkChangeMonitor.NetworkChanged -= OnNetworkChanged;
                NetworkChangeMonitor.IpRecorded -= OnIpRecorded;
                NetworkChangeMonitor.OnlineChanged -= OnOnlineChanged;
                _debounce.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
