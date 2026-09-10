// UserControls/Pages/DnsLookupControl.Run.cs
using DeviceDataModule;
using DnsClient;
using NetPulseApp.Managers;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// DNS Lookup page run logic: button wiring, lookup lifecycle, service
    /// events, summary-strip state, persisted domain history, and report
    /// building. Layout/theme live in DnsLookupControl.cs.
    /// </summary>
    public partial class DnsLookupControl
    {
        private int _recordCount;
        private System.Diagnostics.Stopwatch _clock;
        private Color _statusColor = Color.Gray;

        // ─────────────────────────────────────────────────────────────────────
        // RUN CONTROL
        // ─────────────────────────────────────────────────────────────────────

        private void WireButtons()
        {
            _lookup.Click += (s, e) => StartLookup();
            _copy.Click += (s, e) => ToolUiFactory.CopyResults(_table.GetAsText());
            _export.Click += (s, e) => ToolUiFactory.ExportReport(FindForm(),
                $"NetPulse_DNS_{DateTime.Now:yyyyMMdd_HHmm}.txt", BuildReport());
            _clear.Click += (s, e) =>
            {
                if (_service.IsRunning) return; // keep rows while resolving
                _table.ClearRows();
                ResetSummary();
            };
            _domain.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; StartLookup(); }
            };
        }

        private void StartLookup()
        {
            string domain = _domain.Text.Trim();
            if (domain.Length == 0)
            {
                ToastsNotificationManager.Instance.ShowInfoNotification("Enter a domain name first.");
                return;
            }
            if (_service.IsRunning)
                return;

            // LabeledValue carries the wire value of DnsClient's QueryType
            // (0 = query the whole common set).
            var typeValue = _recordType.SelectedItem is ToolUiFactory.LabeledValue lv ? lv.Value : 0;
            QueryType? queryType = typeValue == 0 ? null : (QueryType)typeValue;

            // "8.8.8.8 (Google)" → "8.8.8.8"; "Default" stays "Default".
            string server = _dnsServer.SelectedItem as string ?? "Default";
            server = server.Split(' ')[0];

            _table.ClearRows();
            _recordCount = 0;
            _clock = System.Diagnostics.Stopwatch.StartNew();
            SaveHistory(domain);
            SetRunning(true);
            SetStatus("● Running", themeManager.CurrentTheme.AccentColor,
                $"Resolving {domain}…");
            _service.Start(domain, queryType, server);
        }

        private void SetRunning(bool running)
        {
            _lookup.Enabled = !running;
            _domain.Enabled = !running;
            _recordType.Enabled = !running;
            _dnsServer.Enabled = !running;
            _clear.Enabled = !running;
        }

        // ─────────────────────────────────────────────────────────────────────
        // SERVICE EVENTS + SUMMARY STATE
        // ─────────────────────────────────────────────────────────────────────

        private void WireService()
        {
            _service.RecordReceived += OnRecord;
            _service.RunFinished += OnRunFinished;
        }

        private void OnRecord(DnsRecordResult record)
        {
            if (IsDisposed) return;

            var theme = themeManager.CurrentTheme;
            _recordCount++;

            // Error rows read as errors; everything else stays plain so the
            // TYPE column doesn't turn into a rainbow.
            bool isError = record.Type == "Error";
            _table.AddRow(new TableRow(new[]
            {
                record.Type,
                record.Value,
                record.Ttl,
                record.Extra
            }, 0, isError ? theme.ErrorColor : null));

            _lblRecords.Text = _recordCount.ToString();
            _lblQueryTime.Text = $"{_clock.ElapsedMilliseconds} ms";
        }

        private void OnRunFinished(bool any)
        {
            if (IsDisposed) return;

            var theme = themeManager.CurrentTheme;
            SetRunning(false);
            _clock.Stop();
            _lblQueryTime.Text = $"{_clock.ElapsedMilliseconds} ms";

            if (any)
                SetStatus("● Complete", theme.SuccessColor, "Lookup completed.");
            else
                SetStatus("● No records", theme.WarningColor,
                    "No matching records were found for this domain.");
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
            _recordCount = 0;
            _clock = System.Diagnostics.Stopwatch.StartNew();
            _clock.Stop();
            _lblRecords.Text = "0";
            _lblQueryTime.Text = "— ms";
            SetStatus("● Ready", themeManager.CurrentTheme.SecondaryTextColor,
                "Enter a domain and press Lookup to resolve its records.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // DOMAIN HISTORY (persisted via SettingsService)
        // ─────────────────────────────────────────────────────────────────────

        private void LoadHistory()
        {
            foreach (var t in SettingsService.Current.DnsTargets)
                _domain.Items.Add(t);
            _domain.Text = _domain.Items.Count > 0 ? (string)_domain.Items[0] : "example.com";
        }

        private void SaveHistory(string domain)
        {
            var list = SettingsService.Current.DnsTargets;
            list.Remove(domain);
            list.Insert(0, domain);
            while (list.Count > 8)
                list.RemoveAt(list.Count - 1);
            SettingsService.Save();

            _domain.Items.Clear();
            foreach (var t in list)
                _domain.Items.Add(t);
            _domain.Text = domain;
        }

        // ─────────────────────────────────────────────────────────────────────
        // REPORT
        // ─────────────────────────────────────────────────────────────────────

        private string BuildReport()
        {
            // ModernDropDown doesn't forward Text — read the selected item.
            string type = _recordType.SelectedItem?.ToString() ?? "A";
            string server = _dnsServer.SelectedItem as string ?? "Default";
            string summary = _recordCount > 0
                ? $"Records {_recordCount} | Query time {_lblQueryTime.Text} | " +
                  $"Type {type} | Server {server} | Status {_lblStatus.Text.TrimStart('●', ' ')}"
                : "No data";
            return $"NetPulse Toolkit — DNS Lookup Report\n" +
                   $"Domain: {_domain.Text.Trim()}\n" +
                   $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Summary: {summary}\n\n" +
                   _table.GetAsText();
        }
    }
}
