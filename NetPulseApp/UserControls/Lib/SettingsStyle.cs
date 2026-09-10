using NetPulseApp.Managers;
using NetPulseApp.Service.Dpi;

namespace NetPulseApp.UserControls.Lib
{
    /// <summary>
    /// Shared styling helpers for building a Settings-style page: DPI-scaled
    /// dimensions and theme-aware flat inputs, so ComboBox/NumericUpDown/
    /// Button don't show stock WinForms light-mode chrome inside a dark theme.
    /// </summary>
    public static class SettingsStyle
    {
        public static int Scale(int value) => (int)Math.Round(value * DpiAwareService.ScaleFactor);

        private static AppTheme Theme => ThemeManager.Instance.CurrentTheme;

        /// <summary>
        /// Themes a row input (ModernDropDown, NumericUpDown, ...) to match
        /// the page's card look. Unknown control types are left alone, so
        /// self-painting controls (ToggleSwitchControl, ColorPickerButton)
        /// can simply not be listed here rather than needing a guard at each
        /// call site.
        /// </summary>
        public static void ThemeInput(Control input)
        {
            var theme = Theme;
            bool dark = ThemeManager.Instance.IsDarkTheme;

            switch (input)
            {
                case ModernDropDown dropDown:
                    dropDown.ApplyTheme();
                    break;
                case ComboBox combo:
                    combo.FlatStyle = FlatStyle.Flat;
                    combo.BackColor = dark ? Color.FromArgb(55, 55, 55) : Color.White;
                    combo.ForeColor = theme.TextColor;
                    break;
                case NumericUpDown numeric:
                    numeric.BorderStyle = BorderStyle.FixedSingle;
                    numeric.BackColor = dark ? Color.FromArgb(55, 55, 55) : Color.White;
                    numeric.ForeColor = theme.TextColor;
                    break;
                // ColorPickerButton derives from Button but paints its own
                // swatch — ghost-theming it would fight its own styling, so
                // it is deliberately matched before the generic Button case.
                case ColorPickerButton:
                    break;
                case Button button:
                    ThemeGhostButton(button);
                    break;
            }
        }

        /// <summary>Solid accent-filled flat button (primary action).</summary>
        public static void ThemeSolidButton(Button button)
        {
            var theme = Theme;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = theme.AccentColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = theme.AccentHoverColor;
        }

        /// <summary>Bordered card-colored flat button (secondary action).</summary>
        public static void ThemeGhostButton(Button button)
        {
            var theme = Theme;
            bool dark = ThemeManager.Instance.IsDarkTheme;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = dark ? Color.FromArgb(45, 45, 45) : Color.FromArgb(240, 241, 243);
            button.ForeColor = theme.TextColor;
            button.FlatAppearance.BorderColor = theme.BorderColor;
            button.FlatAppearance.MouseOverBackColor = theme.HoverColor;
        }
    }
}
