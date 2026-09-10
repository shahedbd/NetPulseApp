// UserControls/Pages/PingControl.Run.cs
using DeviceDataModule;
using NetPulseApp.Managers;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Ping page run logic: button wiring, run lifecycle, service events,
    /// persisted target history, and report building. Layout/theme live in
    /// PingControl.cs.
    /// </summary>
    public partial class PingControl
    {
        // ─────────────────────────────────────────────────────────────────────
        // RUN CONTROL
        // ─────────────────────────────────────────────────────────────────────

        private void WireButtons()
        {
            _start.Click += (s, e) => StartRun();
            _stop.Click += (s, e) => { _service.Stop(); _stop.Enabled = false; };
            _copy.Click += (s, e) => ToolUiFactory.CopyResults(_table.GetAsText());
            _export.Click += (s, e) => ToolUiFactory.ExportReport(FindForm(),
                $"NetPulse_Ping_{DateTime.Now:yyyyMMdd_HHmm}.txt", BuildReport());
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

            int intervalMs = SelectedValue(_interval, 1000);
            int count = _cont.Checked ? 0 : SelectedValue(_count, 4);

            _table.ClearRows();
            _graph.Clear();
            _lastStats = null;
            SaveHistory(target);
            SetRunning(true);
            _service.Start(target, intervalMs, count);
        }

        private static int SelectedValue(ModernDropDown drop, int fallback) =>
            drop.SelectedItem is ToolUiFactory.LabeledValue lv ? lv.Value : fallback;

        private void SetRunning(bool running)
        {
            _start.Enabled = !running;
            _stop.Enabled = running;
            _target.Enabled = !running;
            _interval.Enabled = !running;
            _cont.Enabled = !running;
            _fixed.Enabled = !running;
            _count.Enabled = !running && _fixed.Checked;
            _graph.SetLive(running);
        }

        // ─────────────────────────────────────────────────────────────────────
        // SERVICE EVENTS
        // ─────────────────────────────────────────────────────────────────────

        private void WireService()
        {
            _service.ReplyReceived += OnReply;
            _service.RunError += (msg) => ToastsNotificationManager.Instance.ShowErrorNotification(msg);
            _service.RunFinished += () => SetRunning(false);
        }

        private void OnReply(PingResult result, PingStats stats)
        {
            if (IsDisposed) return;

            _lastStats = stats;
            var theme = themeManager.CurrentTheme;
            Color accent = result.RoundtripMs.HasValue ? theme.SuccessColor : theme.ErrorColor;
            if (!result.RoundtripMs.HasValue && result.Status != "Timeout")
                accent = theme.WarningColor; // Unknown host / unreachable / errors

            _table.AddRow(new TableRow(new[]
            {
                result.Seq.ToString(),
                result.RoundtripMs.HasValue ? $"{result.RoundtripMs} ms" : "—",
                result.Ttl.HasValue ? result.Ttl.ToString() : "—",
                result.Status
            }, 3, accent));

            _graph.AddPoint(result.ElapsedSec, result.RoundtripMs);
            _stats.Text = $"Sent: {stats.Sent}   Received: {stats.Received}   " +
                          $"Lost: {stats.Lost} ({stats.LossPercent:0.#}%)   " +
                          $"Min/Avg/Max: {stats.MinAvgMaxText}";
        }

        private void ResetStatsLabel() =>
            _stats.Text = "Sent: 0   Received: 0   Lost: 0 (0%)   Min/Avg/Max: —";

        // ─────────────────────────────────────────────────────────────────────
        // TARGET HISTORY (persisted via SettingsService)
        // ─────────────────────────────────────────────────────────────────────

        private void LoadHistory()
        {
            foreach (var t in SettingsService.Current.PingTargets)
                _target.Items.Add(t);
            _target.Text = _target.Items.Count > 0 ? (string)_target.Items[0] : "google.com";
        }

        private void SaveHistory(string target)
        {
            var list = SettingsService.Current.PingTargets;
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
            string stats = _lastStats != null
                ? $"Sent {_lastStats.Sent} | Received {_lastStats.Received} | Lost {_lastStats.Lost} ({_lastStats.LossPercent:0.#}%) | Min/Avg/Max {_lastStats.MinAvgMaxText}"
                : "No data";
            return $"NetPulse Toolkit — Ping Report\n" +
                   $"Target: {_target.Text.Trim()}\n" +
                   $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Stats: {stats}\n\n" +
                   _table.GetAsText();
        }
    }
}
