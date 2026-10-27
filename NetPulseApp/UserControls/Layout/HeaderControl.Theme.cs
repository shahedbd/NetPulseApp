// UserControls/Layout/HeaderControl.Theme.cs
using NetPulseApp.Service.Dpi;
using NetPulseApp.Service.Icons;
using NetPulseApp.UiComponent;

namespace NetPulseApp.UserControls.Layout
{
    /// <summary>
    /// HeaderControl — theme re-application and DPI refresh.
    /// </summary>
    public partial class HeaderControl
    {
        // ─────────────────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Re-applies the current theme to every header control.</summary>
        public void ApplyTheme()
        {
            var theme = _themeManager.CurrentTheme;

            BackColor = theme.HeaderBackground;
            _buttonBar.BackColor = theme.HeaderBackground;
            _bottomBorder.BackColor = theme.BorderColor;
            _leftAccent.BackColor = HeaderButtonService.BrandColor;

            // Segment labels take their colour at build time — rebuild them.
            RenderSummary();

            // Colourful icons on a neutral surface — HeaderControl.ActionStyle.cs.
            StyleActionButtons();
        }

        // ─────────────────────────────────────────────────────────────────────
        // DPI REFRESH
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Resizes the bar, summary strip and button group after a DPI change.</summary>
        public void RefreshForDpi()
        {
            int btnH = HeaderButtonService.ButtonHeight;

            Height = HeaderHeight;
            _buttonBar.Height = HeaderHeight;
            _leftAccent.Width = DpiAwareService.Scale(3);
            _bottomBorder.Height = DpiAwareService.Scale(1);

            _summaryStrip.Padding = SummaryPadding();
            RenderSummary();

            foreach (var btn in ActionButtons)
            {
                btn.Size = new Size(HeaderButtonService.IconOnlyWidth, btnH);

                if (btn is IconButton icon)
                    icon.IconSize = ActionIconSize;
            }

            PositionButtons();
        }
    }
}
