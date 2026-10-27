// UserControls/About/AboutFeatureCard.cs
using NetPulseApp.Managers;
using NetPulseApp.Service.Icons;

namespace NetPulseApp.UserControls.About
{
    /// <summary>
    /// One About-screen card: accented header and a list of feature rows,
    /// with an optional note. Copied from CpuZxPro's AboutFeatureCard — the
    /// call-to-action button is left out (no paid tier in NetPulse).
    /// </summary>
    public class AboutFeatureCard : Panel
    {
        private readonly ThemeManager _theme;
        private readonly float _dpiScale;
        private readonly Color _accent;

        private Panel _header;
        private readonly List<Label> _rowLabels = new();
        private Label? _lblNote;

        public AboutFeatureCard(ThemeManager theme, float dpiScale, int width, AboutCardSpec spec)
        {
            _theme = theme;
            _dpiScale = dpiScale;
            _accent = spec.Accent;

            Width = width;
            BackColor = _theme.CurrentTheme.CardBackground;
            Margin = new Padding(0, 0, 0, S(14));

            Build(spec);
        }

        private int S(int value) => (int)Math.Round(value * _dpiScale);

        // ─────────────────────────────────────────────────────────────────────
        // BUILD
        // ─────────────────────────────────────────────────────────────────────

        private void Build(AboutCardSpec spec)
        {
            int headerH = S(38);
            AddHeader(spec.Icon, spec.Title, headerH);

            int y = headerH + S(8);
            foreach (var (rowIcon, text, rowAccent) in spec.Rows)
            {
                AddRow(rowIcon, rowAccent, text, y);
                y += S(26);
            }

            if (!string.IsNullOrWhiteSpace(spec.Note))
            {
                _lblNote = new Label
                {
                    Text = spec.Note,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    ForeColor = _theme.CurrentTheme.SecondaryTextColor,
                    BackColor = Color.Transparent,
                    AutoSize = false,
                    UseMnemonic = false,
                    Size = new Size(Width - S(32), S(34)),
                    Location = new Point(S(16), y + S(2)),
                    TextAlign = ContentAlignment.TopLeft
                };
                Controls.Add(_lblNote);
                y += S(38);
            }

            Height = y + S(10);
        }

        private void AddHeader(IconChar icon, string title, int headerH)
        {
            _header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(Width, headerH),
                BackColor = Color.FromArgb(28, _accent)
            };

            // Left accent bar — same language as the page cards.
            _header.Controls.Add(new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(S(4), headerH),
                BackColor = _accent
            });

            _header.Controls.Add(new IconPictureBox
            {
                IconChar = icon,
                IconColor = _accent,
                IconSize = S(16),
                Size = new Size(S(22), headerH),
                Location = new Point(S(14), 0),
                SizeMode = PictureBoxSizeMode.CenterImage,
                BackColor = Color.Transparent
            });

            _header.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = _accent,
                BackColor = Color.Transparent,
                AutoSize = false,
                UseMnemonic = false,
                Size = new Size(Width - S(44), headerH),
                Location = new Point(S(42), 0),
                TextAlign = ContentAlignment.MiddleLeft
            });

            Controls.Add(_header);
        }

        private void AddRow(IconChar icon, Color accent, string text, int y)
        {
            Controls.Add(new IconPictureBox
            {
                IconChar = icon,
                IconColor = accent,
                IconSize = S(14),
                Size = new Size(S(20), S(24)),
                Location = new Point(S(16), y),
                SizeMode = PictureBoxSizeMode.CenterImage,
                BackColor = Color.Transparent,
                Enabled = false
            });

            var label = new Label
            {
                Text = text,
                UseMnemonic = false, // feature strings may contain real ampersands
                Font = new Font("Segoe UI", 9f),
                ForeColor = _theme.CurrentTheme.TextColor,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(Width - S(52), S(24)),
                Location = new Point(S(44), y),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            _rowLabels.Add(label);
            Controls.Add(label);
        }

        // ─────────────────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────────────────

        public void ApplyTheme()
        {
            BackColor = _theme.CurrentTheme.CardBackground;
            _header.BackColor = Color.FromArgb(28, _accent);

            foreach (var label in _rowLabels)
                label.ForeColor = _theme.CurrentTheme.TextColor;

            if (_lblNote != null)
                _lblNote.ForeColor = _theme.CurrentTheme.SecondaryTextColor;
        }
    }
}
