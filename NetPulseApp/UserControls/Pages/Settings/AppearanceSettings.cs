using NetPulseApp.Managers;

namespace NetPulseApp.UserControls.Pages.Settings
{
    /// <summary>
    /// Appearance — the app window's Dark / Light theme, as two preview cards.
    /// Same setting as the header's theme button.
    /// </summary>
    internal sealed class AppearanceSettings : SettingsCategory
    {
        public override string Title => "Appearance";
        public override string Subtitle => "How the NetPulse window looks";

        private readonly List<ThemePreviewCard> _themeCards = new();

        protected override void BuildContent()
        {
            AddSection("Theme");
            var card = AddCard();

            var strip = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.CardBackground,
                Padding = new Padding(0, S(6), 0, S(4))
            };

            foreach (var (isDark, caption) in new[] { (true, "Dark"), (false, "Light") })
            {
                var preview = new ThemePreviewCard(isDark, caption)
                {
                    Selected = ThemeManager.Instance.IsDarkTheme == isDark
                };
                preview.Click += (s, e) => Select(isDark);
                _themeCards.Add(preview);
                strip.Controls.Add(preview);
            }

            card.AddFull(strip);
            card.AddDivider();
            card.AddFull(MakeNote("Same as the theme button in the header."));

            Register(strip, "Theme", "Dark or Light app window",
                "dark", "light", "mode", "night", "appearance");
        }

        public override void ResetToDefaults() => ThemeManager.Instance.SetTheme(true);

        /// <summary>
        /// Applies immediately; the page rebuilds itself (deferred) on
        /// ThemeChanged and reopens on this category.
        /// </summary>
        private void Select(bool isDark)
        {
            foreach (var c in _themeCards) c.Selected = c.IsDark == isDark;
            ThemeManager.Instance.SetTheme(isDark);
        }
    }
}
