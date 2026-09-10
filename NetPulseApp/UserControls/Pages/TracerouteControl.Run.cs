// UserControls/Pages/TracerouteControl.Run.cs
using DeviceDataModule;
using NetPulseApp.Managers;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;
using System.Net;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Traceroute page run logic: button wiring, run lifecycle, service
    /// events, summary-card state, persisted target history, and report
    /// building. Layout/theme live in TracerouteControl.cs.
    /// </summary>
    public partial class TracerouteControl
    {
        private int _hopCount;
        private int _replyHops;          // hops that answered, for the average
        private double _rttSum;
        private string _finalAddress = "";
        private Color _statusColor = Color.Gray;

        // ─────────────────────────────────────────────────────────────────────
        // RUN CONTROL
        // ─────────────────────────────────────────────────────────────────────

        private void WireButtons()
        {
            _start.Click += (s, e) => StartRun();
            _stop.Click += (s, e) => { _service.Stop(); _stop.Enabled = false; };
            _copy.Click += (s, e) => ToolUiFactory.CopyResults(_table.GetAsText());
            _export.Click += (s, e) => ToolUiFactory.ExportReport(FindForm(),
                $"NetPulse_Traceroute_{DateTime.Now:yyyyMMdd_HHmm}.txt", BuildReport());
            _clear.Click += (s, e) =>
            {
                if (_service.IsRunning) return; // keep the log while tracing
                _table.ClearRows();
                ResetSummary();
            };
            _target.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; StartRun(); }
            };
        }

        private void StartRun()
        {
            string target = _target.Text.Trim();
            if (target.Length == 0)
            {
                ToastsNotificationManager.Instance.ShowInfoNotification("Enter a host name or IP address first.");
                return;
            }
            if (_service.IsRunning)
                return;

            int maxHops = SelectedValue(_maxHops, 30);
            int timeoutMs = SelectedValue(_timeout, 3000);

            _table.ClearRows();
            ResetSummary();
            SaveHistory(target);
            SetRunning(true);
            SetStatus("● Running", themeManager.CurrentTheme.AccentColor,
                $"Tracing route to {target}…");
            _service.Start(target, maxHops, timeoutMs);
        }

        private static int SelectedValue(ModernDropDown drop, int fallback) =>
            drop.SelectedItem is ToolUiFactory.LabeledValue lv ? lv.Value : fallback;

        private void SetRunning(bool running)
        {
            _start.Enabled = !running;
            _stop.Enabled = running;
            _target.Enabled = !running;
            _maxHops.Enabled = !running;
            _timeout.Enabled = !running;
            _protocol.Enabled = !running;
            _clear.Enabled = !running;
        }

        // ─────────────────────────────────────────────────────────────────────
        // SERVICE EVENTS + SUMMARY STATE
        // ─────────────────────────────────────────────────────────────────────

        private void WireService()
        {
            _service.HopReceived += OnHop;
            _service.RunError += (msg) => ToastsNotificationManager.Instance.ShowErrorNotification(msg);
            _service.RunFinished += OnRunFinished;
        }

        private void OnHop(TraceHopResult hop)
        {
            if (IsDisposed) return;

            var theme = themeManager.CurrentTheme;
            _hopCount++;
            if (hop.RttMs.HasValue)
            {
                _replyHops++;
                _rttSum += hop.RttMs.Value;
            }
            if (hop.IsDestination)
                _finalAddress = hop.Address;

            // Accent the response cell only where it matters — green at the
            // destination, warning for a hop that never answered. Normal
            // hops stay plain (Ping-style accent, used sparingly).
            int accentCol = hop.IsDestination || !hop.RttMs.HasValue ? 3 : -1;
            Color? accent = hop.IsDestination ? theme.SuccessColor
                : !hop.RttMs.HasValue ? theme.WarningColor
                : null;
            _table.AddRow(new TableRow(new[]
            {
                hop.Hop.ToString(),
                hop.Address.Length > 0 ? hop.Address : "*",
                hop.Hostname.Length > 0 ? hop.Hostname : "—",
                hop.RttMs.HasValue ? $"{hop.RttMs:0} ms" : "—",
                hop.Address.Length > 0 ? ClassifyLocation(hop.Address) : "—"
            }, accentCol, accent));

            _lblHops.Text = _hopCount.ToString();
            _lblAvgTime.Text = _replyHops > 0 ? $"{_rttSum / _replyHops:0} ms" : "— ms";
            if (!_lblDestination.Text.Contains('/'))
                _lblDestination.Text = _target.Text.Trim();
            _lblMessage.Text = hop.IsDestination
                ? $"Reached {hop.Address} at hop {hop.Hop}."
                : $"Hop {hop.Hop}: {(hop.Address.Length > 0 ? hop.Address : "request timed out")}";
        }

        private void OnRunFinished(bool reached)
        {
            if (IsDisposed) return;

            var theme = themeManager.CurrentTheme;
            SetRunning(false);

            if (reached)
            {
                SetStatus("● Completed", theme.SuccessColor, "Trace completed successfully.");
                _lblDestination.Text = $"{_target.Text.Trim()} / {_finalAddress}";
            }
            else
            {
                SetStatus("● Max hops", theme.WarningColor,
                    $"Destination not reached — stopped after {_hopCount} hops.");
            }
        }

        /// <summary>Status pill color. Kept as a field so a theme switch can
        /// re-apply whatever the current state's color maps to.</summary>
        private void SetStatus(string text, Color color, string message)
        {
            _lblStatus.Text = text;
            _statusColor = color;
            _lblStatus.ForeColor = color;
            _lblMessage.Text = message;
        }

        private void ApplyStatusTheme() => _lblStatus.ForeColor = _statusColor;

        private void ResetSummary()
        {
            _hopCount = 0;
            _replyHops = 0;
            _rttSum = 0;
            _finalAddress = "";
            _lblHops.Text = "0";
            _lblDestination.Text = "—";
            _lblAvgTime.Text = "— ms";
            SetStatus("● Ready", themeManager.CurrentTheme.SecondaryTextColor,
                "Enter a target and press Start to trace the route.");
        }

        /// <summary>Offline guess for the LOCATION / ISP cell — no geo API
        /// dependency, just the obvious local-range cases.</summary>
        private static string ClassifyLocation(string address)
        {
            if (!IPAddress.TryParse(address, out var ip))
                return "—";
            if (IPAddress.IsLoopback(ip))
                return "Local";
            if (IsPrivateV4(ip))
                return "Private network";
            return "—";
        }

        private static bool IsPrivateV4(IPAddress ip)
        {
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return false;
            var b = ip.GetAddressBytes();
            return b[0] == 10                                    // 10.0.0.0/8
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)     // 172.16/12
                || (b[0] == 192 && b[1] == 168)                  // 192.168/16
                || (b[0] == 169 && b[1] == 254);                 // link-local
        }

        // ─────────────────────────────────────────────────────────────────────
        // TARGET HISTORY (persisted via SettingsService)
        // ─────────────────────────────────────────────────────────────────────

        private void LoadHistory()
        {
            foreach (var t in SettingsService.Current.TraceTargets)
                _target.Items.Add(t);
            _target.Text = _target.Items.Count > 0 ? (string)_target.Items[0] : "google.com";
        }

        private void SaveHistory(string target)
        {
            var list = SettingsService.Current.TraceTargets;
            list.Remove(target);
            list.Insert(0, target);
            while (list.Count > 8)
                list.RemoveAt(list.Count - 1);
            SettingsService.Save();

            _target.Items.Clear();
            foreach (var t in list)
                _target.Items.Add(t);
            _target.Text = target;
        }

        // ─────────────────────────────────────────────────────────────────────
        // REPORT
        // ─────────────────────────────────────────────────────────────────────

        private string BuildReport()
        {
            string summary = _hopCount > 0
                ? $"Hops {_hopCount} | Destination {_lblDestination.Text} | " +
                  $"Avg {_lblAvgTime.Text} | Status {_lblStatus.Text.TrimStart('●', ' ')}"
                : "No data";
            return $"NetPulse Toolkit — Traceroute Report\n" +
                   $"Target: {_target.Text.Trim()}\n" +
                   $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Summary: {summary}\n\n" +
                   _table.GetAsText();
        }
    }
}
