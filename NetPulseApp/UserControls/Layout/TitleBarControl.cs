// UserControls/Layout/TitleBarControl.cs
using System.Runtime.InteropServices;
using NetPulseApp.Helper;
using NetPulseApp.Managers;
using NetPulseApp.Service.Dpi;

namespace NetPulseApp.UserControls.Layout
{
    /// <summary>
    /// Part 01 — custom-drawn title bar. Shows AppConfig.AppName + AppSubtitle
    /// on the left and Minimize/Close buttons on the right. No maximize button —
    /// fixed-layout template design.
    /// </summary>
    public class TitleBarControl : UserControl
    {
        // Segoe MDL2 Assets glyphs
        private const string GlyphMinimize = "\uE921";
        private const string GlyphClose = "\uE8BB";

        private readonly ThemeManager _themeManager;

        /// <summary>
        /// The bar is a horizontal gradient derived from AppConfig.TitleBarColor
        /// (which defaults to AppConfig.PrimaryColor — the app's brand color).
        /// ApplyTheme() sets the stops per theme: dark mode blends toward
        /// black, light mode toward white. Child controls are transparent, so
        /// OnPaint's gradient shows through everywhere; BackColor is kept as a
        /// flat fallback for the paintless edges of resizes.
        /// </summary>
        private Color _gradientStart = AppConfig.TitleBarColor;
        private Color _gradientEnd = AppConfig.TitleBarColor;

        private Panel _iconPanel;
        private PictureBox _pbAppIcon;
        private Label _lblTitle;
        private Panel _captionButtonBar;
        private Button _btnMinimize;
        private Button _btnClose;

        public TitleBarControl()
        {
            _themeManager = ThemeManager.Instance;

            Dock = DockStyle.Top;
            Height = DpiAwareService.Scale(AppConfig.TitleBarHeight);
            DoubleBuffered = true;

            BuildLayout();
            ApplyTheme();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // The gradient brush spans ClientRectangle — repaint when it changes.
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                ClientRectangle, _gradientStart, _gradientEnd,
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, ClientRectangle);

            base.OnPaint(e);
        }

        private void BuildLayout()
        {
            // Fixed-width right-side panel, hosting the caption buttons.
            _captionButtonBar = new Panel
            {
                Dock = DockStyle.Right,
                Width = DpiAwareService.Scale(96)
            };

            _btnMinimize = CreateCaptionButton(GlyphMinimize);
            _btnMinimize.Click += (s, e) =>
            {
                if (FindForm() is Form f) f.WindowState = FormWindowState.Minimized;
            };

            _btnClose = CreateCaptionButton(GlyphClose);
            _btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            _btnClose.MouseEnter += (s, e) => _btnClose.ForeColor = Color.White;
            _btnClose.MouseLeave += (s, e) => _btnClose.ForeColor = GetContrastColor(_gradientStart);
            _btnClose.Click += (s, e) => FindForm()?.Close();

            // Dock=Right on each button (add order: Minimize first, Close
            // last → Close ends up outermost, flush against the window's
            // right edge). Dock also stretches each button to the panel's
            // full height automatically — so the hover highlight spans the
            // entire title bar top-to-bottom, matching native Windows
            // caption buttons, instead of a fixed height sitting at a
            // separately-computed vertical offset that could drift out of
            // sync with it.
            _captionButtonBar.Controls.Add(_btnMinimize);
            _captionButtonBar.Controls.Add(_btnClose);

            _lblTitle = new Label
            {
                Text = $"{AppConfig.AppName} — {AppConfig.AppSubtitle}",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(DpiAwareService.Scale(8), 0, 0, 0),
                BackColor = Color.Transparent
            };
            _lblTitle.MouseDown += TitleBar_MouseDown;
            MouseDown += TitleBar_MouseDown;

            // Left: app icon, in its own fixed-width panel so it never
            // overlaps the title (same reasoning as _captionButtonBar).
            _pbAppIcon = new PictureBox
            {
                Image = LoadAppIcon(),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            _iconPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = DpiAwareService.Scale(40),
                BackColor = Color.Transparent
            };
            _iconPanel.Controls.Add(_pbAppIcon);
            _iconPanel.MouseDown += TitleBar_MouseDown;
            _pbAppIcon.MouseDown += TitleBar_MouseDown;

            // Add order (first → last): Fill title first (innermost), then
            // the Left icon panel and Right caption-button bar — none of the
            // three overlap in bounds, so z-order can't hide any of them.
            Controls.Add(_lblTitle);
            Controls.Add(_iconPanel);
            Controls.Add(_captionButtonBar);

            LayoutAppIcon();
        }

        /// <summary>Centers the app icon inside its left panel at the current DPI.</summary>
        private void LayoutAppIcon()
        {
            int size = DpiAwareService.Scale(22);
            _pbAppIcon.Size = new Size(size, size);
            _pbAppIcon.Location = new Point(
                (_iconPanel.Width - size) / 2,
                (Height - size) / 2);
        }

