// UserControls/Pages/DashboardControl.cs
using NetPulseApp.Managers;
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Dashboard tool page (doc/NetPulse_Toolkit_UI_Images/01_Dashboard_Running.png):
    /// live network overview — four stat tiles (latency, download,
    /// upload, packet loss) fed by a monitor ping and interface counters,
    /// a reused LatencyGraphControl, a connection info card, and a
    /// Running badge with a Pause/Resume control. Layout/theme/teardown
    /// here; monitor wiring in DashboardControl.Run.cs.
    /// </summary>
    public partial class DashboardControl : DpiAwareUserControl
    {
        private Label _title;
        private Label _subtitle;
        private Label _badge;
        private IconButton _btnToggle;
        private Panel _statRow;
        private StatTileControl _tileLatency;
        private StatTileControl _tileDown;
        private StatTileControl _tileUp;
        private StatTileControl _tileLoss;
        private LatencyGraphControl _graph;
        private Panel _connectionCard;
        private readonly List<Label> _connectionCaptions = new();
        private readonly Dictionary<string, Label> _connectionValues = new();

        public DashboardControl()
        {
            BackColor = themeManager.CurrentTheme.ContentBackground;
            Padding = ScaledPadding(16);

            // Docking order per the sibling tool pages: Top panels added
            // bottom-up — display order ends up header, tiles, graph,
            // connection card.
            BuildConnectionCard();
            BuildGraph();
            BuildStatRow();
            BuildHeader();

            WireMonitor();
            ApplyTheme();
            StartMonitor();

            themeManager.ThemeChanged += OnThemeChanged;
        }

        // ─────────────────────────────────────────────────────────────────────
        // LAYOUT
        // ─────────────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(56), BackColor = Color.Transparent };
            _title = ToolUiFactory.PageTitle("Dashboard");
            _title.Location = new Point(0, Scale(2));
            _subtitle = ToolUiFactory.PageSubtitle("Real-time network overview of your connection.");
            _subtitle.Location = new Point(Scale(2), Scale(34));

            // Live badge, right-aligned — same "● Live" convention as the
            // Ping page's graph badge.
            _badge = new Label
            {
                Text = "● Running",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = themeManager.CurrentTheme.SuccessColor,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            _btnToggle = ToolUiFactory.GhostButton("Pause", IconChar.Stop);

            panel.Controls.Add(_subtitle);
            panel.Controls.Add(_title);
            panel.Controls.Add(_badge);
            panel.Controls.Add(_btnToggle);
            panel.Resize += (s, e) =>
            {
                _btnToggle.Location = new Point(panel.Width - _btnToggle.Width - Scale(2), Scale(13));
                _badge.Location = new Point(_btnToggle.Left - _badge.Width - Scale(12), Scale(18));
            };
            Controls.Add(panel);
        }

        private void BuildStatRow()
        {
            _statRow = new Panel { Dock = DockStyle.Top, Height = Scale(84), BackColor = Color.Transparent };

            _tileLatency = new StatTileControl(new StatTileControl.TileConfig(
                "⚡", Color.FromArgb(59, 130, 246), "LATENCY", "— ms", "monitor ping", Color.FromArgb(59, 130, 246)));
            _tileDown = new StatTileControl(new StatTileControl.TileConfig(
                "⬇", Color.FromArgb(34, 197, 94), "DOWNLOAD", "0 Mbps", "current traffic", Color.FromArgb(34, 197, 94)));
            _tileUp = new StatTileControl(new StatTileControl.TileConfig(
                "⬆", Color.FromArgb(99, 102, 241), "UPLOAD", "0 Mbps", "current traffic", Color.FromArgb(99, 102, 241)));
            _tileLoss = new StatTileControl(new StatTileControl.TileConfig(
                "⚠", Color.FromArgb(244, 63, 94), "PACKET LOSS", "—", "monitor ping", Color.FromArgb(244, 63, 94)));

            _statRow.Controls.Add(_tileLatency);
            _statRow.Controls.Add(_tileDown);
            _statRow.Controls.Add(_tileUp);
            _statRow.Controls.Add(_tileLoss);
            // Even 4-up layout, re-flowed whenever the page resizes.
            _statRow.Resize += (s, e) =>
            {
                int gap = Scale(12);
                int tileW = Math.Max(Scale(120), (_statRow.Width - gap * 3) / 4);
                var tiles = new[] { _tileLatency, _tileDown, _tileUp, _tileLoss };
                for (int i = 0; i < tiles.Length; i++)
                    tiles[i].SetBounds(i * (tileW + gap), 0, tileW, _statRow.Height);
            };
            Controls.Add(_statRow);
        }

        private void BuildGraph()
        {
            _graph = new LatencyGraphControl { Dock = DockStyle.Top, Height = Scale(200) };
            Controls.Add(_graph);
        }

        private void BuildConnectionCard()
        {
            _connectionCard = ToolUiFactory.Card();
            _connectionCard.Dock = DockStyle.Top;
            _connectionCard.Height = Scale(196);

            var caption = new Label
            {
                Text = "CONNECTION",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = themeManager.CurrentTheme.SecondaryTextColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(Scale(16), Scale(10))
            };
            _connectionCard.Controls.Add(caption);
            _connectionCaptions.Add(caption);

            int y = Scale(32);
            foreach (var field in new[] { "INTERFACE", "TYPE", "IP ADDRESS", "GATEWAY", "DNS SERVER", "MAC ADDRESS" })
            {
                var cap = new Label
                {
                    Text = field,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = themeManager.CurrentTheme.SecondaryTextColor,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(Scale(16), y + Scale(2))
                };
                var val = new Label
                {
                    Text = "—",
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = themeManager.CurrentTheme.TextColor,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(Scale(150), y)
                };
                _connectionCard.Controls.Add(cap);
                _connectionCard.Controls.Add(val);
                _connectionCaptions.Add(cap);
                _connectionValues[field] = val;
                y += Scale(26);
            }

            Controls.Add(_connectionCard);
        }

        // Monitor wiring, service events, and start/stop live in
        // DashboardControl.Run.cs (partial).

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
            ToolUiFactory.ApplyGhostTheme(_btnToggle);
            ToolUiFactory.ApplyCardTheme(_connectionCard);
            foreach (var cap in _connectionCaptions)
                cap.ForeColor = theme.SecondaryTextColor;
            foreach (var val in _connectionValues.Values)
                val.ForeColor = theme.TextColor;
            // StatTileControl doesn't subscribe to theme changes itself —
            // rebuild the tiles so their card/icon colors follow.
            _tileLatency.Rebuild();
            _tileDown.Rebuild();
            _tileUp.Rebuild();
            _tileLoss.Rebuild();
            _graph.ApplyTheme();
            ApplyBadgeTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                themeManager.ThemeChanged -= OnThemeChanged;
                StopMonitor();
                DisposeServices();
            }
            base.Dispose(disposing);
        }
    }
}
