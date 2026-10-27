// UserControls/Pages/DashboardControl.PublicIp.cs
using NetPulseApp.Model;
using NetPulseApp.Service;
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Ip;
using NetPulseApp.Service.Privacy;
using NetPulseApp.UiComponent;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Dashboard public-IP rows (PUBLIC IP / ISP / LOCATION), the VPN badge,
    /// and the Copy / Refresh actions on the connection card. Data comes from
    /// the shared IpDataService (cache + provider fallback chain); results
    /// recorded anywhere (e.g. the status bar) repaint here via
    /// NetworkChangeMonitor.IpRecorded.
    /// </summary>
    public partial class DashboardControl
    {
        private Label _vpnBadge;
        private IconButton _btnCopyIp;
        private IconButton _btnRefreshIp;
        private CancellationTokenSource _ipCts;
        private bool _vpnActive;

        private void BuildPublicIpActions()
        {
            _btnRefreshIp = ToolUiFactory.GhostButton("Refresh", IconChar.ArrowsRotate);
            _btnCopyIp = ToolUiFactory.GhostButton("Copy IP", IconChar.Copy);
            foreach (var btn in new[] { _btnRefreshIp, _btnCopyIp })
                btn.Size = new Size(Scale(96), Scale(28));

            _vpnBadge = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.Transparent,
                AutoSize = true
            };

            _btnRefreshIp.Click += async (s, e) => await RefreshPublicIpAsync(force: true);
            _btnCopyIp.Click += (s, e) =>
                ToolUiFactory.CopyResults(PublicIpValue("PUBLIC IP"));

            _connectionCard.Controls.Add(_vpnBadge);
            _connectionCard.Controls.Add(_btnCopyIp);
            _connectionCard.Controls.Add(_btnRefreshIp);
            _connectionCard.Resize += (s, e) => LayoutPublicIpActions();
        }

        private void LayoutPublicIpActions()
        {
            int top = Scale(8);
            _btnRefreshIp.Location = new Point(_connectionCard.ClientSize.Width - _btnRefreshIp.Width - Scale(10), top);
            _btnCopyIp.Location = new Point(_btnRefreshIp.Left - _btnCopyIp.Width - Scale(8), top);
            _vpnBadge.Location = new Point(_btnCopyIp.Left - _vpnBadge.Width - Scale(12), top + Scale(6));
        }

        private void StartPublicIp()
        {
            NetworkChangeMonitor.IpRecorded += OnIpRecorded;

            // Paint last run's result at once (labelled as cached), then go live.
            var cached = IpDataService.Instance.Current ?? IpDataService.Instance.LoadCached();
            if (cached != null) ShowPublicIp(cached);
            _ = RefreshPublicIpAsync(force: false);
        }

        private async Task RefreshPublicIpAsync(bool force)
        {
            if (IsDisposed) return;

            _ipCts?.Cancel();
            _ipCts?.Dispose();
            _ipCts = new CancellationTokenSource();
            var token = _ipCts.Token;

            _btnRefreshIp.Enabled = false;
            try
            {
                var vpnTask = Task.Run(() => VpnDetector.Scan());
                var info = await IpDataService.Instance.RefreshAsync(token, force);
                _vpnActive = (await vpnTask).HasActiveTunnel;
                if (IsDisposed || token.IsCancellationRequested) return;

                if (info != null) NetworkChangeMonitor.Record(info); // repaints via IpRecorded
                else
                {
                    NetworkChangeMonitor.RecordFailure();
                    if (IpDataService.Instance.Current == null)
                        _connectionValues["PUBLIC IP"].Text = "Unavailable — check your connection";
                }
                ApplyVpnBadge();
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (!IsDisposed) _btnRefreshIp.Enabled = true;
            }
        }

        private void OnIpRecorded(IpInfo info)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => OnIpRecorded(info))); return; }
            ShowPublicIp(info);
        }

        private void ShowPublicIp(IpInfo info)
        {
            string ip = info.PrimaryIp();
            _connectionValues["PUBLIC IP"].Text = string.IsNullOrEmpty(ip) ? "—"
                : info.IsCached ? $"{ip}  (cached {IpDataCache.DescribeAge(info.RetrievedUtc)})" : ip;
            _connectionValues["ISP"].Text = Fallback(string.IsNullOrWhiteSpace(info.Isp) ? info.Organization : info.Isp);
            _connectionValues["LOCATION"].Text = Fallback(info.LocationLine());
        }

        /// <summary>Copy-ready value: strips the "(cached …)" suffix.</summary>
        private string PublicIpValue(string field)
        {
            string text = _connectionValues[field].Text;
            int cut = text.IndexOf("  (", StringComparison.Ordinal);
            text = cut > 0 ? text[..cut] : text;
            return text == "—" || text.StartsWith("Unavailable") ? "" : text;
        }

        private static string Fallback(string value) =>
            string.IsNullOrWhiteSpace(value) ? "—" : value;

        /// <summary>
        /// Positive-only signal: no tunnel adapter found is NOT "no VPN" —
        /// a router-level VPN is invisible from here (see VpnDetector).
        /// </summary>
        private void ApplyVpnBadge()
        {
            var theme = themeManager.CurrentTheme;
            _vpnBadge.Text = _vpnActive ? "● VPN active" : "No VPN adapter";
            _vpnBadge.ForeColor = _vpnActive ? theme.SuccessColor : theme.SecondaryTextColor;
            LayoutPublicIpActions();
        }

        private void ApplyPublicIpTheme()
        {
            ToolUiFactory.ApplyGhostTheme(_btnCopyIp);
            ToolUiFactory.ApplyGhostTheme(_btnRefreshIp);
            if (!string.IsNullOrEmpty(_vpnBadge.Text)) ApplyVpnBadge();
        }

        private void DisposePublicIp()
        {
            NetworkChangeMonitor.IpRecorded -= OnIpRecorded;
            _ipCts?.Cancel();
            _ipCts?.Dispose();
            _ipCts = null;
        }
    }
}
