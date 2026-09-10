// UserControls/Pages/PingControl.cs
using NetPulseApp.Managers;
using NetPulseApp.Model;
using NetPulseApp.Service.Ping;
using NetPulseApp.UserControls.Pages.Ping;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Ping page shell. Composes PingInputPanel + PingResultPanel +
    /// PingSummaryPanel and orchestrates PingService.
    /// Replaces TabOneControl in AppConfig.NavItems.
    /// </summary>
    public sealed class PingControl : UserControl
    {
        // ── Sub-panels ────────────────────────────────────────────────────────
        private readonly PingInputPanel _input;
        private readonly PingResultPanel _results;
        private readonly PingSummaryPanel _summary;

        // ── Service & state ───────────────────────────────────────────────────
        private readonly PingService _service = new();
        private CancellationTokenSource? _cts;
        private bool _running;

        // ── Counters ──────────────────────────────────────────────────────────
        private int _sent, _received, _lost;
        private long _totalMs;
        private readonly List<long> _rtts = new();

        private readonly ThemeManager _theme = ThemeManager.Instance;

        public PingControl()
        {
            _input = new PingInputPanel();
            _results = new PingResultPanel();
            _summary = new PingSummaryPanel();

            _input.StartRequested += OnStartPing;
            _input.StopRequested += OnStopPing;
            _theme.ThemeChanged += (s, e) => ApplyTheme();

            BuildLayout();
            ApplyTheme();
        }

        // ── Layout ────────────────────────────────────────────────────────────

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(16);

            // Page title
            var lblTitle = new Label
            {
                Text = "Ping",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var lblSub = new Label
            {
                Text = "Test reachability and measure response time to any host.",
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                Location = new Point(2, 34)
            };

            // How it works button (top-right)
            var btnHow = new Button
            {
                Text = "📖  How it works?",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnHow.FlatAppearance.BorderSize = 1;

            // Cards
            _input.Location = new Point(0, 66);
            _input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _results.Location = new Point(0, 230);
            _results.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                                AnchorStyles.Right | AnchorStyles.Bottom;

            _summary.Location = new Point(0, 0);  // positioned in Resize
            _summary.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.AddRange(new Control[]
                { lblTitle, lblSub, btnHow, _input, _results, _summary });

            Resize += OnResize;
        }

        private void OnResize(object? s, EventArgs e)
        {
            int w = Width - Padding.Horizontal;
            int h = Height - Padding.Vertical;

            _input.Width = w;
            _input.Height = 150;

            _summary.Width = w;
            _summary.Height = 140;
            _summary.Top = h - 140;

            _results.Width = w;
            _results.Top = 230;
            _results.Height = _summary.Top - 240;

            // How it works button top-right
            foreach (Control c in Controls)
                if (c is Button b && b.Text.Contains("How"))
                    b.Location = new Point(w - b.Width, 0);
        }

        // ── Ping orchestration ────────────────────────────────────────────────

        private async void OnStartPing(object? sender, EventArgs e)
        {
            if (_running) return;
            _running = true;
            _input.SetRunning(true);
            ResetCounters();

            _cts = new CancellationTokenSource();
            var progress = new Progress<PingResult>(OnPingResult);

            _results.Clear();
            _results.AppendHeader(
                _input.TargetHost, "resolving...", _input.PacketBytes);

            try
            {
                await _service.RunAsync(
                    _input.TargetHost,
                    _input.PingCount,
                    _input.PacketBytes,
                    _input.TimeoutMs,
                    _input.ResolveHostname,
                    _input.UseIPv6,
                    progress,
                    _cts.Token);
            }
            catch (OperationCanceledException) { /* user cancelled — normal */ }
            catch (Exception ex)
            {
                _results.AppendResult(new PingResult
                { Success = false, StatusText = ex.Message, Sequence = 0 });
            }
            finally
            {
                AppendFinalStats();
                _running = false;
                _input.SetRunning(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void OnStopPing(object? sender, EventArgs e) =>
            _cts?.Cancel();

        private void OnPingResult(PingResult r)
        {
            _sent++;
            if (r.Success)
            {
                _received++;
                _totalMs += r.RoundtripMs;
                _rtts.Add(r.RoundtripMs);
            }
            else
            {
                _lost++;
                _rtts.Add(-1);
            }

            _results.AppendResult(r);

            long avg = _received > 0 ? _totalMs / _received : -1;
            _summary.Update(_sent, _received, _lost, avg, _rtts);
        }

        private void AppendFinalStats()
        {
            if (_sent == 0) return;
            var validRtts = _rtts.Where(r => r >= 0).ToList();
            long min = validRtts.Count > 0 ? validRtts.Min() : 0;
            long max = validRtts.Count > 0 ? validRtts.Max() : 0;
            long avg = validRtts.Count > 0 ? (long)validRtts.Average() : 0;

            string ip = _rtts.Count > 0 ? _input.TargetHost : "—";
            _results.AppendStats(ip, _sent, _received, _lost, min, max, avg);
        }

        private void ResetCounters()
        {
            _sent = _received = _lost = 0;
            _totalMs = 0;
            _rtts.Clear();
            _summary.Reset();
        }

        // ── Theme ─────────────────────────────────────────────────────────────

        private void ApplyTheme()
        {
            var t = _theme.CurrentTheme;
            BackColor = t.ContentBackground;

            foreach (Control c in Controls)
                c.ForeColor = t.TextColor;

            _input.ApplyTheme();
            _results.ApplyTheme();
            _summary.ApplyTheme();
        }
    }
}
