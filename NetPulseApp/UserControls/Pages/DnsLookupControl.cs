// UserControls/Pages/DnsLookupControl.cs
using DeviceDataModule;
using NetPulseApp.Managers;
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// DNS Lookup tool page (doc/NetPulse_Toolkit_UI_Images/04_DNS_Lookup.png):
    /// domain input + history, record-type and DNS-server pickers, Lookup
    /// button, summary strip (records found / query time / status), and the
    /// TYPE / VALUE / TTL / EXTRA results table. Layout/theme/teardown
    /// here; run logic in DnsLookupControl.Run.cs.
    /// </summary>
    public partial class DnsLookupControl : DpiAwareUserControl
    {
        private readonly DnsLookupService _service = new();

        private ComboBox _domain;
        private ModernDropDown _recordType;
        private ModernDropDown _dnsServer;
        private IconButton _lookup;
        private IconButton _export;
        private IconButton _copy;
        private IconButton _clear;
        private Panel _summaryCard;
        private Label _lblRecords;
        private Label _lblQueryTime;
        private Label _lblStatus;
        private Label _lblMessage;
        private ResultsTableControl _table;
        private Panel _tableCard;
        private Label _title;
        private Label _subtitle;
        private readonly List<Label> _summaryCaptions = new();

        public DnsLookupControl()
        {
            BackColor = themeManager.CurrentTheme.ContentBackground;
            Padding = ScaledPadding(16);

            // Docking order per PingControl/TracerouteControl: the Fill
            // panel goes in first, Bottom footer next, then Top panels
            // bottom-up — WinForms claims dock edges last-added first.
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
            _title = ToolUiFactory.PageTitle("DNS Lookup");
            _title.Location = new Point(0, Scale(2));
            _subtitle = ToolUiFactory.PageSubtitle("Resolve domain names to IP addresses and other DNS records.");
            _subtitle.Location = new Point(Scale(2), Scale(34));
            panel.Controls.Add(_subtitle);
            panel.Controls.Add(_title);
            Controls.Add(panel);
        }

        private void BuildInputRow()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(58), BackColor = Color.Transparent };

            var lblDomain = ToolUiFactory.Caption("Domain");
            lblDomain.Location = new Point(0, Scale(24));
            _domain = ToolUiFactory.EditableCombo(230);
            _domain.Location = new Point(Scale(50), Scale(19));

            var lblType = ToolUiFactory.Caption("Record type");
            lblType.Location = new Point(Scale(296), Scale(24));
            _recordType = new ModernDropDown { Width = Scale(84), Location = new Point(Scale(360), Scale(17)) };
            _recordType.Items.Add(new ToolUiFactory.LabeledValue("All", 0));
            _recordType.Items.Add(new ToolUiFactory.LabeledValue("A", 1));
            _recordType.Items.Add(new ToolUiFactory.LabeledValue("AAAA", 28));
            _recordType.Items.Add(new ToolUiFactory.LabeledValue("CNAME", 5));
            _recordType.Items.Add(new ToolUiFactory.LabeledValue("MX", 15));
            _recordType.Items.Add(new ToolUiFactory.LabeledValue("TXT", 16));
            _recordType.Items.Add(new ToolUiFactory.LabeledValue("NS", 2));
            _recordType.Items.Add(new ToolUiFactory.LabeledValue("SOA", 6));
            _recordType.SelectedIndex = 1; // A — the everyday lookup

            var lblServer = ToolUiFactory.Caption("DNS server");
            lblServer.Location = new Point(Scale(458), Scale(24));
            _dnsServer = new ModernDropDown { Width = Scale(140), Location = new Point(Scale(526), Scale(17)) };
            _dnsServer.Items.Add("Default");
            _dnsServer.Items.Add("8.8.8.8 (Google)");
            _dnsServer.Items.Add("1.1.1.1 (Cloudflare)");
            _dnsServer.Items.Add("9.9.9.9 (Quad9)");
            _dnsServer.SelectedIndex = 0;

            _lookup = ToolUiFactory.AccentButton("Lookup", IconChar.MagnifyingGlass,
                themeManager.CurrentTheme.AccentColor);
            _lookup.Size = new Size(Scale(100), Scale(34));
            _lookup.Location = new Point(Scale(680), Scale(17));

            panel.Controls.AddRange(new Control[] { lblDomain, _domain, lblType, _recordType,
                lblServer, _dnsServer, _lookup });
            Controls.Add(panel);
        }

        private void BuildSummary()
        {
            // Three stat tiles + status pill + message line — the mock's
            // "records found / query time / status" strip. Same fixed-tile
            // pattern as TracerouteControl's summary card.
            _summaryCard = ToolUiFactory.Card();
            _summaryCard.Dock = DockStyle.Top;
            _summaryCard.Height = Scale(84);

            _lblRecords = StatTile(_summaryCard, "RECORDS FOUND", "0", 0);
            _lblQueryTime = StatTile(_summaryCard, "QUERY TIME", "— ms", 1);
            _lblStatus = StatTile(_summaryCard, "STATUS", "● Ready", 2);

            _lblMessage = new Label
            {
                Text = "Enter a domain and press Lookup to resolve its records.",
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
            _table.SetColumns(("TYPE", 90), ("VALUE", 230), ("TTL", 70), ("EXTRA", 0));
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

        // Run control, service events, domain history, and report building
        // live in DnsLookupControl.Run.cs (partial).

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
            ToolUiFactory.ApplyInputTheme(_domain);
            ToolUiFactory.ApplyAccentTheme(_lookup, theme.AccentColor);
            ToolUiFactory.ApplyGhostTheme(_copy);
            ToolUiFactory.ApplyGhostTheme(_export);
            ToolUiFactory.ApplyGhostTheme(_clear);
            ToolUiFactory.ApplyCardTheme(_summaryCard);
            ToolUiFactory.ApplyCardTheme(_tableCard);
            _recordType.ApplyTheme();
            _dnsServer.ApplyTheme();
            _table.ApplyTheme();
            _lblMessage.ForeColor = theme.SecondaryTextColor;
            foreach (var cap in _summaryCaptions)
                cap.ForeColor = theme.SecondaryTextColor;
            _lblRecords.ForeColor = theme.HeadingColor;
            _lblQueryTime.ForeColor = theme.HeadingColor;
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
