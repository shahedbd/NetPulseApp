// UserControls/Lib/LatencyGraphControl.cs
using System.Drawing.Drawing2D;

namespace NetPulseApp.UserControls.Lib
{
    /// <summary>
    /// Live response-time graph card for the Ping page. Custom GDI+ plot
    /// (gridlines, filled latency line, live dot) drawn from AppTheme's
    /// Graph* colors — replaces the stock DataVisualization Chart control,
    /// which rendered blank and crashed on this runtime. Scales through
    /// DpiAwareUserControl (AutoScaleMode.Dpi) like the rest of the
    /// template. Timeouts appear as gaps, not zero-spikes. The hosting page
    /// calls ApplyTheme() on ThemeManager.ThemeChanged.
    /// </summary>
    public class LatencyGraphControl : DpiAwareUserControl
    {
        private const double WindowSec = 60;  // x-axis seconds shown
        private const int MaxPoints = 400;    // memory bound per run

        private readonly List<(double X, double? Y)> _points = new();
        private readonly Panel _plot;
        private readonly Label _lblCaption;
        private readonly Label _lblCurrent;
        private readonly Label _lblLive;
        private double _yMax = 20;
        private bool _currentIsError;

        public LatencyGraphControl()
        {
            BorderStyle = BorderStyle.FixedSingle;
            BackColor = themeManager.CurrentTheme.CardBackground;

            // Plot added first, header last — the header (Dock=Top) claims
            // the top strip and the plot fills the rest (MainForm docking
            // convention: last added claims its edge first).
            _plot = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _plot.Resize += (s, e) => _plot.Invalidate();
            _plot.Paint += OnPaintPlot;
            Controls.Add(_plot);

            var header = new Panel { Dock = DockStyle.Top, Height = Scale(44), BackColor = Color.Transparent };
            _lblCaption = new Label
            {
                Text = "RESPONSE TIME (MS)",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = themeManager.CurrentTheme.SecondaryTextColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(Scale(12), Scale(8))
            };
            header.Controls.Add(_lblCaption);

            _lblCurrent = new Label
            {
                Text = "— ms",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = themeManager.CurrentTheme.HeadingColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(Scale(150), Scale(15))
            };
            header.Controls.Add(_lblCurrent);

            _lblLive = new Label
            {
                Text = "● Live",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = themeManager.CurrentTheme.SuccessColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            header.Controls.Add(_lblLive);
            header.Resize += (s, e) =>
                _lblLive.Location = new Point(header.Width - _lblLive.Width - Scale(12), Scale(16));

            Controls.Add(header);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Shows/hides the green Live badge.</summary>
        public void SetLive(bool live) => _lblLive.Visible = live;

        /// <summary>Adds a data point; ms null renders a gap (timeout).</summary>
        public void AddPoint(double elapsedSec, double? ms)
        {
            _points.Add((elapsedSec, ms));
            while (_points.Count > MaxPoints)
                _points.RemoveAt(0);

            if (ms.HasValue && ms.Value * 1.15 > _yMax)
                _yMax = NiceCeiling(ms.Value);

            SetCurrent(ms.HasValue ? $"{ms.Value:0} ms" : "Timeout", ms == null);
            _plot.Invalidate();
        }

        public void Clear()
        {
            _points.Clear();
            _yMax = 20;
            SetCurrent("— ms", false);
            _plot.Invalidate();
        }

        public void ApplyTheme()
        {
            var theme = themeManager.CurrentTheme;
            BackColor = theme.CardBackground;
            _lblCaption.ForeColor = theme.SecondaryTextColor;
            _lblCurrent.ForeColor = _currentIsError ? theme.ErrorColor : theme.HeadingColor;
            _lblLive.ForeColor = theme.SuccessColor;
            _plot.Invalidate();
        }

        // ─────────────────────────────────────────────────────────────────────
        // PAINTING
        // ─────────────────────────────────────────────────────────────────────

        private void OnPaintPlot(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var theme = themeManager.CurrentTheme;

            var plot = new Rectangle(
                Scale(46), Scale(8),
                Math.Max(Scale(60), _plot.Width - Scale(46) - Scale(14)),
                Math.Max(Scale(40), _plot.Height - Scale(8) - Scale(26)));

            double xmax = _points.Count > 0 ? _points[^1].X + 3 : WindowSec;
            double xmin = Math.Max(0, xmax - WindowSec);

            using (var bg = new SolidBrush(theme.GraphBackgroundColor))
                g.FillRectangle(bg, plot);

            using var grid = new Pen(theme.GraphGridColor);
            using var axis = new Pen(theme.BorderColor);
            var labelFont = new Font("Segoe UI", 7.5f);

            // Horizontal gridlines + y-axis value labels
            for (int i = 0; i <= 4; i++)
            {
                double value = _yMax * i / 4.0;
                int y = plot.Bottom - (int)Math.Round(value / _yMax * plot.Height);
                g.DrawLine(grid, plot.Left, y, plot.Right, y);
                TextRenderer.DrawText(g, FormatAxisValue(value), labelFont,
                    new Rectangle(plot.Left - Scale(42), y - Scale(7), Scale(38), Scale(14)),
                    theme.SecondaryTextColor,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }
            g.DrawLine(axis, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

            // x-axis tick labels
            for (int i = 0; i <= 5; i++)
            {
                double xv = xmin + (xmax - xmin) * i / 5;
                int x = plot.Left + (int)Math.Round((xv - xmin) / (xmax - xmin) * plot.Width);
                TextRenderer.DrawText(g, $"{xv:0}s", labelFont,
                    new Rectangle(x - Scale(18), plot.Bottom + Scale(4), Scale(36), Scale(14)),
                    theme.SecondaryTextColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            if (_points.Count == 0)
            {
                TextRenderer.DrawText(g, "Start a ping to see the live graph",
                    new Font("Segoe UI", 9f), plot,
                    theme.SecondaryTextColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            // Consecutive non-null points form line segments; a timeout
            // breaks the segment (gap) instead of spiking to zero.
            var linePath = new GraphicsPath();
            var fillPath = new GraphicsPath();
            var segment = new List<PointF>();
            foreach (var (x, y) in _points)
            {
                if (!y.HasValue)
                {
                    AppendSegment(segment, linePath, fillPath, plot, xmin, xmax);
                    continue;
                }
                segment.Add(ToPointF(x, y.Value, plot, xmin, xmax));
            }
            AppendSegment(segment, linePath, fillPath, plot, xmin, xmax);

            using (fillPath)
            using (linePath)
            {
                using var fill = new SolidBrush(theme.GraphFillColor);
                if (fillPath.PointCount > 2)
                    g.FillPath(fill, fillPath);

                using var line = new Pen(theme.GraphLineColor, 2f);
                if (linePath.PointCount > 1)
                    g.DrawPath(line, linePath);
            }

            DrawLiveDot(g, plot, xmin, xmax);
        }

        private void AppendSegment(List<PointF> segment, GraphicsPath line,
            GraphicsPath fill, Rectangle plot, double xmin, double xmax)
        {
            if (segment.Count == 0)
                return;
            if (segment.Count == 1)
                segment.Add(segment[0]); // lone point: draw as a flat mark

            line.AddLines(segment.ToArray());

            var area = new List<PointF>(segment)
            {
                new(segment[^1].X, plot.Bottom),
                new(segment[0].X, plot.Bottom)
            };
            fill.AddPolygon(area.ToArray());
            segment.Clear();
        }

        private void DrawLiveDot(Graphics g, Rectangle plot, double xmin, double xmax)
        {
            for (int i = _points.Count - 1; i >= 0; i--)
            {
                if (_points[i].Y is not double y) continue;
                var p = ToPointF(_points[i].X, y, plot, xmin, xmax);
                using var dot = new SolidBrush(themeManager.CurrentTheme.GraphLineColor);
                g.FillEllipse(dot, p.X - 3f, p.Y - 3f, 6f, 6f);
                return;
            }
        }

        private PointF ToPointF(double x, double y, Rectangle plot, double xmin, double xmax) => new(
            plot.Left + (float)((x - xmin) / (xmax - xmin) * plot.Width),
            plot.Bottom - (float)(y / _yMax * plot.Height));

        private void SetCurrent(string text, bool isError)
        {
            _currentIsError = isError;
            _lblCurrent.Text = text;
            _lblCurrent.ForeColor = isError
                ? themeManager.CurrentTheme.ErrorColor
                : themeManager.CurrentTheme.HeadingColor;
        }

        private static string FormatAxisValue(double value) =>
            value >= 100 ? $"{value:0}" : $"{value:0.#}";

        /// <summary>Round a latency ceiling up to a human-friendly axis max.</summary>
        private static double NiceCeiling(double value)
        {
            double step = value > 200 ? 100 : value > 100 ? 50 : value > 50 ? 20 : 10;
            return Math.Ceiling(value * 1.15 / step) * step;
        }
    }
}
