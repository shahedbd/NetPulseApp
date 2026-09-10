// UserControls/Pages/Ping/PingSummaryPanel.cs
using System.Windows.Forms.DataVisualization.Charting;
using NetPulseApp.Managers;

namespace NetPulseApp.UserControls.Pages.Ping
{
    /// <summary>
    /// Bottom row: four stat tiles (Sent/Received/Loss/Avg) on the left
    /// and a bar chart of response times on the right.
    /// </summary>
    public sealed class PingSummaryPanel : Panel
    {
        // ── Stat labels ───────────────────────────────────────────────────────
        private readonly Label _lblSent;
        private readonly Label _lblReceived;
        private readonly Label _lblLoss;
        private readonly Label _lblAvg;

        // ── Chart ─────────────────────────────────────────────────────────────
        private readonly Chart _chart;
        private readonly Series _series;

        private readonly ThemeManager _theme = ThemeManager.Instance;

        public PingSummaryPanel()
        {
            _lblSent = MakeBigLabel("0");
            _lblReceived = MakeBigLabel("0");
            _lblLoss = MakeBigLabel("0%");
            _lblAvg = MakeBigLabel("— ms");

            _series = new Series
            {
                ChartType = SeriesChartType.Column,
                Color = Color.FromArgb(96, 165, 250),
                IsValueShownAsLabel = true,
                Font = new Font("Segoe UI", 7.5f)
            };

            _chart = new Chart { Size = new Size(280, 120) };
            _chart.Series.Add(_series);

            var area = new ChartArea();
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 7.5f);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 7.5f);
            area.AxisY.Minimum = 0;
            area.BackColor = Color.Transparent;
            area.BorderColor = Color.Transparent;
            _chart.ChartAreas.Add(area);
            _chart.BackColor = Color.Transparent;

            BuildLayout();
            ApplyTheme();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Reset()
        {
            _lblSent.Text = "0";
            _lblReceived.Text = "0";
            _lblLoss.Text = "0%";
            _lblAvg.Text = "— ms";
            _series.Points.Clear();
        }

        public void Update(int sent, int received, int lost, long avgMs,
                           IReadOnlyList<long> rtts)
        {
            double loss = sent > 0 ? (lost * 100.0 / sent) : 0;

            _lblSent.Text = sent.ToString();
            _lblReceived.Text = received.ToString();
            _lblLoss.Text = $"{loss:F0}%";
            _lblLoss.ForeColor = lost == 0
                ? Color.FromArgb(74, 222, 128)
                : Color.FromArgb(248, 113, 113);
            _lblAvg.Text = avgMs >= 0 ? $"{avgMs} ms" : "— ms";

            _series.Points.Clear();
            for (int i = 0; i < rtts.Count; i++)
                _series.Points.AddXY(i + 1, rtts[i] >= 0 ? rtts[i] : 0);
        }

        public void ApplyTheme()
        {
            var t = _theme.CurrentTheme;
            BackColor = t.CardBackground;
            foreach (Control c in Controls)
                c.ForeColor = t.TextColor;

            if (_chart.ChartAreas.Count > 0)
            {
                _chart.ChartAreas[0].AxisX.LabelStyle.ForeColor = t.SecondaryTextColor;
                _chart.ChartAreas[0].AxisY.LabelStyle.ForeColor = t.SecondaryTextColor;
            }
        }

        // ── Layout ────────────────────────────────────────────────────────────

        private void BuildLayout()
        {
            Height = 140;
            Padding = new Padding(16, 12, 16, 12);

            // Left: summary card
            var summaryCard = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(480, 116),
                BackColor = Color.Transparent
            };

            summaryCard.Controls.Add(MakeTile("Packets Sent", _lblSent, 0));
            summaryCard.Controls.Add(MakeTile("Packets Received", _lblReceived, 120));
            summaryCard.Controls.Add(MakeTile("Packet Loss", _lblLoss, 240));
            summaryCard.Controls.Add(MakeTile("Average Time", _lblAvg, 360));

            // Right: chart card
            var chartCard = new Panel
            {
                Location = new Point(500, 0),
                Size = new Size(320, 116),
                BackColor = Color.Transparent
            };

            var lblChart = new Label
            {
                Text = "📊  Response Time (ms)",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(0, 0)
            };

            _chart.Location = new Point(0, 20);
            _chart.Size = new Size(320, 96);
            chartCard.Controls.AddRange(new Control[] { lblChart, _chart });

            Controls.AddRange(new Control[] { summaryCard, chartCard });
        }

        private static Panel MakeTile(string caption, Label valueLabel, int x)
        {
            var tile = new Panel
            {
                Location = new Point(x, 0),
                Size = new Size(110, 110),
                BackColor = Color.Transparent
            };

            var lbl = new Label
            {
                Text = caption,
                AutoSize = true,
                Font = new Font("Segoe UI", 8f),
                Location = new Point(0, 4)
            };

            valueLabel.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            valueLabel.AutoSize = true;
            valueLabel.Location = new Point(0, 24);

            tile.Controls.AddRange(new Control[] { lbl, valueLabel });
            return tile;
        }

        private static Label MakeBigLabel(string text) =>
            new Label { Text = text, AutoSize = true };
    }
}
