using NetPulseApp.Helper;
using NetPulseApp.Managers;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages.Settings
{
    /// <summary>
    /// Behavior — close-to-tray and the sidebar's collapsed state. The sidebar
    /// row drives SidebarControl.SetCollapsed (which also saves), so it and the
    /// sidebar's own toggle button can never disagree.
    /// </summary>
    internal sealed class BehaviorSettings : SettingsCategory
    {
        public override string Title => "Behavior";
        public override string Subtitle => "What the app does when you close it, and how it's laid out";

        private ToggleSwitchControl _tglTray;
        private ToggleSwitchControl _tglSidebar;

        protected override void BuildContent()
        {
            AddSection("Window");
            var window = AddCard();

            _tglTray = new ToggleSwitchControl();
            _tglTray.CheckedChanged += (s, e) => Commit(c => c.MinimizeToTrayOnClose = _tglTray.Checked);
            var trayRow = AddRow(window, "Minimize to tray when closing", _tglTray,
                "Keep running in the system tray after the window closes.",
                "tray", "close", "background", "minimize", "exit");
            if (!AppConfig.EnableSystemTray)
                trayRow.SetEnabled(false);
            window.AddDivider();

            _tglSidebar = new ToggleSwitchControl();
            _tglSidebar.CheckedChanged += (s, e) =>
            {
                if (IsLoading) return;
                SetSidebarCollapsed(_tglSidebar.Checked);
            };
            AddRow(window, "Collapse sidebar", _tglSidebar,
                "Show only icons in the left navigation. Remembered next time.",
                "sidebar", "navigation", "menu", "icons", "compact");

            LoadValues();
        }

        public override void ResetToDefaults()
        {
            Current.MinimizeToTrayOnClose = true;
            SetSidebarCollapsed(false); // saves settings too
        }

        private void LoadValues()
        {
            IsLoading = true;
            try
            {
                _tglTray.Checked = Current.MinimizeToTrayOnClose;
                _tglSidebar.Checked = Current.IsSidebarCollapsed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>Live when the shell's sidebar exists; otherwise just saved for next launch.</summary>
        private static void SetSidebarCollapsed(bool collapsed)
        {
            var sidebar = NavigationManager.Current?.Sidebar;
            if (sidebar is { IsDisposed: false })
            {
                sidebar.SetCollapsed(collapsed);
                return;
            }
            Current.IsSidebarCollapsed = collapsed;
            DeviceDataModule.SettingsService.Save();
        }
    }
}
