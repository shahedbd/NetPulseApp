// UserControls/About/AboutHeaderControl.cs
using NetPulseApp.Helper;
using NetPulseApp.Managers;

namespace NetPulseApp.UserControls.About
{
    /// <summary>
    /// About screen header: app icon, name, version badge and tagline.
    /// Copied from CpuZxPro's AboutHeaderControl — the edition badge is left
    /// out because NetPulse has a single (free) edition.
    /// </summary>
    public class AboutHeaderControl : Panel
    {
        private readonly ThemeManager _theme;
        private readonly float _dpiScale;

        private Label _lblAppName;
        private Label _lblTagline;
        private Label _lblSubTagline;
        private Panel _versionBadge;

        public AboutHeaderControl(ThemeManager theme, float dpiScale, int width)
        {
            _theme = theme;
            _dpiScale = dpiScale;

            Width = width;
            BackColor = Color.Transparent;
            Margin = new Padding(0, 0, 0, S(6));

            Build();
        }

        private int S(int value) => (int)Math.Round(value * _dpiScale);

        // ─────────────────────────────────────────────────────────────────────
        // BUILD
        // ─────────────────────────────────────────────────────────────────────

        private void Build()
        {
            int y = S(4);

            Controls.Add(new PictureBox
            {
                Image = File.Exists(AppConfig.AppIconPath) ? Image.FromFile(AppConfig.AppIconPath) : null,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(S(64), S(64)),
                Location = new Point((Width - S(64)) / 2, y),
                BackColor = Color.Transparent
            });
            y += S(70);

            _lblAppName = CenteredLabel(AppConfig.AppName,
                new Font("Segoe UI", 20f, FontStyle.Bold), _theme.CurrentTheme.HeadingColor, y);
            Controls.Add(_lblAppName);
            y += _lblAppName.Height + S(8);

            _versionBadge = BuildBadge(AppConfig.AppVersion, _theme.CurrentTheme.SecondaryTextColor);
            _versionBadge.Location = new Point((Width - _versionBadge.Width) / 2, y);
            Controls.Add(_versionBadge);
            y += _versionBadge.Height + S(14);

            _lblTagline = CenteredLabel(AppConfig.AppDescription,
                new Font("Segoe UI", 10f, FontStyle.Bold), _theme.CurrentTheme.TextColor, y);
            Controls.Add(_lblTagline);
            y += _lblTagline.Height + S(4);

            _lblSubTagline = CenteredLabel(AppConfig.AppSubDescription,
                new Font("Segoe UI", 8.5f), _theme.CurrentTheme.SecondaryTextColor, y);
            Controls.Add(_lblSubTagline);
            y += _lblSubTagline.Height + S(10);

            Height = y;
        }

        /// <summary>Pill badge — tinted background, colored bold text.</summary>
        private Panel BuildBadge(string text, Color accent)
        {
            var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            int width = TextRenderer.MeasureText(text, font).Width + S(20);

            var badge = new Panel
            {
                Size = new Size(width, S(22)),
                BackColor = Color.FromArgb(38, accent)
            };

            badge.Controls.Add(new Label
            {
                Text = text,
                Font = font,
                ForeColor = accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            return badge;
        }

        /// <summary>
        /// Full-width centred label, sized to the text it actually renders —
        /// a fixed height clipped descenders on the large app-name font.
        /// </summary>
        private Label CenteredLabel(string text, Font font, Color color, int y)
        {
            int height = TextRenderer.MeasureText(text, font).Height + S(6);

            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = color,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(Width, height),
                Location = new Point(0, y),
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────────────────

        public void ApplyTheme()
        {
            var t = _theme.CurrentTheme;
            _lblAppName.ForeColor = t.HeadingColor;
            _lblTagline.ForeColor = t.TextColor;
            _lblSubTagline.ForeColor = t.SecondaryTextColor;

            if (_versionBadge.Controls.Count > 0 && _versionBadge.Controls[0] is Label lblVersion)
            {
                lblVersion.ForeColor = t.SecondaryTextColor;
                _versionBadge.BackColor = Color.FromArgb(38, t.SecondaryTextColor);
            }
        }
    }
}
