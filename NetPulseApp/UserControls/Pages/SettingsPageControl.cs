// UserControls/Pages/SettingsPageControl.cs
using DeviceDataModule;
using NetPulseApp.Forms;
using NetPulseApp.Managers;
using NetPulseApp.Service.Icons;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Settings tool page (doc/NetPulse_Toolkit_UI_Images/07_Settings.png):
    /// the app preferences previously buried in the header's Settings
    /// dialog, as an in-app page in the tool style — section cards built
    /// from SettingsRowControl rows. Only settings that actually exist
    /// (theme, tray behavior, tool history) are shown; the header dialog
    /// keeps working unchanged.
    /// </summary>
    public class SettingsPageControl : DpiAwareUserControl
    {
        private readonly ThemeManager _theme = ThemeManager.Instance;

        private Label _title;
        private Label _subtitle;
        private readonly List<Panel> _cards = new();
        private readonly List<Label> _captions = new();
        private readonly List<SettingsRowControl> _rows = new();
        private ToggleSwitchControl _toggleDark;
        private ToggleSwitchControl _toggleTray;
        private IconButton _btnClearHistory;

        public SettingsPageControl()
        {
            BackColor = _theme.CurrentTheme.ContentBackground;
            Padding = ScaledPadding(16);

            // Top-docked cards are claimed last-added-first, so add them
            // bottom-up (see PingControl's docking notes) — display order
            // ends up Header, Appearance, Behavior, Data.
            BuildDataCard();
            BuildBehaviorCard();
            BuildAppearanceCard();
            BuildHeader();

            _theme.ThemeChanged += OnThemeChanged;
            ApplyTheme();
        }

        // ─────────────────────────────────────────────────────────────────────
        // LAYOUT
        // ─────────────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(56), BackColor = Color.Transparent };
            _title = ToolUiFactory.PageTitle("Settings");
            _title.Location = new Point(0, Scale(2));
            _subtitle = ToolUiFactory.PageSubtitle("Manage app preferences and tool behavior.");
            _subtitle.Location = new Point(Scale(2), Scale(34));
            panel.Controls.Add(_subtitle);
            panel.Controls.Add(_title);
            Controls.Add(panel);
        }

        private void BuildAppearanceCard()
        {
            _toggleDark = new ToggleSwitchControl { Checked = _theme.IsDarkTheme };
            _toggleDark.CheckedChanged += (s, e) => _theme.SetTheme(_toggleDark.Checked);

            var row = new SettingsRowControl("Dark Mode", _toggleDark,
                "Use the dark theme across the app.");
            AddCard("Appearance", row);
        }

        private void BuildBehaviorCard()
        {
            _toggleTray = new ToggleSwitchControl
            {
                Checked = SettingsService.Current.MinimizeToTrayOnClose
            };
            _toggleTray.CheckedChanged += (s, e) =>
            {
                SettingsService.Current.MinimizeToTrayOnClose = _toggleTray.Checked;
                SettingsService.Save();
            };

            var row = new SettingsRowControl("Minimize to tray when closing", _toggleTray,
                "Keep running in the system tray after the window closes.");
            AddCard("Behavior", row);
            if (!Helper.AppConfig.EnableSystemTray)
                row.SetEnabled(false);
        }

        private void BuildDataCard()
        {
            _btnClearHistory = ToolUiFactory.GhostButton("Clear History", IconChar.Trash);
            _btnClearHistory.Click += (s, e) => ClearToolHistory();

            var row = new SettingsRowControl("Tool history", _btnClearHistory,
                "Targets remembered by Ping, Traceroute, DNS Lookup, Port Checker and WHOIS IP.");
            AddCard("Data", row);
        }

        /// <summary>One section card: heading caption + rows, Top-docked.</summary>
        private void AddCard(string captionText, params SettingsRowControl[] rows)
        {
            var card = ToolUiFactory.Card();
            card.Dock = DockStyle.Top;

            int y = Scale(12);
            var caption = new Label
            {
                Text = captionText.ToUpperInvariant(),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(Scale(16), y)
            };
            card.Controls.Add(caption);
            _captions.Add(caption);
            y += Scale(24);

            foreach (var row in rows)
            {
                row.Location = new Point(Scale(16), y);
                card.Controls.Add(row);
                _rows.Add(row);
                y += row.Height + row.Margin.Bottom;
            }

            card.Height = y + Scale(4);

            // Rows hug the card's right edge; re-flow when the page resizes.
            card.Resize += (s, e) =>
            {
                int contentWidth = card.Width - Scale(32);
                foreach (Control c in card.Controls)
                    if (c is SettingsRowControl r)
                        r.SetWidth(contentWidth);
            };

            _cards.Add(card);
            Controls.Add(card);
        }

        private void ClearToolHistory()
        {
            bool confirmed = AlertNotificationService.Confirm(
                "This removes every remembered target from Ping, Traceroute, DNS Lookup, Port Checker and WHOIS IP. This cannot be undone.",
                "Clear tool history?",
                AlertMessageType.Warning,
                "Clear",
                "Cancel");
            if (!confirmed)
                return;

            var settings = SettingsService.Current;
            settings.PingTargets.Clear();
            settings.TraceTargets.Clear();
            settings.DnsTargets.Clear();
            settings.PortCheckerHosts.Clear();
            settings.WhoisTargets.Clear();
            SettingsService.Save();
            ToastsNotificationManager.Instance.ShowSuccessNotification("Tool history cleared.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // THEME + TEARDOWN
        // ─────────────────────────────────────────────────────────────────────

        private void OnThemeChanged(object sender, EventArgs e)
        {
            // Theme can be toggled from the header while this page is open —
            // pull the switch back in sync (SetTheme no-ops on same values).
            _toggleDark.Checked = _theme.IsDarkTheme;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var theme = _theme.CurrentTheme;
            BackColor = theme.ContentBackground;
            _title.ForeColor = theme.HeadingColor;
            _subtitle.ForeColor = theme.SecondaryTextColor;
            foreach (var card in _cards)
                ToolUiFactory.ApplyCardTheme(card);
            foreach (var caption in _captions)
                caption.ForeColor = theme.SecondaryTextColor;
            foreach (var row in _rows)
                row.ApplyTheme(theme);
            ToolUiFactory.ApplyGhostTheme(_btnClearHistory);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _theme.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
