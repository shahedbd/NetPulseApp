using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;
using NetPulseApp.UserControls.Pages.Settings;

namespace NetPulseApp.UserControls.Pages
{
    public partial class SettingsPageControl
    {
        private TextBox _txtSearch;
        private Panel _host;
        private Panel _nav;
        private Panel _navDivider;
        private Button _btnReset;
        private readonly List<Label> _navLinks = new();
        private readonly System.Windows.Forms.Timer _searchTimer = new() { Interval = 150 };
        private bool _suppressSearch;
        private bool _searchTimerWired;

        /// <summary>
        /// Page header on top, category column (search + group headings +
        /// links) on the left, a footer strip under the content, and the
        /// scrolling host filling the rest. Docking order matters: the last
        /// control added docks first, so the header spans the full width, the
        /// column the full height below it, and the footer only the content side.
        /// </summary>
        private void BuildChrome()
        {
            var theme = themeManager.CurrentTheme;

            _host = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = theme.ContentBackground };
            _host.Resize += (s, e) => SizeContent();
            Controls.Add(_host);

            Controls.Add(BuildFooter());
            _navDivider = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = theme.BorderColor };
            Controls.Add(_navDivider);
            _nav = BuildNavColumn();
            Controls.Add(_nav);
            _compactBar = BuildCompactBar();
            Controls.Add(_compactBar);
            Controls.Add(BuildHeader());

            if (!_searchTimerWired)
            {
                _searchTimerWired = true;
                _searchTimer.Tick += (s, e) =>
                {
                    _searchTimer.Stop();
                    RunSearch();
                };
            }

