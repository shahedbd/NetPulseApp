// UiComponent/ToolUiFactory.cs
using NetPulseApp.Managers;
using NetPulseApp.Service.Dpi;
using NetPulseApp.Service.Icons;

namespace NetPulseApp.UiComponent
{
    /// <summary>
    /// Shared widget factory + theming helpers for the tool pages
    /// (Ping / Traceroute / DNS / Port / WHOIS). Pages keep references to
    /// whatever they create and re-call the Apply*Theme helpers on
    /// ThemeManager.ThemeChanged — same pattern as the Layout controls.
    /// </summary>
    public static class ToolUiFactory
    {
        /// <summary>Combo item that shows a label but carries a raw value.</summary>
        public record LabeledValue(string Label, int Value)
        {
            public override string ToString() => Label;
        }

        private static AppTheme Theme => ThemeManager.Instance.CurrentTheme;

        // ─────────────────────────────────────────────────────────────────────
        // LABELS
        // ─────────────────────────────────────────────────────────────────────

        public static Label PageTitle(string text) => new()
        {
            Text = text,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = Theme.HeadingColor,
            BackColor = Color.Transparent,
            AutoSize = true
        };

        public static Label PageSubtitle(string text) => new()
        {
            Text = text,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Theme.SecondaryTextColor,
            BackColor = Color.Transparent,
            AutoSize = true
        };

        /// <summary>Small field caption, e.g. "TARGET" / "Interval".</summary>
        public static Label Caption(string text) => new()
        {
            Text = text,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Theme.SecondaryTextColor,
            BackColor = Color.Transparent,
            AutoSize = true
        };

        // ─────────────────────────────────────────────────────────────────────
        // INPUTS
        // ─────────────────────────────────────────────────────────────────────

        public static TextBox Input(int width) => new()
        {
            Width = DpiAwareService.Scale(width),
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle
        };

        /// <summary>Editable combo with history autocomplete (e.g. ping target).</summary>
        public static ComboBox EditableCombo(int width) => new()
        {
            Width = DpiAwareService.Scale(width),
            DropDownStyle = ComboBoxStyle.DropDown,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };

        public static void ApplyInputTheme(Control input)
        {
            input.BackColor = Theme.SecondaryBackground;
            input.ForeColor = Theme.TextColor;
        }

        // ─────────────────────────────────────────────────────────────────────
        // BUTTONS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Filled action button in its own accent (Start = green, Stop = red).</summary>
        public static IconButton AccentButton(string text, IconChar icon, Color accent) => new()
        {
            Text = text,
            IconChar = icon,
            IconColor = Color.White,
            IconSize = DpiAwareService.Scale(12),
            Size = new Size(DpiAwareService.Scale(94), DpiAwareService.Scale(34)),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            TextAlign = ContentAlignment.MiddleLeft,
            ImageAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(DpiAwareService.Scale(8), 0, DpiAwareService.Scale(8), 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        /// <summary>Quiet bordered button (Export Report / Copy Results).</summary>
        public static IconButton GhostButton(string text, IconChar icon) => new()
        {
            Text = text,
            IconChar = icon,
            IconColor = Theme.TextColor,
            IconSize = DpiAwareService.Scale(12),
            Size = new Size(DpiAwareService.Scale(120), DpiAwareService.Scale(34)),
            Font = new Font("Segoe UI", 9f),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            TextAlign = ContentAlignment.MiddleLeft,
            ImageAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(DpiAwareService.Scale(8), 0, DpiAwareService.Scale(8), 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        public static void ApplyAccentTheme(IconButton button, Color accent)
        {
            button.BackColor = accent;
            button.ForeColor = Color.White;
            button.IconColor = Color.White;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(accent, 0.1f);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(accent, 0.25f);
        }

        public static void ApplyGhostTheme(IconButton button)
        {
            button.BackColor = Theme.CardBackground;
            button.ForeColor = Theme.TextColor;
            button.IconColor = Theme.TextColor;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Theme.BorderColor;
            button.FlatAppearance.MouseOverBackColor = Theme.HoverColor;
            button.FlatAppearance.MouseDownBackColor = Theme.SelectedColor;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CARDS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Flat themed card panel — FixedSingle like the Lib controls.</summary>
        public static Panel Card() => new()
        {
            BackColor = Theme.CardBackground,
            BorderStyle = BorderStyle.FixedSingle
        };

        public static void ApplyCardTheme(Panel card) => card.BackColor = Theme.CardBackground;

        // ─────────────────────────────────────────────────────────────────────
        // EXPORT / COPY (shared footer actions)
        // ─────────────────────────────────────────────────────────────────────

        public static void CopyResults(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                ToastsNotificationManager.Instance.ShowInfoNotification("No results to copy yet.");
                return;
            }

            Clipboard.SetText(text);
            ToastsNotificationManager.Instance.ShowSuccessNotification("Results copied to clipboard.");
        }

        public static void ExportReport(IWin32Window owner, string fileName, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                ToastsNotificationManager.Instance.ShowInfoNotification("No results to export yet.");
                return;
            }

            using var dialog = new SaveFileDialog
            {
                FileName = fileName,
                Filter = "Text file (*.txt)|*.txt",
                DefaultExt = "txt"
            };

            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return;

            try
            {
                File.WriteAllText(dialog.FileName, text);
                ToastsNotificationManager.Instance.ShowSuccessNotification($"Report saved to {dialog.FileName}");
            }
            catch (Exception ex)
            {
                ToastsNotificationManager.Instance.ShowErrorNotification($"Export failed: {ex.Message}");
            }
        }
    }
}
