// UserControls/About/AboutFooterControl.cs
using DeviceDataModule;
using NetPulseApp.Helper;
using NetPulseApp.Managers;
using NetPulseApp.Service.Icons;

namespace NetPulseApp.UserControls.About
{
    /// <summary>
    /// About screen footer: website link, support address (with a copy action)
    /// and the developer/copyright lines. Closing is left to the dialog's own
    /// title bar — a second Close button below the fold only added noise.
    /// Reusable by every About dialog. Copied from CpuZxPro; contact details
    /// come from AppConfig's About section.
    /// </summary>
    public class AboutFooterControl : Panel
    {
        private readonly ThemeManager _theme;
        private readonly float _dpiScale;

        private Panel _divider;
        private IconPictureBox _globe;
        private IconPictureBox _mail;
        private LinkLabel _lnkWebsite;
        private Label _lblSupport;
        private Label _lblDeveloper;
        private Label _lblCopyright;
        private IconButton _btnCopyEmail;
        private readonly ToolTip _tips = new();

        public AboutFooterControl(ThemeManager theme, float dpiScale, int width)
        {
            _theme = theme;
            _dpiScale = dpiScale;

            Width = width;
            BackColor = Color.Transparent;
            Margin = new Padding(0, 0, 0, S(12));

            Build();
        }

        private int S(int value) => (int)Math.Round(value * _dpiScale);

        // ─────────────────────────────────────────────────────────────────────
        // BUILD
        // ─────────────────────────────────────────────────────────────────────

        private void Build()
        {
            int y = 0;

            _divider = new Panel
            {
                Size = new Size(Width, S(1)),
                Location = new Point(0, y),
                BackColor = _theme.CurrentTheme.BorderColor
            };
            Controls.Add(_divider);
            y += S(14);

            y = AddWebsite(y);
            y = AddSupport(y);

            _lblDeveloper = CenteredLabel($"Developed by {AppConfig.DeveloperName}",
                new Font("Segoe UI", 8.5f, FontStyle.Bold), _theme.CurrentTheme.TextColor, y);
            Controls.Add(_lblDeveloper);
            y += S(20);

            _lblCopyright = CenteredLabel(AppConfig.Copyright,
                new Font("Segoe UI", 8f), _theme.CurrentTheme.SecondaryTextColor, y);
            Controls.Add(_lblCopyright);

            // Breathing room under the last line — the stack's own padding
            // alone left the copyright sitting on the dialog's bottom edge.
            y += S(18) + S(16);

            Height = y;
        }

        /// <summary>Globe + link, centred as one block.</summary>
        private int AddWebsite(int y)
        {
            _globe = RowIcon(IconChar.Globe, _theme.CurrentTheme.AccentColor);

            _lnkWebsite = new LinkLabel
            {
                Text = AppConfig.WebsiteLabel,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                LinkBehavior = LinkBehavior.HoverUnderline,
                LinkColor = _theme.CurrentTheme.AccentColor,
                ActiveLinkColor = _theme.CurrentTheme.AccentColor,
                VisitedLinkColor = _theme.CurrentTheme.AccentColor
            };
            _lnkWebsite.LinkClicked += (s, e) =>
                _ = Utility.StartProcessAsync(AppConfig.WebsiteUrl, 0);

            CenterPair(_globe, _lnkWebsite, y);
            Controls.Add(_globe);
            Controls.Add(_lnkWebsite);
            return y + S(26);
        }

