// UserControls/Pages/SpeedTestControl.Run.cs
using NetPulseApp.Helper;
using NetPulseApp.Model;
using NetPulseApp.Service;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;
using NetPulseApp.UserControls.Lib.DataCard;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Speed Test run logic (adapted from WiFiPasswordViewer's
    /// SpeedTestControl): button wiring, the SpeedTestService run with
    /// IProgress updates, card values, and the persisted history table.
    /// Layout/theme live in SpeedTestControl.cs.
    /// </summary>
    public partial class SpeedTestControl
    {
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private bool _phaseFailed;
        private string _serverLabel = "Cloudflare";

        private void WireButtons()
        {
            _start.Click += async (s, e) => await StartTestAsync();
            _cancel.Click += (s, e) => { _cts?.Cancel(); _cancel.Enabled = false; };
            _copy.Click += (s, e) => ToolUiFactory.CopyResults(_table.GetAsText());
            _clear.Click += (s, e) =>
            {
                if (_isRunning) return;
                TestHistoryStore.Clear();
                _table.ClearRows();
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // RUN CONTROL
        // ─────────────────────────────────────────────────────────────────────

        private async Task StartTestAsync()
        {
            if (_isRunning) return;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            SetRunning(true);
            ResetDisplay();
            _ = ResolveServerAsync();

            var result = await _service.RunFullTestAsync(new Progress<SpeedTestProgress>(OnProgress), _cts.Token);
            if (IsDisposed) return;

            SetRunning(false);
            if (result.Success)
            {
                ShowResult(result);
                TestHistoryStore.Append(result);
                AddHistoryRow(result);
            }
            else if (!string.IsNullOrEmpty(result.ErrorMessage))
                SetPhase(result.ErrorMessage, failed: true);
        }

        /// <summary>The Cloudflare edge serving this connection (e.g. "DAC"), for the SERVER column.</summary>
        private async Task ResolveServerAsync()
        {
            try
            {
                string colo = await IpDataService.Instance.GetTestServerAsync(_cts.Token);
                if (!string.IsNullOrEmpty(colo)) _serverLabel = $"Cloudflare ({colo})";
            }
            catch { /* keep the generic label */ }
        }

        private void OnProgress(SpeedTestProgress p)
        {
            if (IsDisposed) return;

            SetPhase(p.StatusText, failed: p.Phase == SpeedTestPhase.Failed);
            int trackW = _progressTrack.Width;
            _progressFill.Width = trackW > 0 ? (int)(trackW * p.Percent / 100.0) : 0;
            _progressFill.BackColor = p.Phase switch
            {
                SpeedTestPhase.Download => DataCardAccents.Green,
                SpeedTestPhase.Upload => DataCardAccents.Purple,
                _ => DataCardAccents.Blue
            };

            // Live sample in the active card; final values land in ShowResult.
            if (p.ValueMbps <= 0) return;
            if (p.Phase == SpeedTestPhase.Download)
                _tileDown.SetData($"{p.ValueMbps:0.0} Mbps", $"peak {p.PeakMbps:0.0} Mbps");
            else if (p.Phase == SpeedTestPhase.Upload)
                _tileUp.SetData($"{p.ValueMbps:0.0} Mbps", $"peak {p.PeakMbps:0.0} Mbps");
        }

        private void ShowResult(SpeedTestResult r)
        {
            _tileDown.SetData($"{r.DownloadMbps:0.0} Mbps", "final");
            _tileUp.SetData($"{r.UploadMbps:0.0} Mbps", "final");
            _tilePing.SetData($"{r.PingMs:0} ms", _serverLabel);
            _tileJitter.SetData($"{r.JitterMs:0} ms", "ping variation");
            r.ServerUsed = _serverLabel;
        }

        private void SetRunning(bool running)
        {
            _isRunning = running;
            _start.Enabled = !running;
            _cancel.Enabled = running;
            _clear.Enabled = !running;
            _start.Text = running ? "Testing…" : "Start Test";
        }

        private void ResetDisplay()
        {
            foreach (var card in new[] { _tileDown, _tileUp, _tilePing, _tileJitter })
                card.SetData("—", "testing…");
            _progressFill.Width = 0;
            SetPhase("Initialising…", failed: false);
        }

        private void SetPhase(string text, bool failed)
        {
            _lblPhase.Text = text;
            _phaseFailed = failed;
            ApplyPhaseTheme();
        }

        private void ApplyPhaseTheme()
        {
            var theme = themeManager.CurrentTheme;
            _lblPhase.ForeColor = _phaseFailed ? theme.ErrorColor : theme.SecondaryTextColor;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HISTORY
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Chronological like the other tool tables — the store returns
        /// newest first, so reverse; the table auto-scrolls to the newest row.</summary>
        private void LoadHistory()
        {
            foreach (var r in Enumerable.Reverse(TestHistoryStore.Load()))
                AddHistoryRow(r);
        }

        private void AddHistoryRow(SpeedTestResult r) =>
            _table.AddRow(new TableRow(new[]
            {
                r.TestedAt.ToString("yyyy-MM-dd HH:mm"),
                $"{r.DownloadMbps:0.0} Mbps",
                $"{r.UploadMbps:0.0} Mbps",
                $"{r.PingMs:0} ms",
                $"{r.JitterMs:0} ms",
                ServerText(r.ServerUsed)
            }, accentColumn: 1, accent: DataCardAccents.Green));

        /// <summary>Older entries stored the ping endpoint URL — show just its host.</summary>
        private static string ServerText(string server) =>
            Uri.TryCreate(server, UriKind.Absolute, out var uri) ? uri.Host : server;
    }
}
