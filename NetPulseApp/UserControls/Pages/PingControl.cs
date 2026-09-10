// UserControls/Pages/PingControl.cs
using DeviceDataModule;
using NetPulseApp.Managers;
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Ping tool page (doc/NetPulse_Toolkit_UI_Images/02_Ping.png): target
    /// input + history, interval, Start/Stop, continuous vs fixed-count
    /// modes, live response-time graph, reply log, and run stats.
    /// Layout/theme/teardown here; run logic in PingControl.Run.cs.
    /// </summary>
    public partial class PingControl : DpiAwareUserControl
    {
        private readonly PingService _service = new();

        private ComboBox _target;
        private ModernDropDown _interval;
        private ModernDropDown _count;
        private IconButton _start;
        private IconButton _stop;
        private IconButton _export;
        private IconButton _copy;
        private RadioButton _cont;
        private RadioButton _fixed;
        private LatencyGraphControl _graph;
        private ResultsTableControl _table;
        private Panel _tableCard;
        private Label _title;
        private Label _subtitle;
        private Label _stats;

        private PingStats _lastStats;

        public PingControl()
        {
            BackColor = themeManager.CurrentTheme.ContentBackground;
            Padding = ScaledPadding(16);

            // Docking: the Fill panel goes in first, Bottom-docked panels
            // next (stats before footer, so the footer claims the outer
            // bottom edge), Top-docked last and bottom-up — WinForms claims
            // dock edges from the last-added control first (see MainForm's
            // InitializeMainArea notes), so the header ends up topmost.
            BuildTable();
            BuildStats();
            BuildFooter();
            BuildGraph();
            BuildInputRow();
            BuildHeader();

            WireService();
            WireButtons();
            LoadHistory();
            ResetStatsLabel();

            themeManager.ThemeChanged += OnThemeChanged;
        }

        // ─────────────────────────────────────────────────────────────────────
        // LAYOUT
        // ─────────────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(56), BackColor = Color.Transparent };
            _title = ToolUiFactory.PageTitle("Ping");
            _title.Location = new Point(0, Scale(2));
            _subtitle = ToolUiFactory.PageSubtitle("Round-trip latency test with a live response-time graph.");
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
            _target = ToolUiFactory.EditableCombo(230);
            _target.Location = new Point(Scale(46), Scale(19));

            var lblInterval = ToolUiFactory.Caption("Interval");
            lblInterval.Location = new Point(Scale(292), Scale(24));
            _interval = new ModernDropDown { Width = Scale(85), Location = new Point(Scale(344), Scale(17)) };
            _interval.Items.Add(new ToolUiFactory.LabeledValue("0.5 s", 500));
            _interval.Items.Add(new ToolUiFactory.LabeledValue("1 s", 1000));
            _interval.Items.Add(new ToolUiFactory.LabeledValue("2 s", 2000));
            _interval.Items.Add(new ToolUiFactory.LabeledValue("5 s", 5000));
            _interval.Items.Add(new ToolUiFactory.LabeledValue("10 s", 10000));
            _interval.SelectedIndex = 1;

            _start = ToolUiFactory.AccentButton("Start", IconChar.Play, themeManager.CurrentTheme.SuccessColor);
            _start.Location = new Point(Scale(446), Scale(17));
            _stop = ToolUiFactory.AccentButton("Stop", IconChar.Stop, themeManager.CurrentTheme.ErrorColor);
            _stop.Size = new Size(Scale(88), Scale(34));
            _stop.Location = new Point(Scale(548), Scale(17));
            _stop.Enabled = false;

            _cont = new RadioButton
            {
                Text = "Continuous",
                Font = new Font("Segoe UI", 9f),
                ForeColor = themeManager.CurrentTheme.TextColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Checked = true,
                Location = new Point(Scale(650), Scale(24))
            };
            _fixed = new RadioButton
            {
                Text = "Count:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = themeManager.CurrentTheme.TextColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(Scale(752), Scale(24))
            };
            _count = new ModernDropDown { Width = Scale(66), Location = new Point(Scale(806), Scale(17)) };
            _count.Items.Add(new ToolUiFactory.LabeledValue("4", 4));
            _count.Items.Add(new ToolUiFactory.LabeledValue("10", 10));
            _count.Items.Add(new ToolUiFactory.LabeledValue("25", 25));
            _count.Items.Add(new ToolUiFactory.LabeledValue("50", 50));
            _count.SelectedIndex = 0;
            _count.Enabled = false;
            _fixed.CheckedChanged += (s, e) => _count.Enabled = _fixed.Checked;

            panel.Controls.AddRange(new Control[] { lblTarget, _target, lblInterval, _interval,
                _start, _stop, _cont, _fixed, _count });
            Controls.Add(panel);
        }

        private void BuildGraph()
        {
            _graph = new LatencyGraphControl { Dock = DockStyle.Top, Height = Scale(248) };
            Controls.Add(_graph);
        }

        private void BuildTable()
        {
            _tableCard = ToolUiFactory.Card();
            _tableCard.Padding = new Padding(1);
            _tableCard.Dock = DockStyle.Fill;
            _table = new ResultsTableControl { Dock = DockStyle.Fill };
            _table.SetColumns(("SEQ", 64), ("TIME", 110), ("TTL", 80), ("STATUS", 0));
            _tableCard.Controls.Add(_table);
            Controls.Add(_tableCard);
        }

        private void BuildStats()
        {
            _stats = new Label
            {
                Dock = DockStyle.Bottom,
                Height = Scale(30),
                Font = new Font("Segoe UI", 9f),
                ForeColor = themeManager.CurrentTheme.SecondaryTextColor,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(_stats);
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
            flow.Controls.Add(_copy);
            flow.Controls.Add(_export);
            Controls.Add(flow);
        }

        // Run control, service events, target history, and report building
        // live in PingControl.Run.cs (partial).

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
            _stats.ForeColor = theme.SecondaryTextColor;
            ToolUiFactory.ApplyInputTheme(_target);
            ToolUiFactory.ApplyAccentTheme(_start, theme.SuccessColor);
            ToolUiFactory.ApplyAccentTheme(_stop, theme.ErrorColor);
            ToolUiFactory.ApplyGhostTheme(_copy);
            ToolUiFactory.ApplyGhostTheme(_export);
            ToolUiFactory.ApplyCardTheme(_tableCard);
            _cont.ForeColor = theme.TextColor;
            _fixed.ForeColor = theme.TextColor;
            _interval.ApplyTheme();
            _count.ApplyTheme();
            _table.ApplyTheme();
            _graph.ApplyTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                themeManager.ThemeChanged -= OnThemeChanged;
                _service.ReplyReceived -= OnReply;
                _service.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
