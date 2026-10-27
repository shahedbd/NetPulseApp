// UserControls/Pages/DashboardControl.cs
using NetPulseApp.Managers;
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;
using NetPulseApp.UserControls.Lib.DataCard;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Dashboard tool page (doc/NetPulse_Toolkit_UI_Images/01_Dashboard_Running.png):
    /// live network overview — five data cards (latency + grade, jitter,
    /// download, upload, packet loss) fed by a monitor ping and interface
    /// counters, a reused LatencyGraphControl, a connection info card with
    /// public IP / ISP / location, and a Running badge with a Pause/Resume
    /// control. Layout/theme/teardown here; monitor wiring in
    /// DashboardControl.Run.cs; public IP in DashboardControl.PublicIp.cs.
    /// </summary>
    public partial class DashboardControl : DpiAwareUserControl
    {
        private Label _title;
        private Label _subtitle;
        private Label _badge;
        private IconButton _btnToggle;
        private IconButton _btnDiagnose;
        private DataCardGrid _statRow;
        private DataCardControl _tileLatency;
        private DataCardControl _tileJitter;
        private DataCardControl _tileDown;
        private DataCardControl _tileUp;
        private DataCardControl _tileLoss;
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
            StartPublicIp();

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
            _btnDiagnose = ToolUiFactory.AccentButton("Diagnose", IconChar.MagnifyingGlass,
                themeManager.CurrentTheme.AccentColor);
            _btnDiagnose.Size = new Size(Scale(120), Scale(34));
            // Deferred: the switch disposes this page mid-click otherwise.
            _btnDiagnose.Click += (s, e) => BeginInvoke(new Action(() =>
                NavigationManager.Current?.NavigateTo(typeof(DiagnoseControl))));

            panel.Controls.Add(_subtitle);
            panel.Controls.Add(_title);
            panel.Controls.Add(_badge);
            panel.Controls.Add(_btnToggle);
            panel.Controls.Add(_btnDiagnose);
            panel.Resize += (s, e) =>
            {
                _btnToggle.Location = new Point(panel.Width - _btnToggle.Width - Scale(2), Scale(13));
                _btnDiagnose.Location = new Point(_btnToggle.Left - _btnDiagnose.Width - Scale(8), Scale(13));
                _badge.Location = new Point(_btnDiagnose.Left - _badge.Width - Scale(12), Scale(18));
            };
            Controls.Add(panel);
        }

        private void BuildStatRow()
        {
            // Equal-width 5-up row — DataCardGrid re-flows on resize itself.
            _statRow = new DataCardGrid { Dock = DockStyle.Top, Height = Scale(90), Gap = 12 };

            _tileLatency = AddCard(IconChar.Gauge, "LATENCY", DataCardAccents.Blue, "monitor ping");
            _tileJitter = AddCard(IconChar.WaveSquare, "JITTER", DataCardAccents.Amber, "monitor ping");
            _tileDown = AddCard(IconChar.Download, "CURRENT DOWNLOAD", DataCardAccents.Green, "current traffic");
            _tileUp = AddCard(IconChar.Upload, "CURRENT UPLOAD", DataCardAccents.Purple, "current traffic");
            _tileLoss = AddCard(IconChar.TriangleExclamation, "PACKET LOSS", DataCardAccents.Red, "monitor ping");

            Controls.Add(_statRow);
        }

        /// <summary>Card shows a loading skeleton until its first live value arrives.</summary>
        private DataCardControl AddCard(IconChar icon, string caption, Color accent, string subText)
        {
            var card = _statRow.AddCard(new DataCardControl(icon, caption, accent));
            card.SubText = subText;
            card.IsLoading = true;
            return card;
        }

        private DataCardPalette CurrentPalette()
        {
            var t = themeManager.CurrentTheme;
            return DataCardPalette.From(
                t.CardBackground, t.BorderColor, t.TextColor,
                t.SecondaryTextColor, themeManager.IsDarkTheme);
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
            _connectionCard.Height = Scale(274);

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
            foreach (var field in new[] { "INTERFACE", "TYPE", "IP ADDRESS", "GATEWAY", "DNS SERVER", "MAC ADDRESS",
                                          "PUBLIC IP", "ISP", "LOCATION" })
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

            BuildPublicIpActions();
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
            ToolUiFactory.ApplyAccentTheme(_btnDiagnose, theme.AccentColor);
            ToolUiFactory.ApplyCardTheme(_connectionCard);
            ApplyPublicIpTheme();
            foreach (var cap in _connectionCaptions)
                cap.ForeColor = theme.SecondaryTextColor;
            foreach (var val in _connectionValues.Values)
                val.ForeColor = theme.TextColor;
            // Data cards are theme-agnostic — map the theme onto their palette.
            _statRow.ApplyPalette(CurrentPalette());
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
                DisposePublicIp();
            }
            base.Dispose(disposing);
        }
    }
}
