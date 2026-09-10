// UserControls/Pages/Ping/PingResultPanel.cs
using NetPulseApp.Managers;
using NetPulseApp.Model;
using NetPulseApp.Service.Ping;

namespace NetPulseApp.UserControls.Pages.Ping
{
    /// <summary>
    /// "Results" card: black terminal RichTextBox that streams
    /// colored ping output exactly like the UI mockup.
    /// </summary>
    public sealed class PingResultPanel : Panel
    {
        private readonly RichTextBox _terminal;
        private readonly ThemeManager _theme = ThemeManager.Instance;

        // Cached colors — set once, reused per append
        private static readonly Color _colorNormal = Color.FromArgb(204, 204, 204);
        private static readonly Color _colorOk = Color.FromArgb(74, 222, 128);  // green
        private static readonly Color _colorFail = Color.FromArgb(248, 113, 113);  // red
        private static readonly Color _colorLabel = Color.FromArgb(147, 197, 253);  // blue
        private static readonly Color _colorStat = Color.FromArgb(253, 224, 71);   // yellow

        public PingResultPanel()
        {
            _terminal = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = _colorNormal,
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = false
            };

            var lblSection = new Label
            {
                Text = "☰  Results",
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(16, 12)
            };

            var termWrapper = new Panel
            {
                Location = new Point(16, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Left |
                           AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = Color.FromArgb(15, 15, 15),
                Padding = new Padding(6)
            };
            termWrapper.Controls.Add(_terminal);

            Controls.AddRange(new Control[] { lblSection, termWrapper });
            Resize += (s, e) =>
                termWrapper.Size = new Size(Width - 32, Height - 44);

            ApplyTheme();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Clear() => _terminal.Clear();

        /// <summary>Appends a header line when ping starts.</summary>
        public void AppendHeader(string host, string ip, int bytes)
        {
            Append($"Pinging {host} [{ip}] with {bytes} bytes of data:\n",
                   _colorNormal);
        }

        /// <summary>Appends one reply line with color coding.</summary>
        public void AppendResult(PingResult r)
        {
            if (r.Success)
            {
                Append($"Reply from {r.ResolvedIp}: ", _colorNormal);
                Append($"bytes={r.BytesSent} ", _colorLabel);
                Append($"time={r.RoundtripMs}ms ", _colorOk);
                Append($"TTL={r.Ttl}\n", _colorNormal);
            }
            else
            {
                Append($"Request timed out (seq={r.Sequence}): ", _colorNormal);
                Append($"{r.StatusText}\n", _colorFail);
            }
        }

        /// <summary>Appends the statistics footer block.</summary>
        public void AppendStats(string ip, int sent, int received, int lost,
                                long min, long max, long avg)
        {
            double lossPercent = sent > 0 ? (lost * 100.0 / sent) : 0;

            Append($"\nPing statistics for {ip}:\n", _colorStat);
            Append($"    Packets: Sent = {sent}, Received = {received}, Lost = ", _colorNormal);
            Append($"{lost} ({lossPercent:F0}% loss)", lost == 0 ? _colorOk : _colorFail);
            Append($",\nApproximate round trip times in milli-seconds:\n", _colorNormal);
            Append($"    Minimum = {min}ms, Maximum = {max}ms, Average = {avg}ms\n", _colorNormal);
        }

        public void ApplyTheme()
        {
            foreach (Control c in Controls)
                if (c is Label lbl)
                    lbl.ForeColor = _theme.CurrentTheme.TextColor;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Append(string text, Color color)
        {
            _terminal.SelectionStart = _terminal.TextLength;
            _terminal.SelectionLength = 0;
            _terminal.SelectionColor = color;
            _terminal.AppendText(text);
            _terminal.ScrollToCaret();
        }
    }
}
