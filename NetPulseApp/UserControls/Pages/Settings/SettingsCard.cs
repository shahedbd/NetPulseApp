using NetPulseApp.Managers;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages.Settings
{
    /// <summary>
    /// One bordered card of settings rows, stacked top to bottom (ported from
    /// NetSpeedMeterPlus; flat border like ToolUiFactory.Card instead of the
    /// rounded CardStyle NetPulse doesn't have). Owns its children's widths so
    /// a row's input stays hugging the right edge as the page resizes.
    /// </summary>
    internal sealed class SettingsCard : FlowLayoutPanel
    {
        private static int S(int value) => SettingsStyle.Scale(value);

        public SettingsCard()
        {
            FlowDirection = FlowDirection.TopDown;
            WrapContents = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(S(14), S(8), S(14), S(8));
            Margin = new Padding(0, 0, 0, S(16));
            BackColor = ThemeManager.Instance.CurrentTheme.CardBackground;
            DoubleBuffered = true;
            Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.Instance.CurrentTheme.BorderColor);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };
        }

        /// <summary>Adds a settings row (themed here, so callers don't have to).</summary>
        public SettingsRowControl AddRow(SettingsRowControl row)
        {
            row.ApplyTheme(ThemeManager.Instance.CurrentTheme);
            row.Margin = new Padding(0, S(2), 0, S(2));
            Controls.Add(row);
            return row;
        }

        /// <summary>Adds any full-width control (a button strip, a note).</summary>
        public T AddFull<T>(T control) where T : Control
        {
            control.Margin = new Padding(0, S(4), 0, S(4));
            if (control.BackColor == Color.Empty || control.BackColor == SystemColors.Control)
                control.BackColor = ThemeManager.Instance.CurrentTheme.CardBackground;
            Controls.Add(control);
            return control;
        }

        /// <summary>A thin rule between rows.</summary>
        public void AddDivider()
        {
            Controls.Add(new Panel
            {
                Height = 1,
                Margin = new Padding(0, S(2), 0, S(2)),
                BackColor = ThemeManager.Instance.CurrentTheme.BorderColor
            });
        }

        /// <summary>Sets the card's outer width and stretches every child to the inner width.</summary>
        public void SetWidth(int width)
        {
            int inner = width - Padding.Horizontal;
            foreach (Control child in Controls)
            {
                if (child is SettingsRowControl row)
                {
                    row.SetWidth(inner);
                    continue;
                }
                // An AutoSize strip ignores Width but wraps at its MaximumSize.
                // Only those get one: on a fixed-height control the setter
                // clamps the current Height to 0 before treating 0 as "no limit".
                if (child.AutoSize)
                    child.MaximumSize = new Size(inner, 0);
                child.Width = inner;
            }
            // AutoSize grows to fit; pin the width so a narrow child can't shrink the card.
            MinimumSize = new Size(width, 0);
            MaximumSize = new Size(width, 0);
            Invalidate();
        }
    }
}
