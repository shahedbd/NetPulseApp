// UserControls/Pages/SubnetCalcControl.cs
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Subnet Calculator tool page: address/prefix input + Calculate, the
    /// FIELD / VALUE results table, and Copy / Clear. Calculation is pure
    /// and instant (SubnetCalculator), so there is no Run partial.
    /// </summary>
    public class SubnetCalcControl : DpiAwareUserControl
    {
        private const string DefaultInput = "192.168.1.10/24";

        private Label _title;
        private Label _subtitle;
        private TextBox _input;
        private IconButton _calculate;
        private Label _lblMessage;
        private Panel _tableCard;
        private ResultsTableControl _table;
        private IconButton _copy;
        private IconButton _clear;

        public SubnetCalcControl()
        {
            BackColor = themeManager.CurrentTheme.ContentBackground;
            Padding = ScaledPadding(16, 0, 16, 16); // no top inset — header sits flush under the shell header

            // Docking order per the sibling tool pages: Fill first, Bottom
            // footer next, then Top panels bottom-up.
            BuildTable();
            BuildFooter();
            BuildInputRow();
            BuildHeader();

            WireEvents();
            ApplyTheme();
            Calculate();

            themeManager.ThemeChanged += OnThemeChanged;
        }

        // ─────────────────────────────────────────────────────────────────────
        // LAYOUT
        // ─────────────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(56), BackColor = Color.Transparent };
            _title = ToolUiFactory.PageTitle("Subnet Calculator");
            _title.Location = new Point(0, Scale(2));
            _subtitle = ToolUiFactory.PageSubtitle("Network, broadcast, host range and masks for any IPv4 or IPv6 prefix.");
            _subtitle.Location = new Point(Scale(2), Scale(40)); // below the 20pt title's descenders
            panel.Controls.Add(_subtitle);
            panel.Controls.Add(_title);
            Controls.Add(panel);
        }

        private void BuildInputRow()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(84), BackColor = Color.Transparent };

            var lblInput = ToolUiFactory.Caption("Address / prefix");
            lblInput.Location = new Point(0, Scale(24));
            _input = ToolUiFactory.Input(260);
            _input.Text = DefaultInput;
            _input.Location = new Point(Scale(104), Scale(20));

            _calculate = ToolUiFactory.AccentButton("Calculate", IconChar.Calculator,
                themeManager.CurrentTheme.AccentColor);
            _calculate.Size = new Size(Scale(110), Scale(34));
            _calculate.Location = new Point(Scale(378), Scale(15));

            _lblMessage = new Label
            {
                Text = "e.g. 10.0.0.5/8, 192.168.1.10 255.255.255.0, or 2001:db8::/48",
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(Scale(104), Scale(56))
            };

            panel.Controls.AddRange(new Control[] { lblInput, _input, _calculate, _lblMessage });
            Controls.Add(panel);
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
            _clear = ToolUiFactory.GhostButton("Clear", IconChar.Trash);
            flow.Controls.Add(_copy);
            flow.Controls.Add(_clear);
            Controls.Add(flow);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ACTIONS
        // ─────────────────────────────────────────────────────────────────────

        private void WireEvents()
        {
            _calculate.Click += (s, e) => Calculate();
            _input.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true; // no ding
                Calculate();
            };
            _copy.Click += (s, e) => ToolUiFactory.CopyResults(_table.GetAsText());
            _clear.Click += (s, e) =>
            {
                _input.Clear();
                _table.ClearRows();
                SetMessage("Enter an address and press Calculate.", isError: false);
                _input.Focus();
            };
        }

        private void Calculate()
        {
            _table.ClearRows();
            if (!SubnetCalculator.TryCalculate(_input.Text, out var fields, out string error))
            {
                SetMessage(error, isError: true);
                return;
            }

            foreach (var f in fields)
                _table.AddRow(new TableRow(new[] { f.Name, f.Value }));
            SetMessage($"{fields.Count} fields calculated.", isError: false);
        }

        private void SetMessage(string text, bool isError)
        {
            var theme = themeManager.CurrentTheme;
            _lblMessage.Text = text;
            _lblMessage.ForeColor = isError ? theme.ErrorColor : theme.SecondaryTextColor;
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
            if (_lblMessage.ForeColor != theme.ErrorColor)
                _lblMessage.ForeColor = theme.SecondaryTextColor;
            ToolUiFactory.ApplyInputTheme(_input);
            ToolUiFactory.ApplyAccentTheme(_calculate, theme.AccentColor);
            ToolUiFactory.ApplyGhostTheme(_copy);
            ToolUiFactory.ApplyGhostTheme(_clear);
            ToolUiFactory.ApplyCardTheme(_tableCard);
            _table.ApplyTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                themeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
