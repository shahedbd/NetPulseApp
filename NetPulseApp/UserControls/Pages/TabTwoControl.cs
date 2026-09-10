// UserControls/Pages/TabTwoControl.cs
using NetPulseApp.Managers;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Starting-point placeholder for the Tab Two nav item.
    /// Replace this content with the real page for your app.
    /// </summary>
    public class TabTwoControl : UserControl
    {
        public TabTwoControl()
        {
            var themeManager = ThemeManager.Instance;
            BackColor = themeManager.CurrentTheme.ContentBackground;

            Controls.Add(new Label
            {
                Text = "Your tab two",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = themeManager.CurrentTheme.HeadingColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(24, 24)
            });
        }
    }
}
