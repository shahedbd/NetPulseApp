// UserControls/Pages/SpeedTestControl.cs
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;
using NetPulseApp.UserControls.Lib.DataCard;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Internet Speed Test page (engine copied from WiFiPasswordViewer —
    /// Cloudflare __down / __up endpoints): Start / Cancel, four DataCards
    /// (download, upload, ping, jitter), a phase + progress strip, and the
    /// saved test-history table (TestHistoryStore) with Copy / Clear.
    /// Layout/theme/teardown here; run logic in SpeedTestControl.Run.cs.
    /// </summary>
    public partial class SpeedTestControl : DpiAwareUserControl
    {
        private readonly SpeedTestService _service = new();

        private Label _title;
        private Label _subtitle;
        private IconButton _start;
        private IconButton _cancel;
        private DataCardGrid _statRow;
        private DataCardControl _tileDown;
        private DataCardControl _tileUp;
        private DataCardControl _tilePing;
        private DataCardControl _tileJitter;
        private Panel _progressStrip;
        private Label _lblPhase;
        private Panel _progressTrack;
        private Panel _progressFill;
        private Panel _tableCard;
        private ResultsTableControl _table;
        private IconButton _copy;
        private IconButton _clear;

        public SpeedTestControl()
        {
            BackColor = themeManager.CurrentTheme.ContentBackground;
            Padding = ScaledPadding(16);

            // Docking order per the sibling tool pages: Fill first, Bottom
            // footer next, then Top panels bottom-up.
            BuildTable();
            BuildFooter();
            BuildProgressStrip();
            BuildStatRow();
            BuildHeader();

            WireButtons();
            ApplyTheme();
            LoadHistory();

            themeManager.ThemeChanged += OnThemeChanged;
        }

        // ─────────────────────────────────────────────────────────────────────
        // LAYOUT
        // ─────────────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(64), BackColor = Color.Transparent };
            _title = ToolUiFactory.PageTitle("Speed Test");
            _title.Location = new Point(0, Scale(2));
            _subtitle = ToolUiFactory.PageSubtitle("Measure download, upload, ping and jitter against Cloudflare's speed servers.");
            _subtitle.Location = new Point(Scale(2), Scale(34));

            _start = ToolUiFactory.AccentButton("Start Test", IconChar.Play, themeManager.CurrentTheme.SuccessColor);
            _start.Size = new Size(Scale(120), Scale(34));
            _cancel = ToolUiFactory.GhostButton("Cancel", IconChar.Stop);
            _cancel.Size = new Size(Scale(100), Scale(34));
            _cancel.Enabled = false;

            panel.Controls.AddRange(new Control[] { _subtitle, _title, _start, _cancel });
            panel.Resize += (s, e) =>
            {
                _cancel.Location = new Point(panel.Width - _cancel.Width - Scale(2), Scale(10));
                _start.Location = new Point(_cancel.Left - _start.Width - Scale(8), Scale(10));
            };
            Controls.Add(panel);
        }

        private void BuildStatRow()
        {
            _statRow = new DataCardGrid { Dock = DockStyle.Top, Height = Scale(90), Gap = 12 };
            _tileDown = AddCard(IconChar.Download, "DOWNLOAD", DataCardAccents.Green);
            _tileUp = AddCard(IconChar.Upload, "UPLOAD", DataCardAccents.Purple);
            _tilePing = AddCard(IconChar.Gauge, "PING", DataCardAccents.Blue);
            _tileJitter = AddCard(IconChar.WaveSquare, "JITTER", DataCardAccents.Amber);
            Controls.Add(_statRow);
        }

        private DataCardControl AddCard(IconChar icon, string caption, Color accent)
        {
            var card = _statRow.AddCard(new DataCardControl(icon, caption, accent));
            card.SetData("—", "not tested yet");
            return card;
        }

        private void BuildProgressStrip()
        {
            _progressStrip = new Panel { Dock = DockStyle.Top, Height = Scale(54), BackColor = Color.Transparent };
            _lblPhase = new Label
            {
                Text = "Press Start Test to measure your connection.",
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(0, Scale(12))
            };
            _progressTrack = new Panel { Height = Scale(6), Location = new Point(0, Scale(36)) };
            _progressFill = new Panel { Height = Scale(6), Width = 0, Dock = DockStyle.Left };
            _progressTrack.Controls.Add(_progressFill);

            _progressStrip.Controls.Add(_lblPhase);
            _progressStrip.Controls.Add(_progressTrack);
            _progressStrip.Resize += (s, e) => _progressTrack.Width = _progressStrip.Width;
            Controls.Add(_progressStrip);
        }

        private void BuildTable()
        {
            _tableCard = ToolUiFactory.Card();
            _tableCard.Padding = new Padding(1);
            _tableCard.Dock = DockStyle.Fill;
            _table = new ResultsTableControl { Dock = DockStyle.Fill };
            _table.SetColumns(("TESTED", 140), ("DOWNLOAD", 110), ("UPLOAD", 110),
                ("PING", 80), ("JITTER", 80), ("SERVER", 0));
            _tableCard.Controls.Add(_table);
            Controls.Add(_tableCard);
        }

        private void BuildFooter()
        {
            // Right-to-left flow right-aligns the shared tool actions.
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = Scale(50),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, Scale(8), 0, 0)
            };
            _copy = ToolUiFactory.GhostButton("Copy Results", IconChar.Copy);
            _clear = ToolUiFactory.GhostButton("Clear History", IconChar.Trash);
            _clear.Width = Scale(130);
            flow.Controls.Add(_copy);
            flow.Controls.Add(_clear);
            Controls.Add(flow);
        }

        private DataCardPalette CurrentPalette()
        {
            var t = themeManager.CurrentTheme;
            return DataCardPalette.From(
                t.CardBackground, t.BorderColor, t.TextColor,
                t.SecondaryTextColor, themeManager.IsDarkTheme);
        }

        // Run control, progress, and history live in SpeedTestControl.Run.cs.

        // ─────────────────────────────────────────────────────────────────────
        // THEME + TEARDOWN
        // ─────────────────────────────────────────────────────────────────────

        private void OnThemeChanged(object sender, EventArgs e) => ApplyTheme();

        private void ApplyTheme()
        {
            var theme = themeManager.CurrentTheme;
            BackColor = theme.ContentBackground;
            _title.ForeColor = theme.HeadingColor;
            _subtitle.ForeColor = theme.SecondaryTextColor;
            ToolUiFactory.ApplyAccentTheme(_start, theme.SuccessColor);
            ToolUiFactory.ApplyGhostTheme(_cancel);
            ToolUiFactory.ApplyGhostTheme(_copy);
            ToolUiFactory.ApplyGhostTheme(_clear);
            ToolUiFactory.ApplyCardTheme(_tableCard);
            _statRow.ApplyPalette(CurrentPalette());
            _progressTrack.BackColor = theme.BorderColor;
            _table.ApplyTheme();
            ApplyPhaseTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                themeManager.ThemeChanged -= OnThemeChanged;
                _cts?.Cancel();
                _cts?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
