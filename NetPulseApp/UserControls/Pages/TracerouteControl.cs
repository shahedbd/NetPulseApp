// UserControls/Pages/TracerouteControl.cs
using DeviceDataModule;
using NetPulseApp.Managers;
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Traceroute tool page (doc/NetPulse_Toolkit_UI_Images/03_Traceroute.png):
    /// target input + history, max-hops / timeout / protocol options,
    /// Start/Stop, live summary card (hops, destination, average, status),
    /// and the hop-by-hop results table. Layout/theme/teardown here; run
    /// logic in TracerouteControl.Run.cs.
    /// </summary>
    public partial class TracerouteControl : DpiAwareUserControl
    {
        private readonly TraceRouteService _service = new();

        private ComboBox _target;
        private ModernDropDown _maxHops;
        private ModernDropDown _timeout;
        private ModernDropDown _protocol;
        private IconButton _start;
        private IconButton _stop;
        private IconButton _export;
        private IconButton _copy;
        private IconButton _clear;
        private Panel _summaryCard;
        private Label _lblHops;
        private Label _lblDestination;
        private Label _lblAvgTime;
        private Label _lblStatus;
        private Label _lblMessage;
        private ResultsTableControl _table;
        private Panel _tableCard;
        private Label _title;
        private Label _subtitle;
        private readonly List<Label> _summaryCaptions = new();

        public TracerouteControl()
        {
            BackColor = themeManager.CurrentTheme.ContentBackground;
            Padding = ScaledPadding(16);

            // Docking order per PingControl: the Fill panel goes in first,
            // Bottom-docked footer next, then Top-docked cards bottom-up —
            // WinForms claims dock edges from the last-added control first.
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
            _title = ToolUiFactory.PageTitle("Traceroute");
            _title.Location = new Point(0, Scale(2));
            _subtitle = ToolUiFactory.PageSubtitle("See the path your packets take to a destination.");
            _subtitle.Location = new Point(Scale(2), Scale(34));
            panel.Controls.Add(_subtitle);
            panel.Controls.Add(_title);
            Controls.Add(panel);
        }

        private void BuildInputRow()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(58), BackColor = Color.Transparent };

            var lblTarget = ToolUiFactory.Caption("Target");
            lblTarget.Location = new Point(0, Scale(24));
            _target = ToolUiFactory.EditableCombo(210);
            _target.Location = new Point(Scale(46), Scale(19));

            var lblHops = ToolUiFactory.Caption("Max hops");
            lblHops.Location = new Point(Scale(272), Scale(24));
            _maxHops = new ModernDropDown { Width = Scale(60), Location = new Point(Scale(328), Scale(17)) };
            _maxHops.Items.Add(new ToolUiFactory.LabeledValue("10", 10));
            _maxHops.Items.Add(new ToolUiFactory.LabeledValue("20", 20));
            _maxHops.Items.Add(new ToolUiFactory.LabeledValue("30", 30));
            _maxHops.Items.Add(new ToolUiFactory.LabeledValue("50", 50));
            _maxHops.SelectedIndex = 2;

            var lblTimeout = ToolUiFactory.Caption("Timeout");
            lblTimeout.Location = new Point(Scale(402), Scale(24));
            _timeout = new ModernDropDown { Width = Scale(72), Location = new Point(Scale(458), Scale(17)) };
            _timeout.Items.Add(new ToolUiFactory.LabeledValue("1 s", 1000));
            _timeout.Items.Add(new ToolUiFactory.LabeledValue("2 s", 2000));
            _timeout.Items.Add(new ToolUiFactory.LabeledValue("3 s", 3000));
            _timeout.Items.Add(new ToolUiFactory.LabeledValue("5 s", 5000));
            _timeout.SelectedIndex = 2;

            var lblProtocol = ToolUiFactory.Caption("Protocol");
            lblProtocol.Location = new Point(Scale(546), Scale(24));
            _protocol = new ModernDropDown { Width = Scale(104), Location = new Point(Scale(600), Scale(17)) };
            _protocol.Items.Add(new ToolUiFactory.LabeledValue("ICMP (default)", 0));
            _protocol.SelectedIndex = 0;

            _start = ToolUiFactory.AccentButton("Start", IconChar.Play, themeManager.CurrentTheme.AccentColor);
            _start.Size = new Size(Scale(116), Scale(34));
            _start.Location = new Point(Scale(718), Scale(17));
            _stop = ToolUiFactory.AccentButton("Stop", IconChar.Stop, themeManager.CurrentTheme.ErrorColor);
            _stop.Size = new Size(Scale(84), Scale(34));
            _stop.Location = new Point(Scale(840), Scale(17));
            _stop.Enabled = false;

            panel.Controls.AddRange(new Control[] { lblTarget, _target, lblHops, _maxHops,
                lblTimeout, _timeout, lblProtocol, _protocol, _start, _stop });
            Controls.Add(panel);
        }

        private void BuildSummary()
        {
            // Four stat tiles + status pill + message line, per the mock's
            // "Summary" card. Fixed positions on one panel — like the input
            // row, this never re-flows.
            _summaryCard = ToolUiFactory.Card();
            _summaryCard.Dock = DockStyle.Top;
            _summaryCard.Height = Scale(84);

            _lblHops = StatTile(_summaryCard, "TOTAL HOPS", "0", 0);
            _lblDestination = StatTile(_summaryCard, "DESTINATION", "—", 1);
            _lblAvgTime = StatTile(_summaryCard, "AVG RESPONSE TIME", "— ms", 2);
            _lblStatus = StatTile(_summaryCard, "STATUS", "● Ready", 3);

            _lblMessage = new Label
            {
                Text = "Enter a target and press Start to trace the route.",
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

        /// <summary>Caption + value pair at tile position 0..3 of the summary card.</summary>
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
            _table.SetColumns(("#", 48), ("IP ADDRESS", 130), ("HOSTNAME", 180),
                ("RESPONSE TIME", 120), ("LOCATION / ISP", 0));
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
        // live in TracerouteControl.Run.cs (partial).

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
            ToolUiFactory.ApplyAccentTheme(_start, theme.AccentColor);
            ToolUiFactory.ApplyAccentTheme(_stop, theme.ErrorColor);
            ToolUiFactory.ApplyGhostTheme(_copy);
            ToolUiFactory.ApplyGhostTheme(_export);
            ToolUiFactory.ApplyGhostTheme(_clear);
            ToolUiFactory.ApplyCardTheme(_summaryCard);
            ToolUiFactory.ApplyCardTheme(_tableCard);
            _maxHops.ApplyTheme();
            _timeout.ApplyTheme();
            _protocol.ApplyTheme();
            _table.ApplyTheme();
            _lblMessage.ForeColor = theme.SecondaryTextColor;
            foreach (var cap in _summaryCaptions)
                cap.ForeColor = theme.SecondaryTextColor;
            _lblHops.ForeColor = theme.HeadingColor;
            _lblDestination.ForeColor = theme.HeadingColor;
            _lblAvgTime.ForeColor = theme.HeadingColor;
            ApplyStatusTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                themeManager.ThemeChanged -= OnThemeChanged;
                _service.HopReceived -= OnHop;
                _service.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
