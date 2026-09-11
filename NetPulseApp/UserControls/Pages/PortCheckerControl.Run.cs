// UserControls/Pages/PortCheckerControl.Run.cs
using DeviceDataModule;
using NetPulseApp.Managers;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Port Checker page run logic: button wiring, run lifecycle, service
    /// events, port-list parsing, preset bundles, summary state, persisted
    /// host history, and report building. Layout/theme live in
    /// PortCheckerControl.cs.
    /// </summary>
    public partial class PortCheckerControl
    {
        private const int MaxPorts = 64;

        private int _open;
        private int _closed;
        private int _filtered;
        private bool _stopRequested;
        private Color _statusColor = Color.Gray;

        // Preset bundles — indices match the Presets dropdown's values.
        private static readonly string[] Presets =
        {
            "80, 443",
            "25, 110, 143, 993, 995",
            "1433, 3306, 5432",
            "22, 3389"
        };

        /// <summary>Well-known ports for the SERVICE column (IANA subset).</summary>
        private static readonly Dictionary<int, string> Services = new()
        {
            [20] = "FTP data", [21] = "FTP", [22] = "SSH", [23] = "Telnet",
            [25] = "SMTP", [53] = "DNS", [80] = "HTTP", [110] = "POP3",
            [123] = "NTP", [143] = "IMAP", [161] = "SNMP", [389] = "LDAP",
            [443] = "HTTPS", [445] = "SMB", [465] = "SMTPS", [587] = "SMTP",
            [993] = "IMAPS", [995] = "POP3S", [1433] = "SQL Server",
            [1521] = "Oracle", [3306] = "MySQL", [3389] = "RDP",
            [5432] = "PostgreSQL", [5900] = "VNC", [6379] = "Redis",
            [8080] = "HTTP alt", [8443] = "HTTPS alt", [27017] = "MongoDB"
        };

        // ─────────────────────────────────────────────────────────────────────
        // RUN CONTROL
        // ─────────────────────────────────────────────────────────────────────

        private void WireButtons()
        {
            _check.Click += (s, e) => StartCheck();
            _stop.Click += (s, e) => { _stopRequested = true; _service.Stop(); _stop.Enabled = false; };
            _copy.Click += (s, e) => ToolUiFactory.CopyResults(_table.GetAsText());
            _export.Click += (s, e) => ToolUiFactory.ExportReport(FindForm(),
                $"NetPulse_Ports_{DateTime.Now:yyyyMMdd_HHmm}.txt", BuildReport());
            _clear.Click += (s, e) =>
            {
                if (_service.IsRunning) return; // keep rows while probing
                _table.ClearRows();
                ResetSummary();
            };
            // Presets are quick-fill — they replace the port list, which
            // stays editable afterwards. -1 = "Custom", already applied.
            _preset.SelectionChanged += (s, e) =>
            {
                if (_preset.SelectedItem is ToolUiFactory.LabeledValue lv && lv.Value >= 0)
                    _ports.Text = Presets[lv.Value];
            };
            _host.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; StartCheck(); }
            };
            _ports.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; StartCheck(); }
            };
        }

        private void StartCheck()
        {
            string host = _host.Text.Trim();
            if (host.Length == 0)
            {
                ToastsNotificationManager.Instance.ShowInfoNotification("Enter a host name or IP address first.");
                return;
            }

            var ports = ParsePorts(_ports.Text);
            if (ports.Count == 0)
            {
                ToastsNotificationManager.Instance.ShowInfoNotification(
                    "Enter at least one valid port (1-65535), e.g. \"80, 443\" or \"8000-8004\".");
                return;
            }
            if (_service.IsRunning)
                return;

            int timeoutMs = _timeout.SelectedItem is ToolUiFactory.LabeledValue tv ? tv.Value : 2000;

            _table.ClearRows();
            _open = _closed = _filtered = 0;
            _stopRequested = false;
            SaveHistory(host);
            SetRunning(true);
            SetStatus("● Checking", themeManager.CurrentTheme.AccentColor,
                $"Checking {ports.Count} ports on {host}…");
            _service.Start(host, ports, timeoutMs);
        }

        /// <summary>"80, 443, 8000-8004" → sorted distinct ports, capped
        /// at MaxPorts so a runaway range can't flood the table.</summary>
        internal static List<int> ParsePorts(string text)
        {
            var ports = new SortedSet<int>();
            foreach (var part in text.Split(',', ';'))
            {
                var token = part.Trim();
                if (token.Length == 0) continue;

                int a = token.IndexOf('-');
                if (a > 0 && token.IndexOf('-', a + 1) < 0 &&
                    int.TryParse(token[..a].Trim(), out int lo) &&
                    int.TryParse(token[(a + 1)..].Trim(), out int hi) &&
                    lo >= 1 && hi >= lo && hi <= 65535)
                {
                    for (int p = lo; p <= hi && ports.Count < MaxPorts; p++)
                        if (p >= 1) ports.Add(p);
                }
                else if (int.TryParse(token, out int port) && port >= 1 && port <= 65535)
                {
                    ports.Add(port);
                }
            }
            while (ports.Count > MaxPorts)
                ports.Remove(ports.Max);
            return ports.ToList();
        }

        private void SetRunning(bool running)
        {
            _check.Enabled = !running;
            _stop.Enabled = running;
            _host.Enabled = !running;
            _ports.Enabled = !running;
            _preset.Enabled = !running;
            _timeout.Enabled = !running;
            _clear.Enabled = !running;
        }

        // ─────────────────────────────────────────────────────────────────────
        // SERVICE EVENTS + SUMMARY STATE
        // ─────────────────────────────────────────────────────────────────────

        private void WireService()
        {
            _service.PortChecked += OnPortChecked;
            _service.RunError += (msg) => ToastsNotificationManager.Instance.ShowErrorNotification(msg);
            _service.RunFinished += OnRunFinished;
        }

        private void OnPortChecked(PortCheckResult result)
        {
            if (IsDisposed) return;

            var theme = themeManager.CurrentTheme;
            switch (result.Status)
            {
                case "Open": _open++; break;
                case "Closed": _closed++; break;
                default: _filtered++; break;
            }

            Color accent = result.Status switch
            {
                "Open" => theme.SuccessColor,
                "Closed" => theme.ErrorColor,
                _ => theme.WarningColor
            };
            _table.AddRow(new TableRow(new[]
            {
                result.Port.ToString(),
                "TCP",
                result.Status,
                Services.TryGetValue(result.Port, out var svc) ? svc : "—",
                $"{result.ElapsedMs} ms"
            }, 2, accent));

            _lblOpen.Text = _open.ToString();
            _lblClosed.Text = _closed.ToString();
            _lblFiltered.Text = _filtered.ToString();
        }

        private void OnRunFinished(bool any)
        {
            if (IsDisposed) return;

            var theme = themeManager.CurrentTheme;
            SetRunning(false);

            if (_stopRequested)
                SetStatus("● Stopped", theme.WarningColor, "Check stopped — results may be incomplete.");
            else if (any)
                SetStatus("● Complete", theme.SuccessColor, "Check completed.");
            else
                SetStatus("● No results", theme.WarningColor, "No probe results — try a longer timeout.");
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
            _open = _closed = _filtered = 0;
            _stopRequested = false;
            _lblOpen.Text = "0";
            _lblClosed.Text = "0";
            _lblFiltered.Text = "0";
            SetStatus("● Ready", themeManager.CurrentTheme.SecondaryTextColor,
                "Enter a host and ports, then press Check.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // HOST HISTORY (persisted via SettingsService)
        // ─────────────────────────────────────────────────────────────────────

        private void LoadHistory()
        {
            foreach (var t in SettingsService.Current.PortCheckerHosts)
                _host.Items.Add(t);
            _host.Text = _host.Items.Count > 0 ? (string)_host.Items[0] : "example.com";
        }

        private void SaveHistory(string host)
        {
            var list = SettingsService.Current.PortCheckerHosts;
            list.Remove(host);
            list.Insert(0, host);
            while (list.Count > 8)
                list.RemoveAt(list.Count - 1);
            SettingsService.Save();

            _host.Items.Clear();
            foreach (var t in list)
                _host.Items.Add(t);
            _host.Text = host;
        }

        // ─────────────────────────────────────────────────────────────────────
        // REPORT
        // ─────────────────────────────────────────────────────────────────────

        private string BuildReport()
        {
            string summary = _open + _closed + _filtered > 0
                ? $"Open {_open} | Closed {_closed} | Filtered {_filtered} | " +
                  $"Status {_lblStatus.Text.TrimStart('●', ' ')}"
                : "No data";
            return $"NetPulse Toolkit — Port Checker Report\n" +
                   $"Host: {_host.Text.Trim()}\n" +
                   $"Ports: {_ports.Text.Trim()}\n" +
                   $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Summary: {summary}\n\n" +
                   _table.GetAsText();
        }
    }
}
