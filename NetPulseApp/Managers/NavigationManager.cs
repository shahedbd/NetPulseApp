using NetPulseApp.Helper;
using NetPulseApp.UserControls.Layout;

namespace NetPulseApp.Managers
{
    /// <summary>
    /// Owns sidebar nav-button rendering (from AppConfig.NavItems), the sidebar
    /// expand/collapse toggle, and content-panel page switching.
    /// </summary>
    public class NavigationManager
    {
        private UserControl _currentControl;

        // ─────────────────────────────────────────────────────────────────────
        // SIDEBAR SETUP
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds one nav button per AppConfig.NavItems entry, wires each to
        /// either load its ControlType into contentPanel or invoke its
        /// ShowModal action (e.g. an About dialog), and wires the sidebar's
        /// "..." button to toggle collapse/expand. Call once during form load.
        /// </summary>
        public void BuildSidebar(SidebarControl sidebar, Panel contentPanel)
        {
            Button firstPageButton = null;

            foreach (var item in AppConfig.NavItems)
            {
                var button = sidebar.AddNavButton(item);
                button.Click += (s, e) =>
                {
                    if (item.ShowModal != null)
                    {
                        // A dialog, not a page — don't touch content or the
                        // active-page highlight.
                        item.ShowModal();
                        return;
                    }

                    var control = (UserControl)Activator.CreateInstance(item.ControlType);
                    ShowPage(control, contentPanel);
                    sidebar.SetActiveButton(button);
                };

                if (item.ShowModal == null)
                    firstPageButton ??= button;
            }

            sidebar.BtnToggle.Click += (s, e) => sidebar.ToggleCollapse();

            // Land on the first actual page on startup — never auto-open a
            // modal item even if one happens to be first in the list.
            firstPageButton?.PerformClick();
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONTENT SWITCHING
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Swaps the content panel's page. The sidebar is a flat, single-level
        /// nav (no back stack), so the outgoing control is disposed rather
        /// than retained — otherwise every nav click would leak a control.
        /// </summary>
        public void ShowPage(UserControl control, Panel containerPanel)
        {
            if (_currentControl != null)
            {
                containerPanel.Controls.Remove(_currentControl);
                _currentControl.Dispose();
            }

            _currentControl = control;
            control.Dock = DockStyle.Fill;
            containerPanel.Controls.Add(control);
        }
    }
}
