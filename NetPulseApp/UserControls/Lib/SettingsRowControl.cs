using NetPulseApp.Managers;

namespace NetPulseApp.UserControls.Lib
{
    /// <summary>
    /// One settings row: a title (with an optional description on a second
    /// line) on the left, and a caller-supplied input control anchored to
    /// the right. Build a settings page from these so label alignment and
    /// spacing stay consistent without each section repeating its own layout
    /// math. ApplyTheme also themes the input via SettingsStyle so stock
    /// WinForms chrome doesn't leak into dark mode.
    /// </summary>
    public class SettingsRowControl : Panel
    {
        private readonly Label _lblTitle;
        private readonly Label _lblDescription;
        private readonly Control _input;

        public Control Input => _input;

        public SettingsRowControl(string title, Control input, string description = null)
        {
            _input = input;
            Height = SettingsStyle.Scale(description == null ? 36 : 52);
            Margin = new Padding(0, 0, 0, SettingsStyle.Scale(8));

            _lblTitle = new Label
            {
                Text = title,
                AutoSize = true,
                Location = new Point(0, description == null ? Math.Max(0, (Height - SettingsStyle.Scale(18)) / 2) : SettingsStyle.Scale(2)),
                Font = new Font("Segoe UI", 9.5f)
            };
            Controls.Add(_lblTitle);

            if (description != null)
            {
                _lblDescription = new Label
                {
                    Text = description,
                    AutoSize = true,
                    Location = new Point(0, _lblTitle.Bottom + SettingsStyle.Scale(2)),
                    Font = new Font("Segoe UI", 8f)
                };
                Controls.Add(_lblDescription);
            }

            input.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(input);
        }

        /// <summary>Sets the row's width and repositions the input to hug the new right edge.</summary>
        public void SetWidth(int width)
        {
            Width = width;
            _input.Location = new Point(Math.Max(0, width - _input.Width), Math.Max(0, (Height - _input.Height) / 2));
        }

        private bool _enabled = true;

        /// <summary>Toggles the row's input and dims its labels, so disabled rows lose visual emphasis instead of just graying the input.</summary>
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            _input.Enabled = enabled;
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }

        public void ApplyTheme(AppTheme theme)
        {
            _lblTitle.ForeColor = _enabled ? theme.TextColor : theme.DisabledColor;
            if (_lblDescription != null)
                _lblDescription.ForeColor = _enabled ? theme.SecondaryTextColor : theme.DisabledColor;

            BackColor = theme.CardBackground;
            _lblTitle.BackColor = theme.CardBackground;
            if (_lblDescription != null)
                _lblDescription.BackColor = theme.CardBackground;

            // ThemeInput no-ops on control types it doesn't recognise
            // (ToggleSwitchControl, ColorPickerButton), so no guard needed.
            SettingsStyle.ThemeInput(_input);
        }
    }
}
