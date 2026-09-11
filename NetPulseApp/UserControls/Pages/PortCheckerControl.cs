// UserControls/Pages/PortCheckerControl.cs
using DeviceDataModule;
using NetPulseApp.Managers;
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Port Checker tool page (doc/NetPulse_Toolkit_UI_Images/05_Port_Checker.png):
    /// host input + history, comma-separated port list, preset bundles,
    /// timeout, Check button, summary card (Open / Closed / Filtered /
    /// Status), and the per-port results table. Layout/theme/teardown
    /// here; run logic in PortCheckerControl.Run.cs.
    /// </summary>
    public partial class PortCheckerControl : DpiAwareUserControl
    {
        private readonly PortCheckService _service = new();

        private ComboBox _host;
        private TextBox _ports;
        private ModernDropDown _preset;
        private ModernDropDown _timeout;
        private IconButton _check;
        private IconButton _stop;
        private IconButton _export;
        private IconButton _copy;
        private IconButton _clear;
        private Panel _summaryCard;
        private Label _lblOpen;
        private Label _lblClosed;
        private Label _lblFiltered;
        private Label _lblStatus;
        private Label _lblMessage;
        private ResultsTableControl _table;
        private Panel _tableCard;
        private Label _title;
        private Label _subtitle;
        private readonly List<Label> _summaryCaptions = new();

        public PortCheckerControl()
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
            _title = ToolUiFactory.PageTitle("Port Checker");
            _title.Location = new Point(0, Scale(2));
            _subtitle = ToolUiFactory.PageSubtitle("Check whether specific ports are open on a host.");
            _subtitle.Location = new Point(Scale(2), Scale(34));
            panel.Controls.Add(_subtitle);
            panel.Controls.Add(_title);
            Controls.Add(panel);
        }

        private void BuildInputRow()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(58), BackColor = Color.Transparent };

            var lblHost = ToolUiFactory.Caption("Host");
            lblHost.Location = new Point(0, Scale(24));
            _host = ToolUiFactory.EditableCombo(190);
            _host.Location = new Point(Scale(36), Scale(19));

            var lblPorts = ToolUiFactory.Caption("Port(s)");
            lblPorts.Location = new Point(Scale(240), Scale(24));
            _ports = ToolUiFactory.Input(120);
            _ports.Location = new Point(Scale(286), Scale(19));
            _ports.Text = "80, 443";

            var lblPreset = ToolUiFactory.Caption("Presets");
            lblPreset.Location = new Point(Scale(420), Scale(24));
            _preset = new ModernDropDown { Width = Scale(150), Location = new Point(Scale(466), Scale(17)) };
            _preset.Items.Add(new ToolUiFactory.LabeledValue("Custom", -1));
            _preset.Items.Add(new ToolUiFactory.LabeledValue("Web (80, 443)", 0));
            _preset.Items.Add(new ToolUiFactory.LabeledValue("Email (25, 110, 143, 993, 995)", 1));
            _preset.Items.Add(new ToolUiFactory.LabeledValue("Databases (1433, 3306, 5432)", 2));
            _preset.Items.Add(new ToolUiFactory.LabeledValue("Remote access (22, 3389)", 3));
            _preset.SelectedIndex = 0;

            var lblTimeout = ToolUiFactory.Caption("Timeout");
            lblTimeout.Location = new Point(Scale(630), Scale(24));
            _timeout = new ModernDropDown { Width = Scale(60), Location = new Point(Scale(686), Scale(17)) };
            _timeout.Items.Add(new ToolUiFactory.LabeledValue("1 s", 1000));
            _timeout.Items.Add(new ToolUiFactory.LabeledValue("2 s", 2000));
            _timeout.Items.Add(new ToolUiFactory.LabeledValue("3 s", 3000));
            _timeout.Items.Add(new ToolUiFactory.LabeledValue("5 s", 5000));
            _timeout.SelectedIndex = 1;

            _check = ToolUiFactory.AccentButton("Check", IconChar.Plug, themeManager.CurrentTheme.AccentColor);
            _check.Size = new Size(Scale(92), Scale(34));
            _check.Location = new Point(Scale(758), Scale(17));
            _stop = ToolUiFactory.AccentButton("Stop", IconChar.Stop, themeManager.CurrentTheme.ErrorColor);
            _stop.Size = new Size(Scale(82), Scale(34));
            _stop.Location = new Point(Scale(858), Scale(17));
            _stop.Enabled = false;

            panel.Controls.AddRange(new Control[] { lblHost, _host, lblPorts, _ports,
                lblPreset, _preset, lblTimeout, _timeout, _check, _stop });
            Controls.Add(panel);
        }

        private void BuildSummary()
        {
            // Four stat tiles + status pill + message line — same summary
            // card pattern as TracerouteControl.
            _summaryCard = ToolUiFactory.Card();
            _summaryCard.Dock = DockStyle.Top;
            _summaryCard.Height = Scale(84);

            _lblOpen = StatTile(_summaryCard, "OPEN", "0", 0);
            _lblClosed = StatTile(_summaryCard, "CLOSED", "0", 1);
            _lblFiltered = StatTile(_summaryCard, "FILTERED", "0", 2);
            _lblStatus = StatTile(_summaryCard, "STATUS", "● Ready", 3);

            _lblMessage = new Label
            {
                Text = "Enter a host and ports, then press Check.",
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
            _table.SetColumns(("PORT", 70), ("PROTOCOL", 100), ("STATUS", 100),
                ("SERVICE", 150), ("RESPONSE TIME", 0));
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

        // Run control, service events, host history, and report building
        // live in PortCheckerControl.Run.cs (partial).

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
            ToolUiFactory.ApplyInputTheme(_host);
            ToolUiFactory.ApplyInputTheme(_ports);
            ToolUiFactory.ApplyAccentTheme(_check, theme.AccentColor);
            ToolUiFactory.ApplyAccentTheme(_stop, theme.ErrorColor);
            ToolUiFactory.ApplyGhostTheme(_copy);
            ToolUiFactory.ApplyGhostTheme(_export);
            ToolUiFactory.ApplyGhostTheme(_clear);
            ToolUiFactory.ApplyCardTheme(_summaryCard);
            ToolUiFactory.ApplyCardTheme(_tableCard);
            _preset.ApplyTheme();
            _timeout.ApplyTheme();
            _table.ApplyTheme();
            _lblMessage.ForeColor = theme.SecondaryTextColor;
            foreach (var cap in _summaryCaptions)
                cap.ForeColor = theme.SecondaryTextColor;
            _lblOpen.ForeColor = theme.SuccessColor;
            _lblClosed.ForeColor = theme.ErrorColor;
            _lblFiltered.ForeColor = theme.WarningColor;
            ApplyStatusTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                themeManager.ThemeChanged -= OnThemeChanged;
                _service.PortChecked -= OnPortChecked;
                _service.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
