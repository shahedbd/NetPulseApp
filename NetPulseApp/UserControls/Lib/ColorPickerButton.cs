using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace NetPulseApp.UserControls.Lib
{
    /// <summary>
    /// Button that shows a color swatch (hex label + picker icon) and opens
    /// the standard ColorDialog on click. Drop-in for any "pick a color"
    /// setting.
    /// </summary>
    public class ColorPickerButton : Button
    {
        private Color _selectedColor = Color.White;
        private bool _isHovered = false;

        // Built in code only (Settings sections), never designer-placed, so
        // this must not be written into a .Designer.cs (WFO1000).
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                _selectedColor = value;
                Invalidate();
            }
        }

        public event EventHandler<Color> ColorChanged;

        public ColorPickerButton()
        {
            Size = new Size(120, 40);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Click += ColorPickerButton_Click;
            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
        }

        private void ColorPickerButton_Click(object sender, EventArgs e)
        {
            using var dialog = new ColorDialog
            {
                Color = _selectedColor,
                FullOpen = true,
                AnyColor = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SelectedColor = dialog.Color;
                ColorChanged?.Invoke(this, dialog.Color);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (_isHovered)
            {
                using var path = GetRoundedRectangle(new Rectangle(2, 2, Width - 4, Height - 4), 6);
                using var shadowBrush = new PathGradientBrush(path)
                {
                    CenterColor = Color.FromArgb(50, 0, 0, 0),
                    SurroundColors = new[] { Color.FromArgb(0, 0, 0, 0) }
                };
                e.Graphics.FillPath(shadowBrush, path);
            }

            using (var path = GetRoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), 6))
            using (var brush = new SolidBrush(_selectedColor))
                e.Graphics.FillPath(brush, path);

            using (var path = GetRoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), 6))
            using (var pen = new Pen(_isHovered ? Color.FromArgb(100, 100, 100) : Color.FromArgb(200, 200, 200), 2))
                e.Graphics.DrawPath(pen, path);

            string colorText = _selectedColor.A == 255
                ? $"#{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}"
                : $"#{_selectedColor.A:X2}{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";

            // Perceived brightness decides label color so it stays readable
            // against any swatch.
            int brightness = (int)Math.Sqrt(
                _selectedColor.R * _selectedColor.R * 0.299 +
                _selectedColor.G * _selectedColor.G * 0.587 +
                _selectedColor.B * _selectedColor.B * 0.114);

            Color textColor = brightness > 130 ? Color.Black : Color.White;

            using (var textBrush = new SolidBrush(textColor))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                e.Graphics.DrawString(colorText, new Font("Segoe UI", 9f, FontStyle.Bold), textBrush, ClientRectangle, sf);

            string icon = "🎨";
            using (var iconFont = new Font("Segoe UI Emoji", 10f))
            using (var iconBrush = new SolidBrush(textColor))
            {
                SizeF iconSize = e.Graphics.MeasureString(icon, iconFont);
                e.Graphics.DrawString(icon, iconFont, iconBrush, Width - iconSize.Width - 5, (Height - iconSize.Height) / 2);
            }
        }

        private static GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
