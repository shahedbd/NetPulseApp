// UserControls/Pages/SettingsPageControl.cs
using NetPulseApp.UserControls.Lib;
using NetPulseApp.UserControls.Pages.Settings;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Settings tool page — its own category column (APP / TOOLS / DATA group
    /// headings), a search box and a scrolling content pane, following
    /// NetSpeedMeterPlus's Settings (doc/MVP_Release_plan/Settings_UI_Redesign_Plan.md).
    /// Every change saves at once, so there is no Apply button.
    /// </summary>
    /// <remarks>
    /// This file owns state and navigation between categories and search;
    /// <c>Settings/SettingsPageControl.Layout.cs</c> builds the chrome.
    /// Categories are built the first time they're opened; typing in search
    /// builds the rest, since search spans every category. A theme switch
    /// rebuilds the page's controls in place (see <see cref="OnThemeChanged"/>).
    /// Requires a public parameterless constructor (NavigationManager creates
    /// pages via Activator.CreateInstance).
    /// </remarks>
    public partial class SettingsPageControl : DpiAwareUserControl
    {
        /// <summary>
        /// Survives the page being rebuilt or reopened — picking Light on
        /// Appearance must leave the user on Appearance.
        /// </summary>
        private static class LastState
        {
            public static int Category;
            public static string Search = "";
        }

        private static readonly string[] CategoryNames =
            { "Appearance", "Behavior", "Network & IP", "Speed Test", "Privacy & Data", "Reset" };

        /// <summary>Group heading shown above the category at that index.</summary>
        private static readonly Dictionary<int, string> GroupHeadings = new()
        {
            [0] = "APP",
            [2] = "TOOLS",
            [4] = "DATA"
        };

        private readonly Func<SettingsCategory>[] _factories;
        private readonly SettingsCategory[] _categories;
        private Control _shown;
        private int _current = -1;

        public SettingsPageControl()
        {
            _factories = new Func<SettingsCategory>[]
            {
                () => new AppearanceSettings(),
                () => new BehaviorSettings(),
                () => new NetworkSettings(),
                () => new SpeedTestSettings(),
                () => new PrivacySettings(),
                () => new ResetSettings(ResetAll),
            };
            _categories = new SettingsCategory[_factories.Length];

            Padding = ScaledPadding(16, 0, 16, 0); // no top inset — header sits flush under the shell header
            BuildPage();

            themeManager.ThemeChanged += OnThemeChanged;
        }

        /// <summary>Builds the chrome and reopens the last category or search.</summary>
        private void BuildPage()
        {
            BackColor = themeManager.CurrentTheme.ContentBackground;
            BuildChrome();

            if (LastState.Search.Length > 0)
                _txtSearch.Text = LastState.Search; // TextChanged runs the search
            else
                ShowCategory(Math.Clamp(LastState.Category, 0, _factories.Length - 1));
        }

        // ── Categories ────────────────────────────────────────────────────────

        private SettingsCategory GetCategory(int index)
        {
            if (_categories[index] is { IsDisposed: false } built) return built;

            var category = _factories[index]();
            category.Build();
            return _categories[index] = category;
        }

        private void ShowCategory(int index, Control scrollTo = null)
        {
            _current = index;
            LastState.Category = index;
            SetActiveNav(index);

            var category = GetCategory(index);
            ShowContent(category);
            _btnReset.Visible = category.CanReset;

            if (scrollTo != null && !scrollTo.IsDisposed)
            {
                _host.ScrollControlIntoView(scrollTo);
                Flash(scrollTo);
            }
        }

        /// <summary>Swaps the pane's content. Categories are kept (cached); search results are thrown away.</summary>
        private void ShowContent(Control content)
        {
            _host.SuspendLayout();
            if (_shown != null && _shown != content)
            {
                _host.Controls.Remove(_shown);
                if (_shown is SettingsSearchResults) _shown.Dispose();
            }

            _shown = content;
            content.Location = Point.Empty;
            if (content.Parent != _host) _host.Controls.Add(content);
            SizeContent();
            _host.ResumeLayout(true);
            _host.AutoScrollPosition = Point.Empty;
        }

        /// <summary>Drops every cached category so the next open rebuilds it from saved values.</summary>
        private void DisposeCategories()
        {
            for (int i = 0; i < _categories.Length; i++)
            {
                var old = _categories[i];
                _categories[i] = null;
                if (old == null) continue;
                if (_shown == old) { _host.Controls.Remove(old); _shown = null; }
                old.Dispose();
            }
        }

        private void ResetCurrentPage()
        {
            int index = _current;
            var category = GetCategory(index);

            // Deferred: resetting Appearance switches the theme, which rebuilds
            // this page — not something to do inside the button's Click.
            BeginInvoke(() =>
            {
                category.ResetToDefaults();
                if (IsDisposed) return;
                DisposeCategories();
                ShowCategory(index);
            });
        }

        /// <summary>Reset category's "Reset all…": every resettable category back to defaults.</summary>
        private void ResetAll()
        {
            BeginInvoke(() =>
            {
                for (int i = 0; i < _factories.Length; i++)
                {
                    var category = GetCategory(i);
                    if (category.CanReset) category.ResetToDefaults();
                }
                if (IsDisposed) return;
                DisposeCategories();
                ShowCategory(_current);
            });
        }

        // ── Search ────────────────────────────────────────────────────────────

        private void RunSearch()
        {
            string query = _txtSearch.Text.Trim();
            LastState.Search = query;

            if (query.Length == 0)
            {
                ShowCategory(LastState.Category);
                return;
            }

            var entries = Enumerable.Range(0, _factories.Length).SelectMany(i => GetCategory(i).SearchEntries);
            var results = new SettingsSearchResults(query, entries,
                entry => BeginInvoke(() => OpenResult(entry)));

            SetActiveNav(-1);
            _btnReset.Visible = false;
            ShowContent(results);
        }

        private void OpenResult(SettingsCategory.SearchEntry entry)
        {
            if (IsDisposed) return;
            int index = Array.IndexOf(_categories, entry.Category);
            if (index < 0) return;

            // Clearing the box without re-running the search: we're leaving it on purpose.
            ClearSearchSilently();
            ShowCategory(index, entry.Target);
        }

        private void ClearSearchSilently()
        {
            _searchTimer.Stop();
            _suppressSearch = true;
            _txtSearch.Text = "";
            _suppressSearch = false;
            LastState.Search = "";
        }

        /// <summary>Ctrl+F anywhere on the page focuses search; Esc in the box clears it.</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                _txtSearch.Focus();
                _txtSearch.SelectAll();
                return true;
            }
            if (keyData == Keys.Escape && _txtSearch.Focused && _txtSearch.TextLength > 0)
            {
                _txtSearch.Clear();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ── Highlight ─────────────────────────────────────────────────────────

        /// <summary>A one-second tint so the eye finds the row a search result jumped to.</summary>
        private void Flash(Control target)
        {
            var normal = target.BackColor;
            var tint = themeManager.CurrentTheme.HoverColor;
            var tinted = new List<Control> { target };
            tinted.AddRange(target.Controls.Cast<Control>().Where(c => c is Label));

            foreach (var c in tinted) c.BackColor = tint;

            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (s, e) =>
            {
                timer.Dispose();
                foreach (var c in tinted.Where(c => !c.IsDisposed)) c.BackColor = normal;
            };
            timer.Start();
        }

        // ── Theme ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Colors are read at build time, so a theme switch (from Appearance or
        /// the header button) rebuilds the page's controls in place. Deferred:
        /// the switch may come from a click inside a control being disposed.
        /// </summary>
        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke(() =>
            {
                if (IsDisposed) return;
                SuspendLayout();
                DisposeCategories();
                ClearChrome();
                BuildPage();
                ResumeLayout(true);
            });
        }
    }
}