        private Image LoadAppIcon()
        {
            try
            {
                if (!File.Exists(AppConfig.FaviconPath)) return null;

                using var icon = new Icon(AppConfig.FaviconPath);
                return icon.ToBitmap();
            }
            catch
            {
                // The icon is decorative — a missing or unreadable file must
                // never stop the window from opening.
                return null;
            }
        }

        private Button CreateCaptionButton(string glyph)
        {
            var btn = new Button
            {
                Text = glyph,
                Dock = DockStyle.Right,
                Width = DpiAwareService.Scale(45),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe MDL2 Assets", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ─────────────────────────────────────────────────────────────────────
        // WINDOW DRAG (standard borderless-form drag pattern)
        // ─────────────────────────────────────────────────────────────────────

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (FindForm() is not Form form) return;

            ReleaseCapture();
            SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        // ─────────────────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────────────────

        public void ApplyTheme()
        {
            bool isDark = _themeManager.IsDarkTheme;

            if (isDark)
            {
                _gradientStart = Blend(AppConfig.TitleBarColor, Color.Black, 0.45);
                _gradientEnd = Blend(AppConfig.TitleBarColor, Color.Black, 0.75);
            }
            else
            {
                _gradientStart = Blend(AppConfig.TitleBarColor, Color.White, 0.55);
                _gradientEnd = Blend(AppConfig.TitleBarColor, Color.White, 0.82);
            }

            // Flat fallback matching the left stop; the real bar is painted
            // in OnPaint.
            BackColor = _gradientStart;
            _captionButtonBar.BackColor = Color.Transparent;
            Invalidate();

            // Title text and caption buttons read against whichever stop is
            // less extreme for the active theme — dark mode's left stop
            // (closer to the un-blended brand color) or light mode's right
            // stop (closer to white) — rather than the theme's HeadingColor,
            // which is dark in light mode and would vanish against a
            // saturated/dark TitleBarColor like a brand blue.
            Color contrastBase = isDark ? _gradientStart : _gradientEnd;
            Color foreColor = GetContrastColor(contrastBase);

            if (_lblTitle != null)
            {
                _lblTitle.BackColor = Color.Transparent;
                _lblTitle.ForeColor = foreColor;
            }

            foreach (var btn in new[] { _btnMinimize, _btnClose })
            {
                if (btn == null) continue;
                btn.ForeColor = foreColor;
            }

            if (_btnMinimize != null)
                _btnMinimize.FlatAppearance.MouseOverBackColor = GetHoverColor(contrastBase);
        }

        /// <summary>Linear blend of <paramref name="from"/> toward <paramref name="to"/> by <paramref name="amount"/> (0..1).</summary>
        private static Color Blend(Color from, Color to, double amount)
        {
            return Color.FromArgb(
                (int)Math.Round(from.R + (to.R - from.R) * amount),
                (int)Math.Round(from.G + (to.G - from.G) * amount),
                (int)Math.Round(from.B + (to.B - from.B) * amount));
        }

        /// <summary>White on dark/saturated backgrounds, near-black on light ones.</summary>
        private static Color GetContrastColor(Color backColor)
        {
            // Perceived luminance (Rec. 601) — catches saturated colors like
            // a brand-blue title bar that a plain RGB average would rate
            // mid-range (and therefore pick the wrong contrast color for).
            double luminance = (backColor.R * 0.299 + backColor.G * 0.587 + backColor.B * 0.114) / 255.0;
            return luminance < 0.6 ? Color.White : Color.FromArgb(24, 24, 24);
        }

        /// <summary>
        /// Nudges a color toward white or black (whichever contrasts more)
        /// for a hover state. AppConfig.TitleBarColor can be set to anything,
        /// so this can't assume it's a dark or light shade the way a fixed
        /// literal could.
        /// </summary>
        private static Color GetHoverColor(Color baseColor)
        {
            int brightness = (baseColor.R + baseColor.G + baseColor.B) / 3;
            int delta = brightness < 128 ? 30 : -30;

            return Color.FromArgb(
                Math.Clamp(baseColor.R + delta, 0, 255),
                Math.Clamp(baseColor.G + delta, 0, 255),
                Math.Clamp(baseColor.B + delta, 0, 255));
        }

        // ─────────────────────────────────────────────────────────────────────
        // DPI REFRESH
        // ─────────────────────────────────────────────────────────────────────

        public void RefreshForDpi()
        {
            Height = DpiAwareService.Scale(AppConfig.TitleBarHeight);
            _captionButtonBar.Width = DpiAwareService.Scale(96);
            _captionButtonBar.BackColor = Color.Transparent;
            _iconPanel.Width = DpiAwareService.Scale(40);

            // Height is handled automatically by Dock=Right; only Width needs
            // updating here.
            foreach (var btn in new[] { _btnMinimize, _btnClose })
                btn.Width = DpiAwareService.Scale(45);

            LayoutAppIcon();
        }
    }
}
