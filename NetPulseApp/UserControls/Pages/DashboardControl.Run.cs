// UserControls/Pages/DashboardControl.Run.cs
using NetPulseApp.Service.Network;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Dashboard page run logic: monitor lifecycle (interface snapshot
    /// service + a continuous Ping reused for latency/jitter/loss and the
    /// QualityScore grade), tile and
    /// graph updates, and the Running badge. Layout/theme live in
    /// DashboardControl.cs.
    /// </summary>
    public partial class DashboardControl
    {
        // Continuous monitor ping — a stable anycast address measures real
        // internet latency and loss, independent of the tool pages' runs.
        private const string MonitorHost = "8.8.8.8";
        private const int MonitorIntervalMs = 2000;

        private readonly DashboardService _dashboardService = new();
        private readonly PingService _pingService = new();
        private bool _servicesDisposed;

        private void WireMonitor()
        {
            _dashboardService.Updated += OnSnapshot;
            _pingService.ReplyReceived += OnMonitorReply;
            _btnToggle.Click += (s, e) =>
            {
                if (_dashboardService.IsRunning) StopMonitor();
                else StartMonitor();
            };
        }

        private void StartMonitor()
        {
            if (_servicesDisposed || _dashboardService.IsRunning)
                return;

            _btnToggle.Text = "Pause";
            _badge.Text = "● Running";
            _graph.SetLive(true);
            _dashboardService.Start();
            _pingService.Start(MonitorHost, MonitorIntervalMs, count: 0);
        }

        private void StopMonitor()
        {
            if (_servicesDisposed)
                return;

            _btnToggle.Text = "Resume";
            _badge.Text = "● Paused";
            _graph.SetLive(false);
            _dashboardService.Stop();
            _pingService.Stop();
        }

        // ─────────────────────────────────────────────────────────────────────
        // LIVE UPDATES
        // ─────────────────────────────────────────────────────────────────────

        private void OnMonitorReply(PingResult result, PingStats stats)
        {
            if (IsDisposed) return;

            _tileLatency.SetData(
                result.RoundtripMs.HasValue ? $"{result.RoundtripMs} ms" : "Timeout",
                $"min/avg/max {stats.MinAvgMaxText}");
            ApplyGrade(QualityScore.Grade(stats));

            _tileJitter.SetData(
                stats.JitterSamples == 0 ? "—" : $"{stats.Jitter:0.#} ms",
                $"Good for: {QualityScore.GoodFor(stats)}");

            _tileLoss.SetData(
                $"{stats.LossPercent:0.#}%",
                $"{stats.Received}/{stats.Sent} replies");

            _graph.AddPoint(result.ElapsedSec, result.RoundtripMs);
        }

        private void OnSnapshot(DashboardSnapshot snap)
        {
            if (IsDisposed) return;

            _tileDown.SetData(FormatRate(snap.DownloadMbps));
            _tileUp.SetData(FormatRate(snap.UploadMbps));

            _connectionValues["INTERFACE"].Text = snap.InterfaceName;
            _connectionValues["TYPE"].Text = snap.InterfaceType;
            _connectionValues["IP ADDRESS"].Text = snap.IpAddress;
            _connectionValues["GATEWAY"].Text = snap.Gateway;
            _connectionValues["DNS SERVER"].Text = snap.DnsServer;
            _connectionValues["MAC ADDRESS"].Text = snap.MacAddress;
        }

        /// <summary>Below 0.1 Mbps the link is effectively idle, not "slow".</summary>
        private static string FormatRate(double mbps) =>
            mbps < 0.1 ? "Idle" : $"{mbps:0.0} Mbps";

        /// <summary>Connection grade as the Latency card's badge.</summary>
        private void ApplyGrade(char? grade)
        {
            var theme = themeManager.CurrentTheme;
            _tileLatency.Badge = grade.HasValue ? $"Grade {grade}" : "";
            _tileLatency.BadgeColor = QualityScore.Level(grade) switch
            {
                QualityLevel.Good => theme.SuccessColor,
                QualityLevel.Fair => theme.WarningColor,
                QualityLevel.Poor => theme.ErrorColor,
                _ => null
            };
        }

        private void ApplyBadgeTheme()
        {
            var theme = themeManager.CurrentTheme;
            _badge.ForeColor = _dashboardService.IsRunning
                ? theme.SuccessColor
                : theme.SecondaryTextColor;
        }

        private void DisposeServices()
        {
            _servicesDisposed = true;
            _dashboardService.Updated -= OnSnapshot;
            _pingService.ReplyReceived -= OnMonitorReply;
            _dashboardService.Dispose();
            _pingService.Dispose();
        }
    }
}
