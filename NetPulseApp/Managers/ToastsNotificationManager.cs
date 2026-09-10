using NetPulseApp.Service.Icons;
using Timer = System.Windows.Forms.Timer;

namespace NetPulseApp.Managers
{
    public class ToastsNotificationManager
    {
        private static ToastsNotificationManager instance;
        public static ToastsNotificationManager Instance => instance ?? (instance = new ToastsNotificationManager());

        // ✅ DPI Scaling helpers
        private float scaleFactor;

        private int Scale(int value) => (int)(value * scaleFactor);
        private Size ScaledSize(int w, int h) => new Size(Scale(w), Scale(h));
        private Point ScaledPoint(int x, int y) => new Point(Scale(x), Scale(y));

        private ToastsNotificationManager()
        {
            // ✅ Calculate DPI scale factor
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                scaleFactor = g.DpiX / 96f;
            }
        }

        public void ShowSuccessNotification(string message)
        {
            ShowCustomNotification("Success", message, Color.FromArgb(34, 197, 94), IconChar.CheckCircle);
        }

        public void ShowErrorNotification(string message)
        {
            ShowCustomNotification("Error", message, Color.FromArgb(239, 68, 68), IconChar.TimesCircle);
        }

        public void ShowInfoNotification(string message)
        {
            ShowCustomNotification("Info", message, Color.FromArgb(59, 130, 246), IconChar.InfoCircle);
        }

        private void ShowCustomNotification(string title, string message, Color color, IconChar icon)
        {
            Form toast = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Size = ScaledSize(350, 80),
                BackColor = color,
                ShowInTaskbar = false,
                TopMost = true,
                Opacity = 0.95
            };

            var screen = Screen.PrimaryScreen;
            toast.Location = new Point(
                screen.WorkingArea.Right - toast.Width - Scale(20),
                screen.WorkingArea.Bottom - toast.Height - Scale(20)
            );

            IconPictureBox iconBox = new IconPictureBox
            {
                IconChar = icon,
                IconColor = Color.White,
                IconSize = Scale(32),
                Size = ScaledSize(40, 40),
                Location = ScaledPoint(15, 20),
                BackColor = Color.Transparent
            };
            toast.Controls.Add(iconBox);

            // ✅ FIXED font sizes
            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = ScaledPoint(65, 15),
                AutoSize = true
            };
            toast.Controls.Add(lblTitle);

            Label lblMessage = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = ScaledPoint(65, 40),
                Size = ScaledSize(270, 30),
                AutoEllipsis = true
            };
            toast.Controls.Add(lblMessage);

            // ✅ Close button
            IconButton btnClose = new IconButton
            {
                IconChar = IconChar.Times,
                IconColor = Color.White,
                IconSize = Scale(14),
                Size = ScaledSize(24, 24),
                Location = new Point(toast.Width - Scale(32), Scale(8)),
                BackColor = Color.FromArgb(80, 255, 255, 255),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(120, 255, 255, 255);
            btnClose.Click += (s, e) => toast.Close();
            toast.Controls.Add(btnClose);

            toast.Show();

            Timer closeTimer = new Timer();
            closeTimer.Interval = 3000;
            closeTimer.Tick += (s, e) =>
            {
                toast.Close();
                closeTimer.Stop();
                closeTimer.Dispose();
            };
            closeTimer.Start();

            toast.Click += (s, e) => toast.Close();
        }
    }
}