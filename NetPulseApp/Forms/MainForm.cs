using NetPulseApp.Helper;
using NetPulseApp.Managers;
using NetPulseApp.Service;
using NetPulseApp.Service.Dpi;
using NetPulseApp.UserControls.Layout;
using DeviceDataModule;

namespace NetPulseApp.Forms
{
    /// <summary>
    /// Application shell. Hosts TitleBarControl, HeaderControl, SidebarControl
    /// and the content panel. Structural only — nothing here should need to
    /// change when bootstrapping a new app; edit AppConfig.cs and
    /// UserControls/Pages/* instead.
    /// </summary>
    public partial class MainForm : Form
    {
        private Panel contentPanel;
        private Panel mainAreaPanel;
        private TitleBarControl _titleBar;
        private HeaderControl _header;
        private StatusBarControl _statusBar;
        private SidebarControl _sidebar;
        private NavigationManager _navigationManager;
        private ThemeManager themeManager;
        private SystemTrayService _trayService;

        public MainForm()
        {
            this.ApplyDpiScaling(1250, 750, 1000, 600);

            InitializeComponent();
            InitializeManagers();

            Load += MainForm_Load;
            Shown += MainForm_Shown;
            DpiChanged += MainForm_DpiChanged;
        }

        // ─────────────────────────────────────────────────────────────────────
        // INIT
        // ─────────────────────────────────────────────────────────────────────

        private void InitializeComponent()
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            Icon = File.Exists(AppConfig.FaviconPath) ? new Icon(AppConfig.FaviconPath) : null;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = AppConfig.AppName;

            ResumeLayout(false);
        }

        private void InitializeManagers()
        {
            themeManager = ThemeManager.Instance;
            themeManager.ThemeChanged += (s, e) => ApplyTheme();

            _navigationManager = new NavigationManager();

            if (AppConfig.EnableSystemTray)
            {
                _trayService = new SystemTrayService();
                _trayService.ShowAppRequested += (s, e) => RestoreFromTray();
                _trayService.AboutRequested += (s, e) => new AboutForm().ShowDialog();
                _trayService.ExitRequested += (s, e) => Application.Exit();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            SuspendLayout();
            try
            {
                // Per Doc/UI layout.md, the sidebar runs the full height
                // below the title bar (both beside the header AND the
                // content) — the header only starts to the right of the
                // sidebar. So Header + Content are nested inside a dedicated
                // "main area" panel that sits beside the sidebar, rather
                // than being direct siblings of it.
                //
                // Add order matters for docking: content (Fill) first, then
                // Header (Top) — both inside mainAreaPanel — then
                // mainAreaPanel (Fill), Sidebar (Left), TitleBar (Top) at the
                // form level. Each later addition claims its slice of the
                // full remaining area first, so TitleBar ends up
                // outermost/topmost, Sidebar spans the full height below it,
                // and mainAreaPanel fills whatever's left beside the sidebar.
                InitializeMainArea();
                InitializeSidebar();
                InitializeTitleBar();

                BeginInvoke(new Action(ApplyTheme));
                _ = Task.Run(() => StartupService.ExecuteStartupTaskAsync());
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            // Header owns its summary load — HeaderControl.SystemSummary.cs.
            _header?.LoadSystemSummary();
            _statusBar?.Start();
        }

        private void MainForm_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            SuspendLayout();
            try
            {
                _titleBar?.RefreshForDpi();
                _header?.RefreshForDpi();
                _statusBar?.RefreshForDpi();
                _sidebar?.RefreshForDpi();
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void InitializeMainArea()
        {
            mainAreaPanel = new Panel { Dock = DockStyle.Fill };

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = themeManager.CurrentTheme.ContentBackground,
                Padding = DpiAwareService.ScaledPadding(10)
            };

            _header = new HeaderControl();
            _statusBar = new StatusBarControl();

            // Content (Fill) added first, then StatusBar (Bottom) and Header
            // (Top) so they claim their strips and content fills the rest.
            mainAreaPanel.Controls.Add(contentPanel);
            mainAreaPanel.Controls.Add(_statusBar);
            mainAreaPanel.Controls.Add(_header);

            Controls.Add(mainAreaPanel);
        }

        private void InitializeSidebar()
        {
            _sidebar = new SidebarControl();
            Controls.Add(_sidebar);

            _navigationManager.BuildSidebar(_sidebar, contentPanel);
        }

        private void InitializeTitleBar()
        {
            _titleBar = new TitleBarControl();
            Controls.Add(_titleBar);
        }

        // ─────────────────────────────────────────────────────────────────────
        // THEME
        // ─────────────────────────────────────────────────────────────────────

        private void ApplyTheme()
        {
            _titleBar?.ApplyTheme();
            _header?.ApplyTheme();
            _statusBar?.ApplyTheme();
            _sidebar?.ApplyTheme();

            if (contentPanel != null)
                contentPanel.BackColor = themeManager.CurrentTheme.ContentBackground;

            Invalidate(true);
        }

        // ─────────────────────────────────────────────────────────────────────
        // SYSTEM TRAY
        // ─────────────────────────────────────────────────────────────────────

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            _trayService?.Hide();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Only the title bar's close button / Alt+F4 (CloseReason.UserClosing)
            // gets redirected to the tray — Application.Exit() (used by the
            // tray's own "Exit" item) reports a different reason and closes
            // for real. EnableSystemTray gates whether the tray icon exists
            // at all (compile-time); MinimizeToTrayOnClose is the user's own
            // runtime preference for whether closing goes there.
            if (AppConfig.EnableSystemTray && SettingsService.Current.MinimizeToTrayOnClose
                && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _trayService?.Show();
                _trayService?.ShowBalloonTip(
                    AppConfig.AppName,
                    "App is still running in the system tray.\nDouble-click to restore.",
                    2000,
                    ToolTipIcon.Info);
                return;
            }

            _trayService?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
