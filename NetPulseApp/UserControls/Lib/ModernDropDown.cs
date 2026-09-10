using System.Drawing.Drawing2D;
using System.ComponentModel;
using NetPulseApp.Managers;
using NetPulseApp.Service.Dpi;

namespace NetPulseApp.UserControls.Lib
{
    /// <summary>
    /// Modern themed dropdown — a ComboBox inset in a custom-painted shell:
    /// rounded border (accent on hover), theme-aware colors, owner-drawn list
    /// items, and a drawn caret in place of the stock arrow. Drop-in familiar
    /// API (Items / SelectedItem / SelectedIndex / SelectionChanged) so it
    /// slots into Settings rows where a raw ComboBox would leak stock
    /// WinForms chrome in dark mode.
    /// </summary>
    public class ModernDropDown : Control
    {
        private readonly ComboBox _inner;
        private readonly Label _caret;
        private bool _hover;

        /// <summary>Raised when the selection changes (wraps the inner combo's event).</summary>
        public event EventHandler SelectionChanged;

        public ComboBox.ObjectCollection Items => _inner.Items;

        // Built in code only (Settings sections), never designer-placed, so
        // nothing here belongs in a .Designer.cs. Hidden also matches what
        // stock ComboBox declares for these same two properties (WFO1000).
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedItem
        {
            get => _inner.SelectedItem;
            set => _inner.SelectedItem = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _inner.SelectedIndex;
            set => _inner.SelectedIndex = value;
        }

        private static int S(int value) => DpiAwareService.Scale(value);

        public ModernDropDown()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            Height = S(30);
            Cursor = Cursors.Hand;

            _inner = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _inner.DrawItem += (s, e) => DrawItem(e);
            _inner.SelectedIndexChanged += (s, e) => SelectionChanged?.Invoke(this, EventArgs.Empty);
            Controls.Add(_inner);

            // Opaque caret sitting on top of the inner combo's right end. The
            // combo is deliberately laid out wider than this shell (see
            // LayoutInner) so its own native arrow falls outside the client
            // area and is clipped; this label then supplies the only visible
            // caret, and forwards its click so the gutter still opens the list.
            _caret = new Label
            {
                Text = "▾",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _caret.Click += (s, e) => OpenList();
            Controls.Add(_caret);
            _caret.BringToFront();

            // Hover has to be tracked from the children too — they cover the
            // shell, so the shell's own MouseEnter/Leave alone would never
            // fire once the pointer is over the combo or the caret.
            HookHover(this);
            HookHover(_inner);
            HookHover(_caret);

            Click += (s, e) => OpenList();

            // Height was set before the children existed, so that resize
            // found nothing to lay out. Callers normally set Width via an
            // object initializer (which lays out again), but do it once here
            // so the control is correct even if they don't.
            LayoutInner();
            ApplyTheme();
        }

        private void OpenList()
        {
            if (_inner.Items.Count > 0)
                _inner.DroppedDown = true;
        }

        private void HookHover(Control control)
        {
            control.MouseEnter += (s, e) =>
            {
                if (_hover) return;
                _hover = true;
                Invalidate();
            };
            control.MouseLeave += (s, e) =>
            {
                // Moving between the shell and its own children fires Leave on
                // one and Enter on the other — only drop the hover state once
                // the pointer has actually left the whole control.
                if (ClientRectangle.Contains(PointToClient(MousePosition))) return;
                _hover = false;
                Invalidate();
            };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInner();
        }

        private void LayoutInner()
        {
            if (_inner == null || _caret == null) return;

            int left = S(8);
            int gutter = S(24);        // reserved on the right for our caret
            int nativeArrow = S(24);   // pushed past the right edge, clipped away

            int innerHeight = _inner.PreferredHeight;
            int top = Math.Max(0, (Height - innerHeight) / 2);

            _inner.SetBounds(left, top,
                Math.Max(S(24), Width - left + nativeArrow), innerHeight);

            _caret.SetBounds(Width - gutter, S(1), gutter - S(3), Height - S(2));
        }

        /// <summary>Applies the current theme's colors to shell, caret, and list items.</summary>
        public void ApplyTheme()
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            bool dark = ThemeManager.Instance.IsDarkTheme;

            Color fill = dark ? Color.FromArgb(55, 55, 55) : Color.White;

            BackColor = fill;
            _inner.BackColor = fill;
            _inner.ForeColor = theme.TextColor;

            _caret.BackColor = fill;
            _caret.ForeColor = theme.SecondaryTextColor;

            Invalidate();
        }

        // ── Painting ─────────────────────────────────────────────────────

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var theme = ThemeManager.Instance.CurrentTheme;
            Color border = _hover ? theme.AccentColor : theme.BorderColor;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedPath(bounds, S(4));
            using var borderPen = new Pen(border, 1.5f);
            g.DrawPath(borderPen, path);
        }

        /// <summary>Owner-draws each list entry with theme colors + selection highlight.</summary>
        private void DrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var theme = ThemeManager.Instance.CurrentTheme;
            bool selected = e.State.HasFlag(DrawItemState.Selected);

            using (var bg = new SolidBrush(selected ? theme.AccentColor : _inner.BackColor))
                e.Graphics.FillRectangle(bg, e.Bounds);

            string text = _inner.GetItemText(_inner.Items[e.Index]);
            var textBounds = e.Bounds;
            textBounds.X += S(6);
            textBounds.Width -= S(6);

            TextRenderer.DrawText(e.Graphics, text, e.Font, textBounds,
                selected ? Color.White : theme.TextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
