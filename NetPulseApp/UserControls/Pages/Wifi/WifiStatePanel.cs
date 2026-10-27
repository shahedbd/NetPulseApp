// UserControls/Pages/Wifi/WifiStatePanel.cs
using NetPulseApp.Service.Icons;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Centred "no data" message for the Wi-Fi page (copied from
    /// WiFiPasswordViewer, buttons switched to ToolUiFactory): icon, title,
    /// body and one or two actions. Covers Location denied / Wi-Fi off /
    /// no adapter / not connected.
    /// </summary>
    public class WifiStatePanel : DpiAwareUserControl
    {
        private readonly FlowLayoutPanel _stack;
        private readonly IconPictureBox _icon;
        private readonly Label _title;
        private readonly Label _body;
        private readonly FlowLayoutPanel _buttons;

        public WifiStatePanel()
        {
            Dock = DockStyle.Fill;

            _stack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };

            _icon = new IconPictureBox
            {
                IconSize = Scale(44),
                Size = ScaledSize(44, 44),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top
            };

            _title = new Label
            {
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top,
                Margin = new Padding(0, Scale(10), 0, Scale(6))
            };

            _body = new Label
            {
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top,
                MaximumSize = new Size(Scale(520), 0),
                TextAlign = ContentAlignment.TopCenter
            };

            _buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top,
                Margin = new Padding(0, Scale(14), 0, 0)
            };

            _stack.Controls.AddRange(new Control[] { _icon, _title, _body, _buttons });
            Controls.Add(_stack);
            Resize += (s, e) => CenterStack();
            _stack.SizeChanged += (s, e) => CenterStack();
            ApplyTheme();
        }

        public void ShowState(IconChar icon, string title, string body, params (string Text, Action OnClick)[] actions)
        {
            _icon.IconChar = icon;
            _title.Text = title;
            _body.Text = body;

            foreach (Control c in _buttons.Controls.Cast<Control>().ToList()) c.Dispose();
            _buttons.Controls.Clear();

            bool first = true;
            foreach (var (text, onClick) in actions)
            {
                var btn = first
                    ? ToolUiFactory.AccentButton(text, IconChar.None, themeManager.CurrentTheme.AccentColor)
                    : ToolUiFactory.GhostButton(text, IconChar.None);
                btn.AutoSize = true;
                btn.Margin = new Padding(Scale(6), 0, Scale(6), 0);
                btn.Click += (s, e) => onClick();
                _buttons.Controls.Add(btn);
                first = false;
            }

            ApplyTheme();
            CenterStack();
        }

        public void ApplyTheme()
        {
            var t = themeManager.CurrentTheme;
            BackColor = t.CardBackground;
            _icon.IconColor = t.AccentColor;
            _title.ForeColor = t.HeadingColor;
            _body.ForeColor = t.SecondaryTextColor;
            bool first = true;
            foreach (var btn in _buttons.Controls.OfType<IconButton>())
            {
                if (first) ToolUiFactory.ApplyAccentTheme(btn, t.AccentColor);
                else ToolUiFactory.ApplyGhostTheme(btn);
                first = false;
            }
        }

        private void CenterStack()
        {
            _stack.Location = new Point(
                Math.Max(0, (ClientSize.Width - _stack.Width) / 2),
                Math.Max(0, (ClientSize.Height - _stack.Height) / 2));
        }
    }
}
