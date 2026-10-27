// UserControls/Layout/HeaderControl.ActionStyle.cs
using NetPulseApp.Service.Dpi;
using NetPulseApp.Service.Icons;
using NetPulseApp.UiComponent;

namespace NetPulseApp.UserControls.Layout
{
    /// <summary>
    /// HeaderControl — the action buttons on the right and their colours.
    ///
    /// Settings and Theme are icon-only buttons on a neutral surface
    /// (header-toned fill, thin border) so each icon can carry its own fixed
    /// colour. The colours are mid-saturation so they read on both themes.
    /// </summary>
    public partial class HeaderControl
    {
        private static readonly Color SettingsIconColor = Color.FromArgb(139, 92, 246); // purple
        private static readonly Color SunColor = Color.FromArgb(251, 191, 36);          // amber
        private static readonly Color MoonColor = Color.FromArgb(99, 102, 241);         // indigo

        // Theme toggle glyphs — Segoe MDL2 Assets (Brightness / QuietHours),
        // drawn as button text rather than a Font Awesome image, matching
        // CpuZxPro.
        private const string SunGlyph = "";
        private const string MoonGlyph = "";
        private static readonly Font ThemeGlyphFont = new("Segoe MDL2 Assets", 12f, FontStyle.Regular);

        /// <summary>Icon-only buttons carry no label, so the glyph runs a touch larger.</summary>
        private static int ActionIconSize => HeaderButtonService.IconSize + DpiAwareService.Scale(2);

        // ─────────────────────────────────────────────────────────────────────
        // BUILD
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Icon-only action button. Built here rather than through
        /// HeaderButtonService, whose buttons carry the brand fill and would
        /// override the neutral surface.
        /// </summary>
        private IconButton CreateActionButton(IconChar icon, string tooltip, EventHandler onClick)
        {
            var btn = new IconButton
            {
                IconChar = icon,
                IconSize = ActionIconSize,
                Text = string.Empty,
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Size = new Size(HeaderButtonService.IconOnlyWidth, HeaderButtonService.ButtonHeight),
                Cursor = Cursors.Hand
            };

            if (!string.IsNullOrEmpty(tooltip))
                _toolTip.SetToolTip(btn, tooltip);

            btn.Click += onClick;
            return btn;
        }

        // ─────────────────────────────────────────────────────────────────────
        // STYLE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Neutral surface for an action button; the icon keeps <paramref name="iconColor"/>.</summary>
        private void StyleActionButton(Button btn, Color iconColor)
        {
            if (btn == null || btn.IsDisposed) return;

            var theme = _themeManager.CurrentTheme;

            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = theme.HeaderBackground;
            btn.ForeColor = theme.TextColor;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = theme.BorderColor;
            btn.FlatAppearance.MouseOverBackColor = theme.HoverColor;
            btn.FlatAppearance.MouseDownBackColor = theme.SelectedColor;

            if (btn is IconButton icon)
                icon.IconColor = iconColor;
        }

        private void StyleActionButtons()
        {
            StyleActionButton(_btnSettings, SettingsIconColor);
            StyleActionButton(_btnTheme, _themeManager.IsDarkTheme ? SunColor : MoonColor);
            UpdateThemeButtonIcon();
        }

        /// <summary>
        /// Sun in dark mode (switch to light), moon in light mode — each in its
        /// own colour. Call after every theme toggle.
        /// </summary>
        public void UpdateThemeButtonIcon()
        {
            if (_btnTheme == null || _btnTheme.IsDisposed) return;

            bool isDark = _themeManager.IsDarkTheme;

            if (_btnTheme is IconButton iconBtn)
                iconBtn.IconChar = IconChar.None;

            _btnTheme.Font = ThemeGlyphFont;
            _btnTheme.Text = isDark ? SunGlyph : MoonGlyph;
            _btnTheme.ForeColor = isDark ? SunColor : MoonColor;
            _btnTheme.TextAlign = ContentAlignment.MiddleCenter;
            _btnTheme.Padding = Padding.Empty;

            _toolTip.SetToolTip(_btnTheme, isDark ? "Switch to Light Mode" : "Switch to Dark Mode");
        }
    }
}
