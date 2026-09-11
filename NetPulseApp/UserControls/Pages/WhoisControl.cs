// UserControls/Pages/WhoisControl.cs
using DeviceDataModule;
using NetPulseApp.Managers;
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// WHOIS IP tool page (doc/NetPulse_Toolkit_UI_Images/06_WHOIS_IP.png):
    /// target input + history (IP or domain), Lookup button, summary strip
    /// (object type / expires / status), and the FIELD / VALUE facts table.
    /// Layout/theme/teardown here; run logic in WhoisControl.Run.cs.
    /// </summary>
    public partial class WhoisControl : DpiAwareUserControl
    {
        private readonly WhoisService _service = new();

        private ComboBox _target;
        private IconButton _lookup;
        private IconButton _export;
        private IconButton _copy;
        private IconButton _clear;
        private Panel _summaryCard;
        private Label _lblType;
        private Label _lblExpires;
        private Label _lblStatus;
        private Label _lblMessage;
        private ResultsTableControl _table;
        private Panel _tableCard;
        private Label _title;
        private Label _subtitle;
        private readonly List<Label> _summaryCaptions = new();

        public WhoisControl()
        {
            BackColor = themeManager.CurrentTheme.ContentBackground;
            Padding = ScaledPadding(16);

            // Docking order per the sibling tool pages: the Fill panel
            // goes in first, Bottom footer next, then Top panels bottom-up.
            BuildTable();
            BuildFooter();
            BuildSummary();
            BuildInputRow();
            BuildHeader();

            WireService();
            WireButtons();
            LoadHistory();
            ResetSummary();

            themeManager.ThemeChanged += OnThemeChanged;
        }

        // ─────────────────────────────────────────────────────────────────────
        // LAYOUT
        // ─────────────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(56), BackColor = Color.Transparent };
            _title = ToolUiFactory.PageTitle("WHOIS IP");
            _title.Location = new Point(0, Scale(2));
            _subtitle = ToolUiFactory.PageSubtitle("Lookup registration and network info for any IP address or domain.");
            _subtitle.Location = new Point(Scale(2), Scale(34));
            panel.Controls.Add(_subtitle);
            panel.Controls.Add(_title);
            Controls.Add(panel);
        }

        private void BuildInputRow()
        {
            // One field and one action — RDAP figures out IP vs domain, so
            // there are no options to pick (mock shows the same bare row).
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(58), BackColor = Color.Transparent };

            var lblTarget = ToolUiFactory.Caption("Target");
            lblTarget.Location = new Point(0, Scale(24));
            _target = ToolUiFactory.EditableCombo(320);
            _target.Location = new Point(Scale(46), Scale(19));

            _lookup = ToolUiFactory.AccentButton("Lookup", IconChar.MagnifyingGlass,
                themeManager.CurrentTheme.AccentColor);
            _lookup.Size = new Size(Scale(100), Scale(34));
            _lookup.Location = new Point(Scale(382), Scale(17));

            panel.Controls.AddRange(new Control[] { lblTarget, _target, _lookup });
            Controls.Add(panel);
        }

        private void BuildSummary()
        {
            // Three stat tiles + status pill + message line — the same
            // summary strip pattern as the DNS Lookup page.
            _summaryCard = ToolUiFactory.Card();
            _summaryCard.Dock = DockStyle.Top;
            _summaryCard.Height = Scale(84);

            _lblType = StatTile(_summaryCard, "OBJECT TYPE", "—", 0);
            _lblExpires = StatTile(_summaryCard, "EXPIRES", "—", 1);
            _lblStatus = StatTile(_summaryCard, "STATUS", "● Ready", 2);

            _lblMessage = new Label
            {
                Text = "Enter an IP address or domain and press Lookup.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = themeManager.CurrentTheme.SecondaryTextColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _summaryCard.Controls.Add(_lblMessage);
            _summaryCard.Resize += (s, e) =>
                _lblMessage.Location = new Point(
                    Math.Max(Scale(16), _summaryCard.Width - _lblMessage.Width - Scale(16)), Scale(56));

            Controls.Add(_summaryCard);
        }

        /// <summary>Caption + value pair at tile position 0..2 of the summary card.</summary>
        private Label StatTile(Panel card, string caption, string value, int index)
        {
            var cap = new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = themeManager.CurrentTheme.SecondaryTextColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(Scale(16 + index * 190), Scale(14))
            };
            var val = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = themeManager.CurrentTheme.HeadingColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(Scale(16 + index * 190), Scale(36))
            };
            card.Controls.Add(cap);
            card.Controls.Add(val);
            _summaryCaptions.Add(cap);
            return val;
        }

        private void BuildTable()
        {
            _tableCard = ToolUiFactory.Card();
            _tableCard.Padding = new Padding(1);
            _tableCard.Dock = DockStyle.Fill;
            _table = new ResultsTableControl { Dock = DockStyle.Fill };
            _table.SetColumns(("FIELD", 200), ("VALUE", 0));
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
            _export = ToolUiFactory.GhostButton("Export Report", IconChar.Download);
            _clear = ToolUiFactory.GhostButton("Clear", IconChar.Trash);
            flow.Controls.Add(_copy);
            flow.Controls.Add(_export);
            flow.Controls.Add(_clear);
            Controls.Add(flow);
        }

        // Run control, service events, target history, and report building
        // live in WhoisControl.Run.cs (partial).

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
            ToolUiFactory.ApplyInputTheme(_target);
            ToolUiFactory.ApplyAccentTheme(_lookup, theme.AccentColor);
            ToolUiFactory.ApplyGhostTheme(_copy);
            ToolUiFactory.ApplyGhostTheme(_export);
            ToolUiFactory.ApplyGhostTheme(_clear);
            ToolUiFactory.ApplyCardTheme(_summaryCard);
            ToolUiFactory.ApplyCardTheme(_tableCard);
            _table.ApplyTheme();
            _lblMessage.ForeColor = theme.SecondaryTextColor;
            foreach (var cap in _summaryCaptions)
                cap.ForeColor = theme.SecondaryTextColor;
            _lblType.ForeColor = theme.HeadingColor;
            _lblExpires.ForeColor = theme.HeadingColor;
            ApplyStatusTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                themeManager.ThemeChanged -= OnThemeChanged;
                _service.RecordReceived -= OnRecord;
                _service.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
