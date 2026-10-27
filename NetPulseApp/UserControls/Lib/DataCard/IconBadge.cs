// UserControls/Lib/DataCard/IconBadge.cs
using NetPulseApp.Service.Icons;
using System.ComponentModel;

namespace NetPulseApp.UserControls.Lib.DataCard
{
    /// <summary>
    /// Standalone decorated icon (gradient circle / squircle / soft / outline)
    /// for headers, list rows, dialogs — anywhere outside a data card.
    /// The badge fills the control minus room for the halo.
    /// </summary>
    [DesignerCategory("Code")]
    public class IconBadge : Control
    {
        private readonly IconBadgeRenderer _renderer = new();
        private readonly System.Windows.Forms.Timer _anim = new() { Interval = 16 };
        private float _hover;
        private bool _inside;

        public IconBadge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint
                   | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Size = new Size(56, 56);
            _anim.Tick += (_, _) =>
            {
                float target = _inside ? 1f : 0f;
                _hover += (target - _hover) * 0.25f;
                if (Math.Abs(target - _hover) < 0.01f) { _hover = target; _anim.Stop(); }
                Invalidate();
            };
        }

        public IconBadge(IconChar icon, Color accent, IconBadgeStyle style = IconBadgeStyle.Circle) : this()
        {
            _renderer.Icon = icon;
            _renderer.Accent = accent;
            _renderer.Style = style;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IconChar Icon { get => _renderer.Icon; set { _renderer.Icon = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color Accent { get => _renderer.Accent; set { _renderer.Accent = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IconBadgeStyle BadgeStyle { get => _renderer.Style; set { _renderer.Style = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float GlyphScale { get => _renderer.GlyphScale; set { _renderer.GlyphScale = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowHalo { get => _renderer.ShowHalo; set { _renderer.ShowHalo = value; Invalidate(); } }

        /// <summary>Animate halo / grow on mouse hover.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HoverEffect { get; set; } = true;

        protected override void OnPaint(PaintEventArgs e)
        {
            float scale = DeviceDpi / 96f;
            int pad = (int)Math.Round((ShowHalo ? 6 : 2) * scale);
            int d = Math.Min(Width, Height) - pad * 2;
            if (d < 8) return;

            var bounds = new Rectangle((Width - d) / 2, (Height - d) / 2, d, d);
            _renderer.Draw(e.Graphics, bounds, scale, _hover);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _inside = true;
            if (HoverEffect) _anim.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _inside = false;
            if (HoverEffect) _anim.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _anim.Dispose();
                _renderer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
