using NetPulseApp.Helper;
using NetPulseApp.Managers;
using NetPulseApp.UserControls.About;

namespace NetPulseApp.Forms
{
    /// <summary>
    /// About dialog, laid out like CpuZxPro's: a scrolling stack of the
    /// header (icon, name, version, tagline), one AboutFeatureCard per
    /// AboutContent.Cards entry, and the footer (website, support address
    /// with copy, developer, copyright). Content comes from AppConfig's
    /// About section and AboutContent — nothing here needs editing.
    /// </summary>
    public class AboutForm : Form
    {
        private readonly ThemeManager _theme;
        private readonly float _dpiScale;

        private FlowLayoutPanel _stack;
        private AboutHeaderControl _header;
        private AboutFooterControl _footer;
        private readonly List<AboutFeatureCard> _cards = new();

        public AboutForm()
        {
            _theme = ThemeManager.Instance;

            using (Graphics g = CreateGraphics())
                _dpiScale = g.DpiX / 96f;

            InitializeComponent();
            BuildLayout();
            ApplyTheme();

            _theme.ThemeChanged += OnThemeChanged;
        }

        private int S(int value) => (int)Math.Round(value * _dpiScale);

        // ─────────────────────────────────────────────────────────────────────
        // FORM
        // ─────────────────────────────────────────────────────────────────────

        private void InitializeComponent()
        {
            SuspendLayout();

            ClientSize = new Size(S(520), S(720));
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Icon = File.Exists(AppConfig.FaviconPath) ? new Icon(AppConfig.FaviconPath) : null;
            Name = "AboutForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = $"About {AppConfig.AppName}";
            BackColor = _theme.CurrentTheme.ContentBackground;
            AutoScaleMode = AutoScaleMode.Dpi;

            ResumeLayout(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        // LAYOUT
        // ─────────────────────────────────────────────────────────────────────

        private void BuildLayout()
        {
            _stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = _theme.CurrentTheme.ContentBackground,
                Padding = new Padding(S(24), S(16), S(24), S(16))
            };
            Controls.Add(_stack);

            // Content width leaves room for the scrollbar so nothing reflows
            // when the stack grows past the dialog height.
            int width = ClientSize.Width - S(48) - SystemInformation.VerticalScrollBarWidth;

            _header = new AboutHeaderControl(_theme, _dpiScale, width);
            _stack.Controls.Add(_header);

            foreach (var spec in AboutContent.Cards)
            {
                var card = new AboutFeatureCard(_theme, _dpiScale, width, spec);
                _cards.Add(card);
                _stack.Controls.Add(card);
            }

            _footer = new AboutFooterControl(_theme, _dpiScale, width);
            _stack.Controls.Add(_footer);
        }

        // ─────────────────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────────────────

        private void ApplyTheme()
        {
            BackColor = _theme.CurrentTheme.ContentBackground;
            _stack.BackColor = _theme.CurrentTheme.ContentBackground;

            _header.ApplyTheme();
            _footer.ApplyTheme();
            foreach (var card in _cards)
                card.ApplyTheme();
        }

        private void OnThemeChanged(object sender, ThemeChangedEventArgs e) => ApplyTheme();

        /// <summary>
        /// Opens at the top. WinForms scrolls an AutoScroll container to
        /// whichever child holds focus, and the first selectable control here
        /// is the footer's copy button — which landed the dialog mid-page.
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            ActiveControl = null;
            _stack.AutoScrollPosition = Point.Empty;
            _stack.PerformLayout();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _theme.ThemeChanged -= OnThemeChanged;
            base.OnFormClosed(e);
        }
    }
}
