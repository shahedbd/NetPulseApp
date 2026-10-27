// UserControls/Pages/Wifi/SignalHistoryChart.cs  (copied from WiFiPasswordViewer)
using System.Drawing.Drawing2D;
using NetPulseApp.Helper;
using NetPulseApp.Managers;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Custom-painted 60-second RSSI line chart (FR-1.3/1.4): Y -30…-90 dBm,
    /// dotted "Weak" line at -80, Min/Avg/Max/Drops footer. A null sample is
    /// a drop (connection lost). History resets when the BSSID changes.
    /// </summary>
    public class SignalHistoryChart : Control
    {
        private const int MaxSamples = 60;
        private const int YTop = -30, YBottom = -90;

        private readonly List<int?> _samples = new();
        private string _bssid = "";
        private string _marker = "";
        private int _markerTtl;

        public SignalHistoryChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Font = new Font("Segoe UI", 8f);
            AccessibleName = "Signal history chart";
        }

        public void AddSample(int? rssi, string? bssid)
        {
            if (rssi.HasValue && !string.IsNullOrEmpty(bssid) && bssid != _bssid)
            {
                if (_bssid.Length > 0)
                {
                    _samples.Clear();
                    _marker = $"Switched to {bssid}";
                    _markerTtl = 10;
                }
                _bssid = bssid;
            }

            _samples.Add(rssi);
            if (_samples.Count > MaxSamples) _samples.RemoveAt(0);
            if (_markerTtl > 0 && --_markerTtl == 0) _marker = "";
            Invalidate();
        }

        public void Reset()
        {
            _samples.Clear();
            _bssid = "";
            _marker = "";
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var t = ThemeManager.Instance.CurrentTheme;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(t.CardBackground);

            float s = DeviceDpi / 96f;
            int S(int v) => (int)(v * s);

            using var bold = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var text = new SolidBrush(t.TextColor);
            using var dim = new SolidBrush(t.SecondaryTextColor);

            // ── Title + current value ─────────────────────────────────────────
            int? last = _samples.LastOrDefault();
            g.DrawString("Signal - last 60 seconds", bold, text, S(10), S(8));
            if (last.HasValue)
            {
                string cur = $"{last} dBm";
                var sz = g.MeasureString(cur, bold);
                using var cb = new SolidBrush(WifiAnalyzerUIHelper.RssiColor(last.Value));
                g.DrawString(cur, bold, cb, Width - sz.Width - S(10), S(8));
            }

            var plot = new Rectangle(S(46), S(34), Width - S(60), Height - S(34) - S(48));
            if (plot.Width < 20 || plot.Height < 20) return;

            float Y(int dbm) => plot.Top + (YTop - Math.Clamp(dbm, YBottom, YTop)) * plot.Height / (float)(YTop - YBottom);
            float X(int i) => plot.Left + i * plot.Width / (float)(MaxSamples - 1);

            // ── Grid + Y labels ───────────────────────────────────────────────
            using var grid = new Pen(t.GraphGridColor, 1);
            for (int db = YTop; db >= YBottom; db -= 10)
            {
                float y = Y(db);
                g.DrawLine(grid, plot.Left, y, plot.Right, y);
                g.DrawString(db.ToString(), Font, dim, S(10), y - S(7));
            }

            using (var weak = new Pen(WifiAnalyzerUIHelper.Poor, Math.Max(1, s)) { DashStyle = DashStyle.Dot })
            {
                float y = Y(-80);
                g.DrawLine(weak, plot.Left, y, plot.Right, y);
                g.DrawString("Weak", Font, dim, plot.Right - S(32), y - S(14));
            }

            // ── X labels ──────────────────────────────────────────────────────
            string[] xl = { "-60s", "-40s", "-20s", "now" };
            for (int i = 0; i < xl.Length; i++)
            {
                float x = plot.Left + i * plot.Width / 3f;
                var sz = g.MeasureString(xl[i], Font);
                g.DrawString(xl[i], Font, dim, Math.Min(x - sz.Width / 2, Width - sz.Width - 2), plot.Bottom + S(3));
            }

            // ── Line (right-aligned: newest sample at "now") ──────────────────
            int offset = MaxSamples - _samples.Count;
            var pts = new List<PointF>();
            void Flush()
            {
                if (pts.Count > 1)
                    using (var pen = new Pen(t.AccentColor, 2 * s) { LineJoin = LineJoin.Round })
                        g.DrawLines(pen, pts.ToArray());
                else if (pts.Count == 1)
                    using (var b = new SolidBrush(t.AccentColor))
                        g.FillEllipse(b, pts[0].X - 2 * s, pts[0].Y - 2 * s, 4 * s, 4 * s);
                pts.Clear();
            }
            for (int i = 0; i < _samples.Count; i++)
            {
                if (_samples[i] is int v) pts.Add(new PointF(X(offset + i), Y(v)));
                else Flush();
            }
            Flush();

            if (_marker.Length > 0)
                g.DrawString(_marker, Font, dim, plot.Left + S(4), plot.Top + S(2));

            // ── Footer stats ──────────────────────────────────────────────────
            var vals = _samples.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            int drops = _samples.Count(v => !v.HasValue);
            string stats = vals.Count == 0
                ? $"Min --    Avg --    Max --    Drops {drops}"
                : $"Min {vals.Min()}    Avg {Math.Round(vals.Average())}    Max {vals.Max()}    Drops {drops}";
            g.DrawString(stats, Font, text, S(10), Height - S(22));
        }
    }
}