        /// <summary>Envelope + support address + copy action, centred as one
        /// block. The address is the one in AppConfig.SupportEmail.</summary>
        private int AddSupport(int y)
        {
            _mail = RowIcon(IconChar.Envelope, _theme.CurrentTheme.SecondaryTextColor);

            _lblSupport = new Label
            {
                Text = AppConfig.SupportEmail,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = _theme.CurrentTheme.SecondaryTextColor,
                AutoSize = true,
                UseMnemonic = false,
                BackColor = Color.Transparent
            };

            _btnCopyEmail = new IconButton
            {
                IconChar = IconChar.Copy,
                IconColor = _theme.CurrentTheme.AccentColor,
                IconSize = S(13),
                Text = string.Empty,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Size = new Size(S(24), S(22)),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            _btnCopyEmail.FlatAppearance.BorderSize = 0;
            _btnCopyEmail.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(38, _theme.CurrentTheme.AccentColor);
            _btnCopyEmail.Click += (s, e) => CopyEmail();
            _tips.SetToolTip(_btnCopyEmail, "Copy the support address");

            // Measured rather than read off the label: AutoSize width isn't
            // settled until the label has a parent.
            int labelWidth = TextRenderer.MeasureText(_lblSupport.Text, _lblSupport.Font).Width;
            int blockWidth = _mail.Width + S(4) + labelWidth + S(2) + _btnCopyEmail.Width;
            int x = (Width - blockWidth) / 2;

            _mail.Location = new Point(x, y);
            _lblSupport.Location = new Point(_mail.Right + S(4), y + S(2));
            _btnCopyEmail.Location = new Point(_lblSupport.Left + labelWidth + S(2), y - S(1));

            Controls.Add(_mail);
            Controls.Add(_lblSupport);
            Controls.Add(_btnCopyEmail);
            return y + S(30);
        }

        private static void CopyEmail()
        {
            try
            {
                Clipboard.SetText(AppConfig.SupportEmail);
                ToastsNotificationManager.Instance.ShowSuccessNotification("Support address copied.");
            }
            catch (Exception ex)
            {
                Logger.Log($"AboutFooterControl: copy email failed: {ex.Message}");
                ToastsNotificationManager.Instance.ShowErrorNotification("Clipboard busy — try again.");
            }
        }

        private IconPictureBox RowIcon(IconChar icon, Color color) => new()
        {
            IconChar = icon,
            IconColor = color,
            // Size before IconSize — see AboutFeatureCard.AddHeader.
            Size = new Size(S(18), S(20)),
            IconSize = S(14),
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.Transparent,
            Enabled = false
        };

        /// <summary>Lays an icon and its label out side by side, centred.</summary>
        private void CenterPair(Control icon, Control label, int y)
        {
            int labelWidth = TextRenderer.MeasureText(label.Text, label.Font).Width;
            int blockWidth = icon.Width + S(4) + labelWidth;

            icon.Location = new Point((Width - blockWidth) / 2, y);
            label.Location = new Point(icon.Right + S(4), y + S(2));
        }

        private Label CenteredLabel(string text, Font font, Color color, int y) => new()
        {
            Text = text,
            Font = font,
            ForeColor = color,
            BackColor = Color.Transparent,
            AutoSize = false,
            UseMnemonic = false,
            Size = new Size(Width, S(18)),
            Location = new Point(0, y),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ─────────────────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────────────────

        public void ApplyTheme()
        {
            var theme = _theme.CurrentTheme;

            if (_divider != null) _divider.BackColor = theme.BorderColor;
            if (_globe != null) _globe.IconColor = theme.AccentColor;
            if (_mail != null) _mail.IconColor = theme.SecondaryTextColor;
            if (_lblSupport != null) _lblSupport.ForeColor = theme.SecondaryTextColor;
            if (_lblDeveloper != null) _lblDeveloper.ForeColor = theme.TextColor;
            if (_lblCopyright != null) _lblCopyright.ForeColor = theme.SecondaryTextColor;

            if (_lnkWebsite != null)
            {
                _lnkWebsite.LinkColor = theme.AccentColor;
                _lnkWebsite.ActiveLinkColor = theme.AccentColor;
                _lnkWebsite.VisitedLinkColor = theme.AccentColor;
            }

            if (_btnCopyEmail != null)
            {
                _btnCopyEmail.IconColor = theme.AccentColor;
                _btnCopyEmail.FlatAppearance.MouseOverBackColor =
                    Color.FromArgb(38, theme.AccentColor);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tips.Dispose();
            base.Dispose(disposing);
        }
    }
}
