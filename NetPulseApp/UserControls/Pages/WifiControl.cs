// UserControls/Pages/WifiControl.cs
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;
using NetPulseApp.UserControls.Lib.DataCard;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Wi-Fi Info page (from WiFiPasswordViewer's CurrentConnectionTab +
    /// SignalHistoryChart + WifiStatePanel, Native WiFi API — not netsh):
    /// four DataCards (signal, band/channel, link speed, security), a
    /// connection details card, the 60-second RSSI chart, and a state panel
    /// that replaces the content for Location denied / Wi-Fi off / no
    /// adapter / not connected. Layout/theme here; polling in WifiControl.Run.cs.
    /// </summary>
    public partial class WifiControl : DpiAwareUserControl
    {
        private readonly WifiAnalyzerService _service = new();

        private Label _title;
        private Label _subtitle;
        private IconButton _copy;
        private Panel _body;
        private DataCardGrid _statRow;
        private DataCardControl _cardSignal;
        private DataCardControl _cardBand;
        private DataCardControl _cardSpeed;
        private DataCardControl _cardSecurity;
        private Panel _detailsCard;
        private readonly List<Label> _detailCaptions = new();
        private readonly Dictionary<string, Label> _detailValues = new();
        private Label _lblTip;
        private SignalHistoryChart _chart;
        private WifiStatePanel _statePanel;

        public WifiControl()
        {
            BackColor = themeManager.CurrentTheme.ContentBackground;
            Padding = ScaledPadding(16, 0, 16, 16); // no top inset — header sits flush under the shell header

            // _body holds the live content; the state panel overlays it
            // (same Fill slot) whenever there is nothing to show.
            _body = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _statePanel = new WifiStatePanel { Visible = false };
            Controls.Add(_statePanel);
            Controls.Add(_body);

            BuildChart();
            BuildDetailsCard();
            BuildStatRow();
            BuildHeader();

            WirePolling();
            ApplyTheme();

            themeManager.ThemeChanged += OnThemeChanged;
        }

        // ─────────────────────────────────────────────────────────────────────
        // LAYOUT
        // ─────────────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(64), BackColor = Color.Transparent };
            _title = ToolUiFactory.PageTitle("Wi-Fi");
            _title.Location = new Point(0, Scale(2));
            _subtitle = ToolUiFactory.PageSubtitle("Current Wi-Fi connection, signal strength and link details.");
            _subtitle.Location = new Point(Scale(2), Scale(34));
            _copy = ToolUiFactory.GhostButton("Copy Details", IconChar.Copy);
            _copy.Width = Scale(130);

            panel.Controls.AddRange(new Control[] { _subtitle, _title, _copy });
            panel.Resize += (s, e) =>
                _copy.Location = new Point(panel.Width - _copy.Width - Scale(2), Scale(10));
            Controls.Add(panel);
        }

        private void BuildStatRow()
        {
            _statRow = new DataCardGrid { Dock = DockStyle.Top, Height = Scale(90), Gap = 12 };
            _cardSignal = AddCard(IconChar.Wifi, "SIGNAL", DataCardAccents.Green);
            _cardBand = AddCard(IconChar.SatelliteDish, "BAND / CHANNEL", DataCardAccents.Blue);
            _cardSpeed = AddCard(IconChar.Gauge, "LINK SPEED", DataCardAccents.Purple);
            _cardSecurity = AddCard(IconChar.ShieldHalved, "SECURITY", DataCardAccents.Amber);
            _body.Controls.Add(_statRow);
        }

        private DataCardControl AddCard(IconChar icon, string caption, Color accent)
        {
            var card = _statRow.AddCard(new DataCardControl(icon, caption, accent));
            card.IsLoading = true;
            return card;
        }

        private void BuildDetailsCard()
        {
            _detailsCard = ToolUiFactory.Card();
            _detailsCard.Dock = DockStyle.Top;
            _detailsCard.Height = Scale(196);

            int y = Scale(12);
            foreach (var field in new[] { "NETWORK (SSID)", "BSSID", "STANDARD", "CHANNEL WIDTH", "ADAPTER" })
            {
                var cap = new Label
                {
                    Text = field,
                    Font = new Font("Segoe UI", 8.5f),
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(Scale(16), y + Scale(2))
                };
                var val = new Label
                {
                    Text = "—",
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(Scale(150), y)
                };
                _detailsCard.Controls.Add(cap);
                _detailsCard.Controls.Add(val);
                _detailCaptions.Add(cap);
                _detailValues[field] = val;
                y += Scale(26);
            }

            _lblTip = new Label
            {
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(Scale(16), y + Scale(8))
            };
            _detailsCard.Controls.Add(_lblTip);
            _detailsCard.Resize += (s, e) =>
                _lblTip.MaximumSize = new Size(Math.Max(Scale(200), _detailsCard.Width - Scale(32)), 0);

            // Spacer so the card and chart don't touch.
            _body.Controls.Add(new Panel { Dock = DockStyle.Top, Height = Scale(12), BackColor = Color.Transparent });
            _body.Controls.Add(_detailsCard);
            _body.Controls.Add(new Panel { Dock = DockStyle.Top, Height = Scale(12), BackColor = Color.Transparent });
        }

        private void BuildChart()
        {
            _chart = new SignalHistoryChart { Dock = DockStyle.Fill };
            _body.Controls.Add(_chart);
        }

        private DataCardPalette CurrentPalette()
        {
            var t = themeManager.CurrentTheme;
            return DataCardPalette.From(
                t.CardBackground, t.BorderColor, t.TextColor,
                t.SecondaryTextColor, themeManager.IsDarkTheme);
        }

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
            ToolUiFactory.ApplyGhostTheme(_copy);
            ToolUiFactory.ApplyCardTheme(_detailsCard);
            foreach (var cap in _detailCaptions)
                cap.ForeColor = theme.SecondaryTextColor;
            foreach (var val in _detailValues.Values)
                val.ForeColor = theme.TextColor;
            _lblTip.ForeColor = theme.SecondaryTextColor;
            _statRow.ApplyPalette(CurrentPalette());
            _statePanel.ApplyTheme();
            _chart.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                themeManager.ThemeChanged -= OnThemeChanged;
                StopPolling();
                _service.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
