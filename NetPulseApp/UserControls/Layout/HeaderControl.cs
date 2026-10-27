// UserControls/Layout/HeaderControl.cs
using NetPulseApp.Forms;
using NetPulseApp.Helper;
using NetPulseApp.Managers;
using NetPulseApp.Service.Dpi;
using NetPulseApp.Service.Icons;
using NetPulseApp.UiComponent;

namespace NetPulseApp.UserControls.Layout
{
    /// <summary>
    /// Part 02 — header bar: system summary on the left, action buttons on
    /// the right (About · Settings · Theme). Only spans the region to the right of the
    /// sidebar (see Doc/UI layout.md) — MainForm nests this inside the
    /// content-side panel, not directly under the title bar.
    ///
    /// Layout: a docked, self-sizing button bar on the right and the summary
    /// strip docked to fill the rest, so a long CPU name is clipped by the
    /// strip instead of running under the buttons — no resize-time width math.
    ///
    /// Button styling lives in HeaderControl.ActionStyle.cs, the summary strip
    /// in HeaderControl.Summary.cs / .SystemSummary.cs, theme and DPI code in
    /// HeaderControl.Theme.cs.
    /// </summary>
    public partial class HeaderControl : UserControl
    {
        // ── Child controls ────────────────────────────────────────────────────
        private Panel _buttonBar;
        private Panel _leftAccent;
        private Panel _bottomBorder;
        private Button _btnSettings;
        private Button _btnTheme;
        private Button _btnAbout;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly ThemeManager _themeManager;
        private readonly ToolTip _toolTip;

        private int HeaderHeight => DpiAwareService.Scale(AppConfig.HeaderHeight);

        // ─────────────────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────────────────

        public HeaderControl()
        {
            _themeManager = ThemeManager.Instance;
            _toolTip = new ToolTip { AutoPopDelay = 3000, InitialDelay = 400, ReshowDelay = 200 };

            Dock = DockStyle.Top;
            Height = HeaderHeight;

            Build();
            ApplyTheme();
        }

        // ─────────────────────────────────────────────────────────────────────
        // BUILD
        // ─────────────────────────────────────────────────────────────────────

        private void Build()
        {
            // Fixed-height right-side panel, seeded from the header height so
            // button positioning is deterministic at construction time instead
            // of waiting for the parent's first layout pass.
            _buttonBar = new Panel
            {
                Dock = DockStyle.Right,
                Height = HeaderHeight
            };

            BuildActionButtons();

            // ── System summary (left side) ────────────────────────────────────
            _summaryStrip = BuildSummaryStrip();
            SetSummaryText("Loading system information...");

            _leftAccent = new Panel { Dock = DockStyle.Left, Width = DpiAwareService.Scale(3) };
            _bottomBorder = new Panel { Dock = DockStyle.Bottom, Height = DpiAwareService.Scale(1) };

            // Add order (first → last): Fill strip first (innermost), then the
            // left accent, the right button bar and the bottom border — each
            // later addition claims its edge from the full area first. The
            // strip and button bar never overlap, so neither hides the other.
            Controls.Add(_summaryStrip);
            Controls.Add(_leftAccent);
            Controls.Add(_buttonBar);
            Controls.Add(_bottomBorder);
        }

        private void BuildActionButtons()
        {
            _btnAbout = CreateActionButton(IconChar.InfoCircle, $"About {AppConfig.AppName}", BtnAbout_Click);

            _btnSettings = CreateActionButton(IconChar.Gear, "Settings", BtnSettings_Click);

            // Icon, colour and tooltip set by UpdateThemeButtonIcon.
            _btnTheme = CreateActionButton(IconChar.None, string.Empty, BtnTheme_Click);

            _buttonBar.Controls.AddRange(ActionButtons);

            PositionButtons();
        }

        /// <summary>
        /// Sizes the button bar to the button group and positions the buttons
        /// right-to-left (Theme → Settings → About), vertically centred.
        /// </summary>
        private void PositionButtons()
        {
            if (_buttonBar == null) return;

            var buttons = Array.FindAll(ActionButtons, b => b != null && b.Visible);
            int rightMargin = DpiAwareService.Scale(12);

            _buttonBar.Width = HeaderButtonService.GroupWidth(buttons, rightMargin) + DpiAwareService.Scale(8);

            HeaderButtonService.PositionButtonsRightAligned(
                parentWidth: _buttonBar.Width,
                parentHeight: _buttonBar.Height,
                rightMargin: rightMargin,
                buttons: buttons);
        }

        private Button[] ActionButtons => new[] { _btnTheme, _btnSettings, _btnAbout };

        // ─────────────────────────────────────────────────────────────────────
        // ACTIONS
        // ─────────────────────────────────────────────────────────────────────

        private void BtnTheme_Click(object sender, EventArgs e)
        {
            _themeManager.ToggleTheme();
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            using var dlg = new SettingsDialog();
            dlg.ShowDialog(this.FindForm());
        }

        private void BtnAbout_Click(object sender, EventArgs e)
        {
            using var dlg = new AboutForm();
            dlg.ShowDialog(this.FindForm());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _toolTip?.Dispose();

            base.Dispose(disposing);
        }
    }
}
