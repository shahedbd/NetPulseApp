// UserControls/Pages/DiagnoseControl.cs
using NetPulseApp.Service.Icons;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Diagnose My Connection page: Diagnose button, a verdict card (plain-
    /// English diagnosis + fix steps + "Open network settings"), and the
    /// CHECK / STATUS / DETAIL / TIME table filled one row per step.
    /// Layout/theme/teardown here; run logic in DiagnoseControl.Run.cs.
    /// </summary>
    public partial class DiagnoseControl : DpiAwareUserControl
    {
        private readonly DiagnoseService _service = new();

        private Label _title;
        private Label _subtitle;
        private IconButton _diagnose;
        private Panel _verdictCard;
        private Label _verdictTitle;
        private Label _verdictText;
        private Label _verdictSteps;
        private LinkLabel _openSettings;
        private Panel _tableCard;
        private ResultsTableControl _table;
        private IconButton _copy;
        private IconButton _export;
        private bool _autoStarted;

        public DiagnoseControl()
        {
            BackColor = themeManager.CurrentTheme.ContentBackground;
            Padding = ScaledPadding(16, 0, 16, 16); // no top inset — header sits flush under the shell header

            // Docking order per the sibling tool pages: Fill first, Bottom
            // footer next, then Top panels bottom-up.
            BuildTable();
            BuildFooter();
            BuildVerdictCard();
            BuildHeader();

            WireService();
            WireButtons();
            ApplyTheme();

            themeManager.ThemeChanged += OnThemeChanged;

            // Opening the page is the one click — start right away.
            HandleCreated += (s, e) =>
            {
                if (_autoStarted) return; // handle can be recreated (DPI/theme)
                _autoStarted = true;
                BeginInvoke(new Action(StartDiagnose));
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // LAYOUT
        // ─────────────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(64), BackColor = Color.Transparent };
            _title = ToolUiFactory.PageTitle("Diagnose My Connection");
            _title.Location = new Point(0, Scale(2));
            _subtitle = ToolUiFactory.PageSubtitle("Finds where your connection breaks — adapter, router, DNS or internet — and how to fix it.");
            _subtitle.Location = new Point(Scale(2), Scale(40)); // below the 20pt title's descenders

            _diagnose = ToolUiFactory.AccentButton("Diagnose", IconChar.MagnifyingGlass,
                themeManager.CurrentTheme.AccentColor);
            _diagnose.Size = new Size(Scale(120), Scale(34));

            panel.Controls.Add(_subtitle);
            panel.Controls.Add(_title);
            panel.Controls.Add(_diagnose);
            panel.Resize += (s, e) =>
                _diagnose.Location = new Point(panel.Width - _diagnose.Width - Scale(2), Scale(10));
            Controls.Add(panel);
        }

        private void BuildVerdictCard()
        {
            _verdictCard = ToolUiFactory.Card();
            _verdictCard.Dock = DockStyle.Top;
            _verdictCard.Height = Scale(150);
            _verdictCard.Padding = ScaledPadding(16);

            _verdictTitle = WrapLabel(new Font("Segoe UI", 13f, FontStyle.Bold));
            _verdictText = WrapLabel(new Font("Segoe UI", 9.5f));
            _verdictSteps = WrapLabel(new Font("Segoe UI", 9f));

            _openSettings = new LinkLabel
            {
                Text = "Open network settings",
                Font = new Font("Segoe UI", 9f),
                AutoSize = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _verdictCard.Controls.AddRange(new Control[] { _verdictTitle, _verdictText, _verdictSteps, _openSettings });
            _verdictCard.Resize += (s, e) => LayoutVerdict();
            Controls.Add(_verdictCard);
        }

        private static Label WrapLabel(Font font) => new()
        {
            Font = font,
            AutoSize = true,
            BackColor = Color.Transparent,
            Text = ""
        };

        /// <summary>Stacks the verdict labels and grows the card to fit the wrapped text.</summary>
        private void LayoutVerdict()
        {
            int x = Scale(16), width = Math.Max(Scale(200), _verdictCard.ClientSize.Width - Scale(32));
            _openSettings.Location = new Point(_verdictCard.ClientSize.Width - _openSettings.Width - Scale(16), Scale(18));

            int y = Scale(14);
            foreach (var label in new[] { _verdictTitle, _verdictText, _verdictSteps })
            {
                int w = label == _verdictTitle ? width - _openSettings.Width - Scale(16) : width;
                label.MaximumSize = new Size(w, 0);
                label.Location = new Point(x, y);
                y += label.Visible && label.Text.Length > 0 ? label.Height + Scale(6) : 0;
            }

            int height = Math.Max(Scale(90), y + Scale(8));
            if (_verdictCard.Height != height) _verdictCard.Height = height;
        }

        private void BuildTable()
        {
            _tableCard = ToolUiFactory.Card();
            _tableCard.Padding = new Padding(1);
            _tableCard.Dock = DockStyle.Fill;
            _table = new ResultsTableControl { Dock = DockStyle.Fill };
            _table.SetColumns(("CHECK", 140), ("STATUS", 100), ("TIME", 80), ("DETAIL", 0));
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
            flow.Controls.Add(_copy);
            flow.Controls.Add(_export);
            Controls.Add(flow);
        }

        // Run control, service events, verdict and report building live in
        // DiagnoseControl.Run.cs (partial).

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
            ToolUiFactory.ApplyAccentTheme(_diagnose, theme.AccentColor);
            ToolUiFactory.ApplyGhostTheme(_copy);
            ToolUiFactory.ApplyGhostTheme(_export);
            ToolUiFactory.ApplyCardTheme(_verdictCard);
            ToolUiFactory.ApplyCardTheme(_tableCard);
            _verdictText.ForeColor = theme.TextColor;
            _verdictSteps.ForeColor = theme.SecondaryTextColor;
            _openSettings.LinkColor = theme.AccentColor;
            _openSettings.ActiveLinkColor = theme.AccentHoverColor;
            _table.ApplyTheme();
            ApplyVerdictTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                themeManager.ThemeChanged -= OnThemeChanged;
                _service.CheckCompleted -= OnCheck;
                _service.RunFinished -= OnFinished;
                _service.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
