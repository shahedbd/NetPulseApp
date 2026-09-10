// UserControls/Pages/Ping/PingInputPanel.cs
using NetPulseApp.Managers;

namespace NetPulseApp.UserControls.Pages.Ping
{
    /// <summary>
    /// "Target" card: hostname box, Count / PacketSize / Timeout spinners,
    /// option checkboxes, and the Start/Stop button.
    /// Raises events consumed by PingControl.
    /// </summary>
    public sealed class PingInputPanel : Panel
    {
        // ── Public events ─────────────────────────────────────────────────────
        public event EventHandler? StartRequested;
        public event EventHandler? StopRequested;

        // ── Public properties (read by PingControl) ───────────────────────────
        public string TargetHost => _txtHost.Text.Trim();
        public int PingCount => _chkContinuous.Checked ? -1 : (int)_nudCount.Value;
        public int PacketBytes => (int)_nudPacket.Value;
        public int TimeoutMs => (int)_nudTimeout.Value;
        public bool ResolveHostname => _chkResolve.Checked;
        public bool UseIPv6 => _chkIPv6.Checked;

        // ── Controls ──────────────────────────────────────────────────────────
        private readonly TextBox _txtHost;
        private readonly NumericUpDown _nudCount;
        private readonly NumericUpDown _nudPacket;
        private readonly NumericUpDown _nudTimeout;
        private readonly CheckBox _chkResolve;
        private readonly CheckBox _chkIPv6;
        private readonly CheckBox _chkContinuous;
        private readonly Button _btnStart;
        private readonly ThemeManager _theme = ThemeManager.Instance;

        public PingInputPanel()
        {
            _txtHost = new TextBox();
            _nudCount = CreateSpinner(4, 1, 100);
            _nudPacket = CreateSpinner(32, 1, 65500);
            _nudTimeout = CreateSpinner(4000, 500, 30000);
            _chkResolve = new CheckBox { Text = "Resolve hostname to IP", Checked = true };
            _chkIPv6 = new CheckBox { Text = "Use IPv6 (if available)" };
            _chkContinuous = new CheckBox { Text = "Continuous ping" };
            _btnStart = new Button { Text = "▶  Start Ping" };

            BuildLayout();
            ApplyTheme();
        }

        // ── Layout ────────────────────────────────────────────────────────────

        private void BuildLayout()
        {
            AutoSize = true;
            Padding = new Padding(16);

            // ── Section label ─────────────────────────────────────────────
            var lblSection = MakeLabel("☰  Target", bold: true, size: 10f);
            lblSection.Location = new Point(16, 12);

            // ── Host row ──────────────────────────────────────────────────
            var lblHost = MakeLabel("Hostname or IP address:");
            lblHost.Location = new Point(16, 40);

            _txtHost.Text = "google.com";
            _txtHost.Size = new Size(340, 26);
            _txtHost.Location = new Point(16, 60);
            _txtHost.Font = new Font("Segoe UI", 10f);

            var lblHint = MakeLabel("Examples: google.com, 8.8.8.8, microsoft.com", size: 8.5f);
            lblHint.Location = new Point(18, 90);

            // ── Spinners row ──────────────────────────────────────────────
            var lblCount = MakeLabel("Count:"); lblCount.Location = new Point(380, 40);
            var lblPacket = MakeLabel("Packet size (bytes):"); lblPacket.Location = new Point(460, 40);
            var lblTimeout = MakeLabel("Timeout (ms):"); lblTimeout.Location = new Point(620, 40);

            _nudCount.Location = new Point(380, 60); _nudCount.Width = 70;
            _nudPacket.Location = new Point(460, 60); _nudPacket.Width = 80;
            _nudTimeout.Location = new Point(620, 60); _nudTimeout.Width = 90;

            // ── Checkboxes + button row ───────────────────────────────────
            _chkResolve.Location = new Point(16, 110); _chkResolve.AutoSize = true;
            _chkIPv6.Location = new Point(200, 110); _chkIPv6.AutoSize = true;
            _chkContinuous.Location = new Point(380, 110); _chkContinuous.AutoSize = true;

            _btnStart.Size = new Size(140, 36);
            _btnStart.Location = new Point(620, 100);
            _btnStart.FlatStyle = FlatStyle.Flat;
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _btnStart.Cursor = Cursors.Hand;
            _btnStart.Click += (s, e) => StartRequested?.Invoke(this, EventArgs.Empty);

            Controls.AddRange(new Control[]
            {
                lblSection, lblHost, _txtHost, lblHint,
                lblCount, lblPacket, lblTimeout,
                _nudCount, _nudPacket, _nudTimeout,
                _chkResolve, _chkIPv6, _chkContinuous, _btnStart
            });

            Height = 150;
        }

        // ── Public: toggle running state ──────────────────────────────────────
        public void SetRunning(bool running)
        {
            _btnStart.Text = running ? "⏹  Stop Ping" : "▶  Start Ping";
            _txtHost.Enabled = !running;

            if (running)
                _btnStart.Click -= OnStart;
            else
                _btnStart.Click -= OnStop;

            if (running) _btnStart.Click += OnStop;
            else _btnStart.Click += OnStart;
        }

        private void OnStart(object? s, EventArgs e) => StartRequested?.Invoke(this, e);
        private void OnStop(object? s, EventArgs e) => StopRequested?.Invoke(this, e);

        // ── Theme ─────────────────────────────────────────────────────────────
        public void ApplyTheme()
        {
            var t = _theme.CurrentTheme;
            BackColor = t.CardBackground;
            _txtHost.BackColor = t.ContentBackground;
            _txtHost.ForeColor = t.TextColor;
            _btnStart.BackColor = Color.FromArgb(37, 99, 235);
            _btnStart.ForeColor = Color.White;

            foreach (Control c in Controls)
            {
                c.ForeColor = t.TextColor;
                if (c is CheckBox cb) { cb.BackColor = Color.Transparent; }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Label MakeLabel(string text, bool bold = false, float size = 9f) =>
            new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular)
            };

        // FIXED — Maximum first, then Value
        private static NumericUpDown CreateSpinner(int val, int min, int max)
        {
            var nud = new NumericUpDown
            {
                Font = new Font("Segoe UI", 10f),
                Minimum = min,
                Maximum = max   // ✅ Maximum set before Value
            };
            nud.Value = val;    // ✅ now safe — 4000 is within [500, 30000]
            return nud;
        }
    }
}