            _narrow = null;
            ApplyNarrowMode();
        }

        /// <summary>Disposes the chrome so <see cref="BuildChrome"/> can run again with new theme colors.</summary>
        private void ClearChrome()
        {
            _searchTimer.Stop();
            var old = Controls.Cast<Control>().ToList();
            Controls.Clear();
            foreach (var c in old) c.Dispose();
            _navLinks.Clear();
        }

        private Panel BuildHeader()
        {
            var theme = themeManager.CurrentTheme;
            var panel = new Panel { Dock = DockStyle.Top, Height = Scale(56), BackColor = Color.Transparent };
            var title = ToolUiFactory.PageTitle("Settings");
            title.Location = new Point(0, Scale(2));
            title.ForeColor = theme.HeadingColor;
            var subtitle = ToolUiFactory.PageSubtitle("Manage app preferences and tool behavior.");
            subtitle.Location = new Point(Scale(2), Scale(40)); // below the 20pt title's descenders
            subtitle.ForeColor = theme.SecondaryTextColor;
            panel.Controls.Add(subtitle);
            panel.Controls.Add(title);
            return panel;
        }

        private TextBox BuildSearchBox(int width)
        {
            var theme = themeManager.CurrentTheme;
            _txtSearch = new TextBox
            {
                PlaceholderText = "Search settings  (Ctrl+F)",
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = theme.IsDark ? Color.FromArgb(45, 45, 45) : Color.White,
                ForeColor = theme.TextColor,
                Width = width,
                AccessibleName = "Search settings"
            };
            _txtSearch.TextChanged += (s, e) =>
            {
                if (_suppressSearch) return;
                _searchTimer.Stop();
                _searchTimer.Start();
            };
            return _txtSearch;
        }

        private Panel BuildNavColumn()
        {
            var nav = new Panel
            {
                Dock = DockStyle.Left,
                Width = Scale(190),
                BackColor = themeManager.CurrentTheme.ContentBackground,
                Padding = ScaledPadding(0, 8, 12, 12)
            };

            var search = BuildSearchBox(nav.Width - nav.Padding.Horizontal);
            search.Location = new Point(nav.Padding.Left, nav.Padding.Top);
            search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            nav.Controls.Add(search);

            int y = search.Bottom + Scale(10);
            for (int i = 0; i < CategoryNames.Length; i++)
            {
                if (GroupHeadings.TryGetValue(i, out var heading))
                {
                    var group = MakeGroupHeading(heading, nav, y);
                    nav.Controls.Add(group);
                    y = group.Bottom + Scale(2);
                }

                var link = MakeNavLink(i, nav, y);
                _navLinks.Add(link);
                nav.Controls.Add(link);
                y += link.Height + Scale(2);
            }
            return nav;
        }

        /// <summary>"APP" / "TOOLS" / "DATA" — small caps above a group of links.</summary>
        private Label MakeGroupHeading(string text, Panel nav, int y) => new()
        {
            Text = text,
            UseMnemonic = false, // "&" is literal text, not an access key
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = themeManager.CurrentTheme.SecondaryTextColor,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = ScaledPadding(4, 0, 0, 2),
            Location = new Point(nav.Padding.Left, y),
            Size = new Size(nav.Width - nav.Padding.Horizontal, Scale(28)),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        private Label MakeNavLink(int index, Panel nav, int y)
        {
            var theme = themeManager.CurrentTheme;
            var link = new Label
            {
                Text = CategoryNames[index],
                UseMnemonic = false, // "&" is literal text, not an access key
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = theme.TextColor,
                BackColor = theme.ContentBackground,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = ScaledPadding(14, 0, 0, 0),
                Location = new Point(nav.Padding.Left, y),
                Size = new Size(nav.Width - nav.Padding.Horizontal, Scale(34)),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                AccessibleRole = AccessibleRole.Link,
                AccessibleName = CategoryNames[index]
            };
            link.Click += (s, e) => OpenCategoryFromNav(index);
            link.Paint += (s, e) => PaintNavLink(link, e);
            link.MouseEnter += (s, e) => link.BackColor = theme.HoverColor;
            link.MouseLeave += (s, e) => link.BackColor = IsActiveLink(link) ? theme.HoverColor : theme.ContentBackground;
            return link;
        }

        private Panel BuildFooter()
        {
            var theme = themeManager.CurrentTheme;
            var footer = new Panel { Dock = DockStyle.Bottom, Height = Scale(52), BackColor = theme.ContentBackground };
            footer.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = theme.BorderColor });

            footer.Controls.Add(new Label
            {
                Text = "Changes are saved instantly.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = theme.SecondaryTextColor,
                AutoSize = true,
                Location = ScaledPoint(24, 18),
                BackColor = Color.Transparent
            });

            _btnReset = new Button
            {
                Text = "Reset page",
                Font = new Font("Segoe UI", 9f),
                Size = ScaledSize(110, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            SettingsStyle.ThemeGhostButton(_btnReset);
            _btnReset.Click += (s, e) => ResetCurrentPage();
            footer.Controls.Add(_btnReset);
            footer.Resize += (s, e) => _btnReset.Location = new Point(footer.Width - _btnReset.Width - Scale(8), Scale(12));
            return footer;
        }

        // ── Nav state ─────────────────────────────────────────────────────────

        private void OpenCategoryFromNav(int index)
        {
            if (_txtSearch.TextLength > 0) ClearSearchSilently();
            ShowCategory(index);
        }

        /// <summary>-1 = none (search results are showing).</summary>
        private void SetActiveNav(int index)
        {
            var theme = themeManager.CurrentTheme;
            for (int i = 0; i < _navLinks.Count; i++)
            {
                bool active = i == index;
                _navLinks[i].BackColor = active ? theme.HoverColor : theme.ContentBackground;
                _navLinks[i].Font = new Font("Segoe UI", 9.5f, active ? FontStyle.Bold : FontStyle.Regular);
                _navLinks[i].Invalidate();
            }

            if (index >= 0 && _cmbCategory != null && _cmbCategory.SelectedIndex != index)
            {
                _suppressSearch = true;
                _cmbCategory.SelectedIndex = index;
                _suppressSearch = false;
            }
        }

        private bool IsActiveLink(Label link) =>
            _shown is not SettingsSearchResults && _navLinks.IndexOf(link) == _current;

        /// <summary>The accent bar on the active link — the design's "▌ Appearance".</summary>
        private void PaintNavLink(Label link, PaintEventArgs e)
        {
            if (!IsActiveLink(link)) return;
            using var bar = new SolidBrush(themeManager.CurrentTheme.AccentColor);
            e.Graphics.FillRectangle(bar, 0, Scale(8), Scale(3), link.Height - Scale(16));
        }

        // ── Sizing ────────────────────────────────────────────────────────────

        /// <summary>
        /// Width for the content: the host's client width minus a scrollbar's
        /// worth even when none shows, so a vertical scrollbar appearing never
        /// forces a horizontal one.
        /// </summary>
        private void SizeContent()
        {
            if (_host == null || _shown == null) return;
            int width = _host.ClientSize.Width - (_host.VerticalScroll.Visible ? 0 : SystemInformation.VerticalScrollBarWidth);
            if (width < Scale(200)) return;

            if (_shown is SettingsCategory category) category.SetContentWidth(width);
            else if (_shown is SettingsSearchResults results) results.SetContentWidth(width);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                themeManager.ThemeChanged -= OnThemeChanged;
                _searchTimer.Dispose();
                // Cached categories that aren't on screen aren't in the control
                // tree, so base.Dispose wouldn't reach them.
                foreach (var category in _categories)
                    if (category != null && category != _shown) category.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
