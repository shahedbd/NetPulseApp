// UserControls/Pages/WhoisControl.Run.cs
using DeviceDataModule;
using NetPulseApp.Managers;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// WHOIS IP page run logic: button wiring, lookup lifecycle, service
    /// events, summary-strip state, persisted target history, and report
    /// building. Layout/theme live in WhoisControl.cs.
    /// </summary>
    public partial class WhoisControl
    {
        private string _expires = "—";
        private Color _statusColor = Color.Gray;

        // ─────────────────────────────────────────────────────────────────────
        // RUN CONTROL
        // ─────────────────────────────────────────────────────────────────────

        private void WireButtons()
        {
            _lookup.Click += (s, e) => StartLookup();
            _copy.Click += (s, e) => ToolUiFactory.CopyResults(_table.GetAsText());
            _export.Click += (s, e) => ToolUiFactory.ExportReport(FindForm(),
                $"NetPulse_WHOIS_{DateTime.Now:yyyyMMdd_HHmm}.txt", BuildReport());
            _clear.Click += (s, e) =>
            {
                if (_service.IsRunning) return; // keep rows while resolving
                _table.ClearRows();
                ResetSummary();
            };
            _target.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; StartLookup(); }
            };
        }

        private void StartLookup()
        {
            string target = _target.Text.Trim();
            if (target.Length == 0)
            {
                ToastsNotificationManager.Instance.ShowInfoNotification("Enter an IP address or domain first.");
                return;
            }
            if (_service.IsRunning)
                return;

            _table.ClearRows();
            _expires = "—";
            SaveHistory(target);
            SetRunning(true);
            SetStatus("● Running", themeManager.CurrentTheme.AccentColor,
                $"Querying registration data for {target}…");
            _service.Start(target);
        }

        private void SetRunning(bool running)
        {
            _lookup.Enabled = !running;
            _target.Enabled = !running;
            _clear.Enabled = !running;
        }

        // ─────────────────────────────────────────────────────────────────────
        // SERVICE EVENTS + SUMMARY STATE
        // ─────────────────────────────────────────────────────────────────────

        private void WireService()
        {
            _service.RecordReceived += OnRecord;
            _service.RunError += (msg) => ToastsNotificationManager.Instance.ShowErrorNotification(msg);
            _service.RunFinished += OnRunFinished;
        }

        private void OnRecord(WhoisRecord record)
        {
            if (IsDisposed) return;

            _table.AddRow(new TableRow(new[] { record.Field, record.Value }, -1, null));

            // Feed the summary tiles from the rows as they stream in.
            switch (record.Field)
            {
                case "Object type":
                    _lblType.Text = record.Value;
                    break;
                case "Expires":
                    _expires = record.Value;
                    _lblExpires.Text = record.Value;
                    break;
            }
        }

        private void OnRunFinished(bool any)
        {
            if (IsDisposed) return;

            var theme = themeManager.CurrentTheme;
            SetRunning(false);

            if (any)
                SetStatus("● Complete", theme.SuccessColor, "Lookup completed.");
            else
                SetStatus("● No data", theme.WarningColor,
                    "No registration data was returned for this target.");
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
            _expires = "—";
            _lblType.Text = "—";
            _lblExpires.Text = "—";
            SetStatus("● Ready", themeManager.CurrentTheme.SecondaryTextColor,
                "Enter an IP address or domain and press Lookup.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // TARGET HISTORY (persisted via SettingsService)
        // ─────────────────────────────────────────────────────────────────────

        private void LoadHistory()
        {
            foreach (var t in SettingsService.Current.WhoisTargets)
                _target.Items.Add(t);
            _target.Text = _target.Items.Count > 0 ? (string)_target.Items[0] : "example.com";
        }

        private void SaveHistory(string target)
        {
            var list = SettingsService.Current.WhoisTargets;
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
            string summary = _table.RowCount > 0
                ? $"Type {_lblType.Text} | Expires {_expires} | Status {_lblStatus.Text.TrimStart('●', ' ')}"
                : "No data";
            return $"NetPulse Toolkit — WHOIS IP Report\n" +
                   $"Target: {_target.Text.Trim()}\n" +
                   $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Summary: {summary}\n\n" +
                   _table.GetAsText();
        }
    }
}
